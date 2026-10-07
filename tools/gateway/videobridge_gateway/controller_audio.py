"""Demand-driven PCM audio; independent of the display/input socket."""
import asyncio
import contextlib
import grpc
from aiohttp import web, WSMsgType
from . import upstream_gateway as upstream

RATE = 48000
MAX_BYTES = RATE * 4 // 10  # At most 100 ms of stereo S16 per packet.

def checked_audio(packet):
    fmt = packet.format
    if (fmt.samplingRate != RATE or fmt.channels != upstream.ec.AudioFormat.Stereo
            or fmt.format != upstream.ec.AudioFormat.AUD_FMT_S16
            or not 0 < len(packet.audio) <= MAX_BYTES or len(packet.audio) % 4):
        raise ValueError('Unsupported audio packet')
    return packet.audio

async def audio_stream(request, channel):
    if request.app.get('audio_owner'):
        raise web.HTTPConflict(text='Audio already owned')
    request.app['audio_owner'] = True
    ws = web.WebSocketResponse(protocols=[request.app['session_token']], max_msg_size=64, heartbeat=10)
    rpc = None
    producer = None
    receipt = asyncio.Event()
    try:
        await ws.prepare(request)
        request.app.setdefault('controller_sockets', set()).add(ws)
        stub = upstream.ec_grpc.EmulatorControllerStub(channel)
        rpc = stub.streamAudio(upstream.ec.AudioFormat(samplingRate=RATE,
            channels=upstream.ec.AudioFormat.Stereo, format=upstream.ec.AudioFormat.AUD_FMT_S16),
            metadata=upstream.get_emulator_metadata())
        async def send():
            try:
                async for packet in rpc:
                    receipt.clear()
                    await ws.send_bytes(checked_audio(packet))
                    await asyncio.wait_for(receipt.wait(), .5)
                await ws.close(code=1000, message=b'Audio ended')
            except (grpc.aio.AioRpcError, ValueError, asyncio.TimeoutError):
                await ws.close(code=1011, message=b'Audio unavailable')
        producer = asyncio.create_task(send())
        async for message in ws:
            if message.type != WSMsgType.TEXT or message.data != 'ack' or receipt.is_set():
                await ws.close(code=1008)
                break
            receipt.set()
    finally:
        if rpc is not None:
            rpc.cancel()
        if producer:
            producer.cancel()
            with contextlib.suppress(asyncio.CancelledError, Exception):
                await producer
        request.app.get('controller_sockets', set()).discard(ws)
        request.app['audio_owner'] = False
    return ws
