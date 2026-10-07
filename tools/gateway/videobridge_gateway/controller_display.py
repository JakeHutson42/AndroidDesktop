"""Authenticated local controller display, with one acknowledged frame in flight."""
import asyncio
import contextlib
import json
import grpc
from aiohttp import web, WSMsgType
from google.protobuf.message import DecodeError
from . import upstream_gateway as upstream

MAX_IMAGE_BYTES = 8 * 1024 * 1024
MAX_PIXELS = 4 * 1024 * 1024

def checked_image(image):
    width = image.format.width or image.width
    height = image.format.height or image.height
    if not (0 < width <= 4096 and 0 < height <= 4096 and width * height <= MAX_PIXELS
            and 0 < len(image.image) <= MAX_IMAGE_BYTES and image.image.startswith(b'\x89PNG\r\n\x1a\n')):
        raise ValueError('Invalid or oversized emulator frame')
    return width, height

def checked_input(data, width, height):
    if not 0 < len(data) <= 65536:
        raise ValueError('Invalid input size')
    event = upstream.ec.InputEvent()
    event.ParseFromString(data)
    fields = event.ListFields()
    if len(fields) != 1 or fields[0][0].name not in ('touch_event', 'key_event'):
        raise ValueError('Unsupported input')
    if event.HasField('touch_event'):
        touch = event.touch_event
        if touch.display != 0 or not 1 <= len(touch.touches) <= 10:
            raise ValueError('Invalid touch slots')
        ids = set()
        for point in touch.touches:
            if point.identifier in ids or not 0 <= point.identifier < 10 or not 0 <= point.pressure <= 1024:
                raise ValueError('Invalid touch')
            if not (0 <= point.x < width and 0 <= point.y < height):
                raise ValueError('Touch outside display')
            ids.add(point.identifier)
            point.expiration = 1
    else:
        key = event.key_event
        if key.eventType not in (0, 1, 2) or key.codeType not in (0, 1, 2) or not 0 <= key.keyCode <= 65535 or len(key.key) > 32 or key.text:
            raise ValueError('Invalid key')
    return event

async def controller_stream(request, channel):
    if request.app.get('controller_owner'):
        raise web.HTTPConflict(text='Display already owned')
    request.app['controller_owner'] = True
    ws = web.WebSocketResponse(protocols=[request.app['session_token']], max_msg_size=65536, heartbeat=10)
    task = None
    keys = set()
    stub = upstream.ec_grpc.EmulatorControllerStub(channel)
    metadata = upstream.get_emulator_metadata()
    ack = asyncio.Event()
    current = {'seq': 0, 'width': 0, 'height': 0}
    try:
        await ws.prepare(request)
        request.app.setdefault('controller_sockets', set()).add(ws)
        async def frames():
            try:
                while not ws.closed:
                    started = asyncio.get_running_loop().time()
                    image = await stub.getScreenshot(upstream.ec.ImageFormat(format=upstream.ec.ImageFormat.PNG), metadata=metadata, timeout=5)
                    width, height = checked_image(image)
                    current.update(seq=current['seq'] + 1, width=width, height=height)
                    ack.clear()
                    await ws.send_json(dict(kind='frame', **current))
                    await ws.send_bytes(image.image)
                    await asyncio.wait_for(ack.wait(), 5)
                    # Encoding, transport and drawing already consume this frame's budget.
                    await asyncio.sleep(max(0, 1 / 15 - (asyncio.get_running_loop().time() - started)))
            except (grpc.aio.AioRpcError, ValueError, asyncio.TimeoutError) as error:
                reason = error.code().name if isinstance(error, grpc.aio.AioRpcError) else 'FRAME_UNAVAILABLE'
                if not ws.closed:
                    await ws.send_json({'kind': 'fault', 'status': reason})
                    await ws.close(code=1011)
        task = asyncio.create_task(frames())
        async for message in ws:
            if message.type == WSMsgType.TEXT:
                data = json.loads(message.data)
                if data != {'ack': current['seq']}:
                    raise ValueError('Invalid frame receipt')
                ack.set()
            elif message.type == WSMsgType.BINARY:
                event = checked_input(message.data, current['width'], current['height'])
                if event.HasField('touch_event'):
                    await stub.sendTouch(event.touch_event, metadata=metadata, timeout=5)
                else:
                    key = event.key_event
                    identity = (key.key, key.keyCode, key.codeType)
                    if key.eventType == 0:
                        if len(keys) >= 64 and identity not in keys:
                            raise ValueError('Too many held keys')
                        keys.add(identity)
                    elif key.eventType == 1:
                        keys.discard(identity)
                    await stub.sendKey(key, metadata=metadata, timeout=5)
            elif message.type in (WSMsgType.ERROR, WSMsgType.CLOSE, WSMsgType.CLOSED):
                break
    except (ValueError, DecodeError, grpc.aio.AioRpcError):
        await ws.close(code=1008, message=b'Input rejected')
    finally:
        if task:
            task.cancel()
            with contextlib.suppress(asyncio.CancelledError, Exception):
                await task
        try:
            await upstream.release_touches()
            for key, code, code_type in keys:
                await stub.sendKey(upstream.ec.KeyboardEvent(key=key, keyCode=code, codeType=code_type, eventType=1), metadata=metadata, timeout=2)
        except grpc.aio.AioRpcError:
            pass
        request.app['controller_owner'] = False
        request.app.get('controller_sockets', set()).discard(ws)
    return ws
