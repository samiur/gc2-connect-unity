# ABOUTME: Mock GSPro server for testing shot relay functionality.
# ABOUTME: Accepts JSON shots, logs spin data, and sends realistic responses.

"""
Mock GSPro Server

Simulates GSPro's Open Connect API for testing:
- Listens on port 921 (or custom port)
- Accepts JSON shot messages
- Logs received spin data (to verify it arrives correctly)
- Sends realistic response JSON
- Supports configurable response delays

Usage:
    uv run tools/mock_gspro_server.py [--port 921] [--delay-ms 50]
"""

from __future__ import annotations

import argparse
import asyncio
import json
import logging
from dataclasses import dataclass, field
from typing import Any

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s.%(msecs)03d [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger(__name__)


@dataclass
class MockGSProServer:
    """Mock GSPro Open Connect API server."""

    port: int = 921
    response_delay_ms: float = 50.0  # Simulated processing delay
    shot_count: int = field(default=0, init=False)
    heartbeat_count: int = field(default=0, init=False)
    status_count: int = field(default=0, init=False)
    _server: asyncio.Server | None = field(default=None, init=False)

    async def start(self) -> None:
        """Start the mock server."""
        self._server = await asyncio.start_server(
            self._handle_client, "0.0.0.0", self.port
        )
        logger.info(f"Mock GSPro Server listening on port {self.port}")
        logger.info(f"Response delay: {self.response_delay_ms}ms")

    async def stop(self) -> None:
        """Stop the server."""
        if self._server:
            self._server.close()
            await self._server.wait_closed()

    async def _handle_client(
        self, reader: asyncio.StreamReader, writer: asyncio.StreamWriter
    ) -> None:
        """Handle a client connection."""
        addr = writer.get_extra_info("peername")
        logger.info(f"Client connected: {addr}")

        buffer = ""

        try:
            while True:
                data = await reader.read(4096)
                if not data:
                    break

                buffer += data.decode("utf-8")
                logger.debug(f"Received {len(data)} bytes, buffer size: {len(buffer)}")

                # Process complete JSON objects
                while buffer:
                    try:
                        # Try to parse JSON from buffer
                        message, end_idx = self._extract_json(buffer)
                        if message is None:
                            break  # Incomplete JSON, wait for more data

                        buffer = buffer[end_idx:].lstrip()

                        # Process the message
                        response = await self._process_message(message)

                        # Send response if this was a shot (not heartbeat/status)
                        if response:
                            response_json = json.dumps(response)
                            writer.write(response_json.encode("utf-8"))
                            await writer.drain()
                            logger.debug(f"Sent response: {response_json}")

                    except json.JSONDecodeError as e:
                        logger.warning(f"JSON parse error: {e}")
                        # Try to recover by finding next '{'
                        next_brace = buffer.find("{", 1)
                        if next_brace > 0:
                            buffer = buffer[next_brace:]
                        else:
                            buffer = ""
                        break

        except Exception as e:
            logger.error(f"Client error: {e}")
        finally:
            writer.close()
            logger.info(f"Client disconnected: {addr}")

    def _extract_json(self, buffer: str) -> tuple[dict[str, Any] | None, int]:
        """Extract a complete JSON object from the buffer.

        Returns:
            (parsed_dict, end_index) or (None, 0) if incomplete
        """
        if not buffer.strip().startswith("{"):
            # Find the start of JSON
            start = buffer.find("{")
            if start < 0:
                return None, len(buffer)
            buffer = buffer[start:]

        # Use brace matching to find complete JSON
        depth = 0
        in_string = False
        escape = False

        for i, char in enumerate(buffer):
            if escape:
                escape = False
                continue

            if char == "\\":
                escape = True
                continue

            if char == '"':
                in_string = not in_string
                continue

            if in_string:
                continue

            if char == "{":
                depth += 1
            elif char == "}":
                depth -= 1
                if depth == 0:
                    # Found complete JSON
                    json_str = buffer[: i + 1]
                    return json.loads(json_str), i + 1

        return None, 0

    async def _process_message(self, message: dict[str, Any]) -> dict[str, Any] | None:
        """Process a GSPro message and return response."""
        options = message.get("ShotDataOptions", {})

        is_heartbeat = options.get("IsHeartBeat", False)
        has_ball_data = options.get("ContainsBallData", False)
        is_ready = options.get("LaunchMonitorIsReady", False)
        ball_detected = options.get("LaunchMonitorBallDetected", False)

        if is_heartbeat:
            self.heartbeat_count += 1
            if self.heartbeat_count % 10 == 1:  # Log every 10th heartbeat
                logger.info(
                    f"Heartbeat #{self.heartbeat_count} "
                    f"(Ready: {is_ready}, Ball: {ball_detected})"
                )
            # GSPro doesn't respond to heartbeats
            return None

        if not has_ball_data:
            # Status update (not a shot)
            self.status_count += 1
            logger.info(
                f"Status update #{self.status_count}: "
                f"Ready={is_ready}, Ball={ball_detected}"
            )
            # GSPro doesn't respond to status updates
            return None

        # This is a shot
        self.shot_count += 1
        ball_data = message.get("BallData", {})

        # Extract and log spin data (this is what we're testing!)
        speed = ball_data.get("Speed", 0)
        back_spin = ball_data.get("BackSpin", 0)
        side_spin = ball_data.get("SideSpin", 0)
        total_spin = ball_data.get("TotalSpin", 0)
        vla = ball_data.get("VLA", 0)
        hla = ball_data.get("HLA", 0)

        logger.info("=" * 60)
        logger.info(f"SHOT #{self.shot_count} RECEIVED")
        logger.info(f"  Speed:      {speed:.1f} mph")
        logger.info(f"  VLA:        {vla:.1f}°")
        logger.info(f"  HLA:        {hla:.1f}°")
        logger.info(f"  BackSpin:   {back_spin:.0f} rpm")
        logger.info(f"  SideSpin:   {side_spin:.0f} rpm")
        logger.info(f"  TotalSpin:  {total_spin:.0f} rpm")

        # Check for the 3500 RPM issue
        if back_spin == 3500 and side_spin == 0:
            logger.warning("  ⚠️  DEFAULT SPIN DETECTED (3500/0) - Spin data missing!")
        elif back_spin == 0 and side_spin == 0:
            logger.warning("  ⚠️  ZERO SPIN DETECTED - Possible misread!")
        else:
            logger.info("  ✓ Spin data looks valid")

        # Log club data if present
        club_data = message.get("ClubData", {})
        if club_data:
            logger.info(f"  Club Speed: {club_data.get('Speed', 0):.1f} mph")
            logger.info(f"  Path:       {club_data.get('Path', 0):.1f}°")
            logger.info(f"  Face:       {club_data.get('FaceToTarget', 0):.1f}°")

        logger.info("=" * 60)

        # Simulate processing delay
        await asyncio.sleep(self.response_delay_ms / 1000.0)

        # Build response
        response = {
            "Code": 201,
            "Message": "Shot received",
            "Player": {
                "Handed": "RH",
                "Club": "DR",
                "DistanceToTarget": 250,
            },
        }

        return response

    def print_stats(self) -> None:
        """Print server statistics."""
        logger.info(f"Stats: {self.shot_count} shots, {self.heartbeat_count} heartbeats, {self.status_count} status updates")


async def main() -> None:
    parser = argparse.ArgumentParser(description="Mock GSPro Server")
    parser.add_argument("--port", type=int, default=921, help="TCP port (default: 921)")
    parser.add_argument(
        "--delay-ms",
        type=float,
        default=50,
        help="Response delay in ms (default: 50)",
    )
    args = parser.parse_args()

    server = MockGSProServer(
        port=args.port,
        response_delay_ms=args.delay_ms,
    )

    await server.start()

    try:
        if server._server:
            await server._server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.print_stats()
        await server.stop()
        logger.info("Server stopped")


if __name__ == "__main__":
    asyncio.run(main())
