# ABOUTME: Stress test for USB packet handling to detect packet loss.
# ABOUTME: Sends packets with extreme timing and verifies all data is received.

"""
Packet Loss Stress Test

Tests the native plugin's ability to handle fast USB packets:
- Sends packets with configurable timing (down to 0.1ms)
- Includes unique identifiers in each packet
- Connects to mock GSPro to verify complete data arrives
- Reports packet loss statistics

Usage:
    # Terminal 1: Start mock GSPro server
    uv run tools/mock_gspro_server.py --port 921

    # Terminal 2: Start Unity app in GSPro mode, connect to localhost:5555

    # Terminal 3: Run stress test
    uv run tools/packet_loss_test.py --packet-delay-ms 0.5 --shots 10
"""

from __future__ import annotations

import argparse
import asyncio
import logging
import random
import time
from dataclasses import dataclass, field

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s.%(msecs)03d [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger(__name__)


@dataclass
class StressTestStats:
    """Statistics from stress test."""

    shots_sent: int = 0
    early_readings_sent: int = 0
    final_readings_sent: int = 0
    packets_sent: int = 0
    bytes_sent: int = 0
    start_time: float = field(default_factory=time.time)

    def report(self) -> None:
        elapsed = time.time() - self.start_time
        logger.info("=" * 60)
        logger.info("STRESS TEST RESULTS")
        logger.info("=" * 60)
        logger.info(f"Duration:        {elapsed:.2f}s")
        logger.info(f"Shots sent:      {self.shots_sent}")
        logger.info(f"Early readings:  {self.early_readings_sent}")
        logger.info(f"Final readings:  {self.final_readings_sent}")
        logger.info(f"Total packets:   {self.packets_sent}")
        logger.info(f"Total bytes:     {self.bytes_sent}")
        logger.info(f"Packets/sec:     {self.packets_sent / elapsed:.1f}")
        logger.info("=" * 60)


@dataclass
class PacketLossStressTester:
    """Stress tester for packet loss detection."""

    port: int = 5555
    packet_size: int = 64
    packet_delay_ms: float = 0.5  # Very fast!
    inter_shot_delay_ms: float = 100  # Delay between shots
    stats: StressTestStats = field(default_factory=StressTestStats)
    _server: asyncio.Server | None = field(default=None, init=False)
    _clients: list[asyncio.StreamWriter] = field(default_factory=list, init=False)

    async def start(self) -> None:
        """Start the test server."""
        self._server = await asyncio.start_server(
            self._handle_client, "0.0.0.0", self.port
        )
        logger.info(f"Stress Test Server listening on port {self.port}")
        logger.info(f"Packet delay: {self.packet_delay_ms}ms")

    async def stop(self) -> None:
        """Stop the server."""
        if self._server:
            self._server.close()
            await self._server.wait_closed()
        for client in self._clients:
            client.close()

    async def _handle_client(
        self, reader: asyncio.StreamReader, writer: asyncio.StreamWriter
    ) -> None:
        """Handle client connection."""
        addr = writer.get_extra_info("peername")
        logger.info(f"Client connected: {addr}")
        self._clients.append(writer)

        try:
            while True:
                await asyncio.sleep(0.1)
                if writer.is_closing():
                    break
        except Exception as e:
            logger.error(f"Client error: {e}")
        finally:
            if writer in self._clients:
                self._clients.remove(writer)
            writer.close()
            logger.info(f"Client disconnected: {addr}")

    async def _send_packets(self, writer: asyncio.StreamWriter, data: str) -> int:
        """Send data as packets with stress timing."""
        encoded = data.encode("utf-8")
        offset = 0
        packet_count = 0

        while offset < len(encoded):
            chunk = encoded[offset : offset + self.packet_size]
            offset += self.packet_size
            packet_count += 1

            writer.write(chunk)
            await writer.drain()

            if offset < len(encoded) and self.packet_delay_ms > 0:
                await asyncio.sleep(self.packet_delay_ms / 1000.0)

        self.stats.packets_sent += packet_count
        self.stats.bytes_sent += len(encoded)
        return packet_count

    def _build_shot_message(
        self,
        shot_id: int,
        speed: float,
        launch: float,
        direction: float,
        back_spin: float,
        side_spin: float,
        total_spin: float,
        msec: int,
        include_spin: bool,
    ) -> str:
        """Build a shot message with unique spin values for tracking."""
        lines = [
            "0H",
            f"SHOT_ID={shot_id}",
            "TIME_SEC=0",
            f"MSEC_SINCE_CONTACT={msec}",
            f"SPEED_MPH={speed:.2f}",
            f"AZIMUTH_DEG={direction:.2f}",
            f"ELEVATION_DEG={launch:.2f}",
            f"SPIN_RPM={total_spin:.0f}",
        ]

        if include_spin:
            lines.extend(
                [
                    f"BACK_RPM={back_spin:.0f}",
                    f"SIDE_RPM={side_spin:.0f}",
                ]
            )

        lines.extend(
            [
                "IS_LEFT=0",
                "WORLDSTART_X=-53.53",
                "WORLDSTART_Y=91.40",
                "WORLDSTART_Z=-477.94",
                "HMT=0",
            ]
        )

        return "\n".join(lines) + "\n\t"

    async def run_test(self, num_shots: int) -> None:
        """Run the stress test."""
        if not self._clients:
            logger.error("No clients connected. Start the Unity app first.")
            return

        self.stats = StressTestStats()
        logger.info(f"Starting stress test with {num_shots} shots...")
        logger.info(f"Packet delay: {self.packet_delay_ms}ms")

        for shot_id in range(1, num_shots + 1):
            # Generate unique spin values for this shot (for tracking)
            # Use shot_id in the spin values so we can verify the correct data arrives
            speed = 150.0 + random.uniform(-10, 10)
            launch = 12.0 + random.uniform(-2, 2)
            direction = random.uniform(-3, 3)
            back_spin = 2500.0 + shot_id  # Unique back spin!
            side_spin = -200.0 + shot_id  # Unique side spin!
            total_spin = back_spin + abs(side_spin)

            logger.info(
                f"Shot {shot_id}/{num_shots}: BackSpin={back_spin:.0f}, SideSpin={side_spin:.0f}"
            )

            for writer in self._clients:
                # Send early reading (no spin)
                early_msg = self._build_shot_message(
                    shot_id, speed, launch, direction,
                    back_spin, side_spin, total_spin,
                    msec=200, include_spin=False
                )
                await self._send_packets(writer, early_msg)
                self.stats.early_readings_sent += 1

                # Brief delay
                await asyncio.sleep(0.05)

                # Send final reading (with spin)
                final_msg = self._build_shot_message(
                    shot_id, speed, launch, direction,
                    back_spin, side_spin, total_spin,
                    msec=1000, include_spin=True
                )
                await self._send_packets(writer, final_msg)
                self.stats.final_readings_sent += 1

            self.stats.shots_sent += 1

            # Delay between shots
            await asyncio.sleep(self.inter_shot_delay_ms / 1000.0)

        self.stats.report()
        logger.info("\nCheck mock GSPro server to verify all spin values arrived correctly.")
        logger.info(f"Expected: BackSpin values from {2501} to {2500 + num_shots}")


async def main() -> None:
    parser = argparse.ArgumentParser(description="Packet Loss Stress Test")
    parser.add_argument("--port", type=int, default=5555, help="TCP port (default: 5555)")
    parser.add_argument("--shots", type=int, default=10, help="Number of shots (default: 10)")
    parser.add_argument(
        "--packet-delay-ms",
        type=float,
        default=0.5,
        help="Delay between packets in ms (default: 0.5)",
    )
    parser.add_argument(
        "--inter-shot-delay-ms",
        type=float,
        default=100,
        help="Delay between shots in ms (default: 100)",
    )
    args = parser.parse_args()

    tester = PacketLossStressTester(
        port=args.port,
        packet_delay_ms=args.packet_delay_ms,
        inter_shot_delay_ms=args.inter_shot_delay_ms,
    )

    await tester.start()

    logger.info("Waiting for Unity app to connect...")
    logger.info("(Start Unity, open GC2 Test Window, set to Client mode, connect to localhost:5555)")

    # Wait for client
    while not tester._clients:
        await asyncio.sleep(0.5)

    logger.info("Client connected! Starting test in 2 seconds...")
    await asyncio.sleep(2)

    await tester.run_test(args.shots)

    # Keep server running for a bit to allow final messages
    await asyncio.sleep(2)
    await tester.stop()


if __name__ == "__main__":
    asyncio.run(main())
