# ABOUTME: GC2 launch monitor simulator for testing USB packet handling.
# ABOUTME: Sends realistic packet patterns via TCP to stress test the native plugin.

"""
GC2 Launch Monitor Simulator

Simulates the GC2's USB packet behavior over TCP:
- Sends packets in 64-byte chunks with realistic timing (1-2ms apart)
- Sends early readings (incomplete, no spin) then final readings (complete)
- Supports configurable timing to stress test packet handling
- Simulates 0M messages for ball detection/device status

Usage:
    uv run tools/gc2_simulator.py [--port 5555] [--packet-delay-ms 1.5]
"""

from __future__ import annotations

import argparse
import asyncio
import logging
import random
import struct
import time
from dataclasses import dataclass, field
from typing import Callable

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s.%(msecs)03d [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger(__name__)


@dataclass
class ShotData:
    """Simulated GC2 shot data."""

    shot_id: int
    speed_mph: float
    launch_angle: float  # ELEVATION_DEG
    direction: float  # AZIMUTH_DEG
    total_spin: float  # SPIN_RPM
    back_spin: float  # BACK_RPM
    side_spin: float  # SIDE_RPM
    # HMT data (optional)
    club_speed: float | None = None
    path: float | None = None  # HPATH_DEG
    attack_angle: float | None = None  # VPATH_DEG
    face_to_target: float | None = None  # FACE_T_DEG

    @classmethod
    def driver(cls, shot_id: int) -> ShotData:
        """Generate a typical driver shot."""
        return cls(
            shot_id=shot_id,
            speed_mph=random.uniform(155, 175),
            launch_angle=random.uniform(9, 13),
            direction=random.uniform(-3, 3),
            total_spin=random.uniform(2200, 3000),
            back_spin=random.uniform(2000, 2800),
            side_spin=random.uniform(-500, 500),
        )

    @classmethod
    def seven_iron(cls, shot_id: int) -> ShotData:
        """Generate a typical 7-iron shot."""
        return cls(
            shot_id=shot_id,
            speed_mph=random.uniform(115, 130),
            launch_angle=random.uniform(15, 19),
            direction=random.uniform(-2, 2),
            total_spin=random.uniform(6000, 8000),
            back_spin=random.uniform(5800, 7800),
            side_spin=random.uniform(-400, 400),
        )

    @classmethod
    def wedge(cls, shot_id: int) -> ShotData:
        """Generate a typical wedge shot."""
        return cls(
            shot_id=shot_id,
            speed_mph=random.uniform(85, 105),
            launch_angle=random.uniform(28, 38),
            direction=random.uniform(-2, 2),
            total_spin=random.uniform(8000, 11000),
            back_spin=random.uniform(7800, 10800),
            side_spin=random.uniform(-300, 300),
        )


@dataclass
class GC2Simulator:
    """Simulates GC2 USB packet behavior over TCP."""

    port: int = 5555
    packet_size: int = 64  # USB packet size
    packet_delay_ms: float = 1.5  # Delay between packets in burst (ms)
    early_reading_delay_ms: float = 200  # Delay before early reading
    final_reading_delay_ms: float = 800  # Additional delay for final reading
    shot_id: int = field(default=0, init=False)
    clients: list[asyncio.StreamWriter] = field(default_factory=list, init=False)
    _server: asyncio.Server | None = field(default=None, init=False)

    async def start(self) -> None:
        """Start the TCP server."""
        self._server = await asyncio.start_server(
            self._handle_client, "0.0.0.0", self.port
        )
        logger.info(f"GC2 Simulator listening on port {self.port}")
        logger.info(f"Packet delay: {self.packet_delay_ms}ms between packets")
        logger.info("Commands: 'driver', '7iron', 'wedge', 'status', 'quit'")

    async def stop(self) -> None:
        """Stop the server."""
        if self._server:
            self._server.close()
            await self._server.wait_closed()
        for client in self.clients:
            client.close()

    async def _handle_client(
        self, reader: asyncio.StreamReader, writer: asyncio.StreamWriter
    ) -> None:
        """Handle a client connection."""
        addr = writer.get_extra_info("peername")
        logger.info(f"Client connected: {addr}")
        self.clients.append(writer)

        try:
            # Send initial device status (ball detected, ready)
            await self._send_device_status(writer, flags=7, balls=1)

            while True:
                # Keep connection alive
                await asyncio.sleep(0.1)
                if writer.is_closing():
                    break
        except Exception as e:
            logger.error(f"Client error: {e}")
        finally:
            self.clients.remove(writer)
            writer.close()
            logger.info(f"Client disconnected: {addr}")

    async def _send_packets(self, writer: asyncio.StreamWriter, data: str) -> None:
        """Send data as 64-byte USB-style packets with realistic timing."""
        encoded = data.encode("utf-8")
        offset = 0
        packet_num = 0

        while offset < len(encoded):
            # Extract next packet (up to 64 bytes)
            chunk = encoded[offset : offset + self.packet_size]
            offset += self.packet_size
            packet_num += 1

            # Log packet info
            logger.debug(
                f"  Packet {packet_num}: {len(chunk)} bytes: {chunk[:40]}..."
                if len(chunk) > 40
                else f"  Packet {packet_num}: {len(chunk)} bytes: {chunk}"
            )

            # Send packet
            writer.write(chunk)
            await writer.drain()

            # Simulate USB interrupt timing (1-2ms between packets)
            if offset < len(encoded):
                delay = self.packet_delay_ms / 1000.0
                await asyncio.sleep(delay)

        logger.info(f"Sent {packet_num} packets, {len(encoded)} bytes total")

    def _build_shot_message(
        self, shot: ShotData, msec_since_contact: int, include_spin: bool
    ) -> str:
        """Build a 0H shot message."""
        lines = [
            "0H",
            f"SHOT_ID={shot.shot_id}",
            f"TIME_SEC=0",
            f"MSEC_SINCE_CONTACT={msec_since_contact}",
            f"SPEED_MPH={shot.speed_mph:.2f}",
            f"AZIMUTH_DEG={shot.direction:.2f}",
            f"ELEVATION_DEG={shot.launch_angle:.2f}",
            f"SPIN_RPM={shot.total_spin:.0f}",
        ]

        if include_spin:
            lines.extend(
                [
                    f"BACK_RPM={shot.back_spin:.0f}",
                    f"SIDE_RPM={shot.side_spin:.0f}",
                ]
            )

        # Add ball position
        lines.extend(
            [
                "IS_LEFT=0",
                "WORLDSTART_X=-53.53",
                "WORLDSTART_Y=91.40",
                "WORLDSTART_Z=-477.94",
            ]
        )

        # Add HMT data if present
        if shot.club_speed is not None:
            lines.append("HMT=1")
            lines.append(f"CLUBSPEED_MPH={shot.club_speed:.1f}")
            if shot.path is not None:
                lines.append(f"HPATH_DEG={shot.path:.1f}")
            if shot.attack_angle is not None:
                lines.append(f"VPATH_DEG={shot.attack_angle:.1f}")
            if shot.face_to_target is not None:
                lines.append(f"FACE_T_DEG={shot.face_to_target:.1f}")
        else:
            lines.append("HMT=0")

        # Add message terminator (\n\t)
        return "\n".join(lines) + "\n\t"

    def _build_status_message(self, flags: int, balls: int) -> str:
        """Build a 0M device status message."""
        lines = [
            "0M",
            f"FLAGS={flags}",
            f"BALLS={balls}",
        ]
        if balls > 0:
            lines.append("BALL1=198,206,12")

        return "\n".join(lines) + "\n\t"

    async def _send_device_status(
        self, writer: asyncio.StreamWriter, flags: int, balls: int
    ) -> None:
        """Send a device status (0M) message."""
        message = self._build_status_message(flags, balls)
        logger.info(f"Sending status: FLAGS={flags}, BALLS={balls}")
        await self._send_packets(writer, message)

    async def fire_shot(self, shot_type: str = "driver") -> None:
        """Fire a simulated shot to all connected clients."""
        if not self.clients:
            logger.warning("No clients connected")
            return

        self.shot_id += 1

        # Create shot based on type
        if shot_type == "driver":
            shot = ShotData.driver(self.shot_id)
        elif shot_type == "7iron":
            shot = ShotData.seven_iron(self.shot_id)
        elif shot_type == "wedge":
            shot = ShotData.wedge(self.shot_id)
        else:
            shot = ShotData.driver(self.shot_id)

        logger.info(f"=== FIRING {shot_type.upper()} SHOT #{self.shot_id} ===")
        logger.info(
            f"Speed: {shot.speed_mph:.1f} mph, Launch: {shot.launch_angle:.1f}°, "
            f"Dir: {shot.direction:.1f}°"
        )
        logger.info(
            f"Spin: {shot.total_spin:.0f} rpm (Back: {shot.back_spin:.0f}, "
            f"Side: {shot.side_spin:.0f})"
        )

        for writer in self.clients:
            # === EARLY READING (no spin data) ===
            logger.info(f"Sending EARLY reading (no spin, {self.early_reading_delay_ms}ms)")
            early_message = self._build_shot_message(
                shot, msec_since_contact=200, include_spin=False
            )
            await self._send_packets(writer, early_message)

            # Wait before final reading
            await asyncio.sleep(self.final_reading_delay_ms / 1000.0)

            # === FINAL READING (with spin data) ===
            logger.info("Sending FINAL reading (with spin)")
            final_message = self._build_shot_message(
                shot, msec_since_contact=1000, include_spin=True
            )
            await self._send_packets(writer, final_message)

        logger.info(f"Shot #{self.shot_id} complete")

    async def send_status(self, ready: bool = True, ball: bool = True) -> None:
        """Send device status to all clients."""
        flags = 7 if ready else 1
        balls = 1 if ball else 0
        for writer in self.clients:
            await self._send_device_status(writer, flags, balls)


async def command_loop(simulator: GC2Simulator) -> None:
    """Interactive command loop."""
    print("\nGC2 Simulator Ready")
    print("=" * 40)
    print("Commands:")
    print("  driver  - Fire a driver shot")
    print("  7iron   - Fire a 7-iron shot")
    print("  wedge   - Fire a wedge shot")
    print("  status  - Send device status")
    print("  burst N - Fire N shots rapidly")
    print("  quit    - Exit")
    print("=" * 40)

    loop = asyncio.get_event_loop()

    while True:
        try:
            # Read command in a non-blocking way
            cmd = await loop.run_in_executor(None, input, "\n> ")
            cmd = cmd.strip().lower()

            if cmd == "quit" or cmd == "q":
                break
            elif cmd == "driver" or cmd == "d":
                await simulator.fire_shot("driver")
            elif cmd == "7iron" or cmd == "7":
                await simulator.fire_shot("7iron")
            elif cmd == "wedge" or cmd == "w":
                await simulator.fire_shot("wedge")
            elif cmd == "status" or cmd == "s":
                await simulator.send_status()
            elif cmd.startswith("burst"):
                parts = cmd.split()
                count = int(parts[1]) if len(parts) > 1 else 5
                logger.info(f"Firing burst of {count} shots...")
                for i in range(count):
                    await simulator.fire_shot("driver")
                    await asyncio.sleep(0.5)  # Short delay between shots
            elif cmd:
                print(f"Unknown command: {cmd}")

        except (EOFError, KeyboardInterrupt):
            break
        except Exception as e:
            logger.error(f"Command error: {e}")


async def main() -> None:
    parser = argparse.ArgumentParser(description="GC2 Launch Monitor Simulator")
    parser.add_argument("--port", type=int, default=5555, help="TCP port (default: 5555)")
    parser.add_argument(
        "--packet-delay-ms",
        type=float,
        default=1.5,
        help="Delay between packets in ms (default: 1.5)",
    )
    parser.add_argument(
        "--early-delay-ms",
        type=float,
        default=200,
        help="Delay before early reading in ms (default: 200)",
    )
    parser.add_argument(
        "--final-delay-ms",
        type=float,
        default=800,
        help="Additional delay for final reading in ms (default: 800)",
    )
    args = parser.parse_args()

    simulator = GC2Simulator(
        port=args.port,
        packet_delay_ms=args.packet_delay_ms,
        early_reading_delay_ms=args.early_delay_ms,
        final_reading_delay_ms=args.final_delay_ms,
    )

    await simulator.start()

    try:
        # Run server and command loop concurrently
        await asyncio.gather(
            simulator._server.serve_forever() if simulator._server else asyncio.sleep(0),
            command_loop(simulator),
        )
    except KeyboardInterrupt:
        pass
    finally:
        await simulator.stop()
        logger.info("Simulator stopped")


if __name__ == "__main__":
    asyncio.run(main())
