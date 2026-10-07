"""Loopback HTTP/WebSocket/gRPC contract tests with a fake emulator; no Android media claims."""
import asyncio
import json
import os
from pathlib import Path
import socket
import sys
import tempfile
import unittest
import base64
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from videobridge_gateway import upstream_gateway as upstream
from videobridge_gateway.controller_audio import checked_audio
import aiohttp
import grpc


def free_port():
    with socket.socket() as sock:
        sock.bind(('127.0.0.1', 0))
        return sock.getsockname()[1]


class FakeRtc(upstream.rtc_grpc.RtcServicer):
    def __init__(self):
        self.requests = []
        self.messages = []
        self.teardown = asyncio.Event()
        self.failure = None

    async def RequestRtcStream(self, request, context):
        if self.failure:
            await context.abort(self.failure, 'private emulator-test-token detail')
        if dict(context.invocation_metadata()).get('authorization') != 'Bearer emulator-test-token':
            await context.abort(grpc.StatusCode.UNAUTHENTICATED, 'Missing emulator token')
        self.requests.append(request)
        return upstream.rtc.RtcStreamResponse(id=upstream.rtc.Id(guid='fake-stream'))

    async def SendJsepMessage(self, request, context):
        self.messages.append(json.loads(request.jsep_msg.message))
        if self.messages[-1].get('bye'):
            self.teardown.set()
        return upstream.rtc.SendJsepMessageResponse()

    async def ReceiveJsepMessageStream(self, request, context):
        yield upstream.rtc.ReceiveJsepMessageResponse(jsep_msg=upstream.rtc.JsepMsg(
            id=request.id, message=json.dumps({'sdp': {'type': 'offer', 'sdp': 'fake-sdp'}})))
        await asyncio.Event().wait()


class FakeController(upstream.ec_grpc.EmulatorControllerServicer):
    async def streamAudio(self, request, context):
        if dict(context.invocation_metadata()).get('authorization') != 'Bearer emulator-test-token':
            await context.abort(grpc.StatusCode.UNAUTHENTICATED, 'Missing token')
        while True:
            yield upstream.ec.AudioPacket(format=request, audio=b'\x00\x40\x00\xc0' * 960)
            await asyncio.sleep(.02)
    def __init__(self):
        self.inputs = []
        self.screenshots = 0

    async def sendTouch(self, request, context):
        self.last_release = request
        self.inputs.append(request)
        return upstream.ec.google_dot_protobuf_dot_empty__pb2.Empty()

    async def sendKey(self, request, context):
        self.inputs.append(request)
        return upstream.ec.google_dot_protobuf_dot_empty__pb2.Empty()

    async def getScreenshot(self, request, context):
        self.screenshots += 1
        return upstream.ec.Image(format=upstream.ec.ImageFormat(width=1, height=1),
            image=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a1i8AAAAASUVORK5CYII='))

    async def getStatus(self, request, context):
        return upstream.ec.EmulatorStatus(version='fake-only', booted=True,
            platformConfig={'hw.lcd.width':'720','hw.lcd.height':'1280'})


class GatewayContractTests(unittest.IsolatedAsyncioTestCase):
    async def test_audio_rejects_oversized_unaligned_and_wrong_format(self):
        fmt=upstream.ec.AudioFormat(samplingRate=48000,channels=upstream.ec.AudioFormat.Stereo,format=upstream.ec.AudioFormat.AUD_FMT_S16)
        for payload in (b'',b'123',b'0'*19204):
            with self.assertRaises(ValueError):checked_audio(upstream.ec.AudioPacket(format=fmt,audio=payload))
        fmt.samplingRate=44100
        with self.assertRaises(ValueError):checked_audio(upstream.ec.AudioPacket(format=fmt,audio=b'1234'))
    async def test_audio_receipt_bounds_and_exclusive_ownership(self):
        ws = await self.client.ws_connect('/audio', protocols=['local-test-token'])
        message = await ws.receive(timeout=2)
        self.assertEqual(message.type, aiohttp.WSMsgType.BINARY)
        self.assertEqual(len(message.data), 3840)
        with self.assertRaises(aiohttp.WSServerHandshakeError) as error:
            await self.client.ws_connect('/audio', protocols=['local-test-token'])
        self.assertEqual(error.exception.status, 409)
        await ws.send_str('ack')
        self.assertEqual((await ws.receive(timeout=2)).type, aiohttp.WSMsgType.BINARY)
        # No further receipt: server closes instead of building a playback backlog.
        self.assertEqual((await ws.receive(timeout=2)).type, aiohttp.WSMsgType.CLOSE)
        await ws.close()

    async def test_audio_requires_websocket_token(self):
        with self.assertRaises(aiohttp.WSServerHandshakeError) as error:
            await self.client.ws_connect('/audio', protocols=['wrong-token'])
        self.assertEqual(error.exception.status, 401)

    async def test_audio_shutdown_cancels_active_stream(self):
        ws = await self.client.ws_connect('/audio', protocols=['local-test-token'])
        self.assertEqual((await ws.receive(timeout=2)).type, aiohttp.WSMsgType.BINARY)
        async with self.client.post('/shutdown') as response:
            self.assertEqual(response.status,200)
        await asyncio.wait_for(self.process.wait(),5)
        await ws.close()
    async def asyncSetUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.server = grpc.aio.server()
        self.rtc = FakeRtc()
        upstream.rtc_grpc.add_RtcServicer_to_server(self.rtc,self.server)
        self.controller = FakeController()
        upstream.ec_grpc.add_EmulatorControllerServicer_to_server(self.controller,self.server)
        port = self.server.add_insecure_port('127.0.0.1:0')
        await self.server.start()
        discovery = Path(self.temp.name)/'fake.ini'
        discovery.write_text(f'grpc.port={port}\ngrpc.token=emulator-test-token\n')
        gateway = free_port()
        env = dict(os.environ, ANDROID_DESKTOP_DISCOVERY_FILE=str(discovery),
            ANDROID_DESKTOP_GATEWAY_PORT=str(gateway), ANDROID_DESKTOP_SESSION_TOKEN='local-test-token')
        self.process = await asyncio.create_subprocess_exec(os.environ.get('ANDROID_DESKTOP_TEST_PYTHON', sys.executable), '-B',
            os.environ.get('ANDROID_DESKTOP_TEST_GATEWAY', str(Path(__file__).resolve().parents[1]/'run.py')),
            env=env, stdout=asyncio.subprocess.DEVNULL, stderr=asyncio.subprocess.PIPE)
        self.client = aiohttp.ClientSession(base_url=f'http://127.0.0.1:{gateway}',
            headers={'Authorization':'Bearer local-test-token'}, timeout=aiohttp.ClientTimeout(total=10))
        for attempt in range(40):
            if self.process.returncode is not None:
                self.fail((await self.process.stderr.read()).decode())
            try:
                async with self.client.get('/api/v1/emulator/status') as response:
                    if response.status == 200: break
            except aiohttp.ClientConnectionError:
                await asyncio.sleep(.1)
        else: self.fail('Gateway did not start')

    async def asyncTearDown(self):
        try:
            if self.process.returncode is None:
                async with self.client.post('/shutdown') as response: await response.read()
            await asyncio.wait_for(self.process.wait(),5)
        finally:
            if self.process.returncode is None:
                self.process.kill(); await self.process.wait()
            await self.client.close()
            await self.server.stop(0)
            self.temp.cleanup()

    async def test_unauthorized_and_external_origins_are_rejected(self):
        async with self.client.get('/api/v1/emulator/status',headers={'Authorization':'Bearer wrong'}) as response:
            self.assertEqual(response.status,401)
        async with self.client.get('/api/v1/emulator/status',headers={'Origin':'https://external.example'}) as response:
            self.assertEqual(response.status,403)
        async with self.client.get('/api/v1/emulator/gps') as response:
            self.assertEqual(response.status,404)

    async def test_probe_invokes_rtc_and_tears_down_without_media_claim(self):
        async with self.client.get('/probe') as response:
            self.assertEqual(response.status,200)
            data = await response.json()
            self.assertFalse(data['mediaVerified'])
        self.assertTrue(self.rtc.teardown.is_set())
        self.assertEqual(len(self.rtc.requests[-1].ice_server_config.ice_servers),0)

    async def test_failed_probe_reports_status_without_private_rpc_details(self):
        self.rtc.failure = grpc.StatusCode.PERMISSION_DENIED
        async with self.client.get('/probe') as response:
            self.assertEqual(response.status, 502)
            body = await response.text()
            data = json.loads(body)
            self.assertEqual(data['rpcStatus'], 'PERMISSION_DENIED')
            self.assertFalse(data['rtcRequestAccepted'])
            self.assertFalse(data['mediaVerified'])
            self.assertNotIn('emulator-test-token', body)

    async def test_controller_has_one_frame_in_flight_and_releases_on_disconnect(self):
        async with self.client.ws_connect('/controller', protocols=['local-test-token']) as ws:
            header = await ws.receive_json(timeout=5)
            frame = await ws.receive(timeout=5)
            self.assertEqual(header, {'kind':'frame', 'seq':1, 'width':1, 'height':1})
            self.assertEqual(frame.type, aiohttp.WSMsgType.BINARY)
            await asyncio.sleep(.2)
            self.assertEqual(self.controller.screenshots, 1)
            with self.assertRaises(aiohttp.WSServerHandshakeError) as conflict:
                await self.client.ws_connect('/controller', protocols=['local-test-token'])
            self.assertEqual(conflict.exception.status, 409)
            key = upstream.ec.InputEvent(key_event=upstream.ec.KeyboardEvent(key='Shift',eventType=0))
            await ws.send_bytes(key.SerializeToString())
            event = upstream.ec.InputEvent(touch_event=upstream.ec.TouchEvent(touches=[upstream.ec.Touch(identifier=0,x=0,y=0,pressure=512)]))
            await ws.send_bytes(event.SerializeToString())
            await ws.send_json({'ack':1})
            self.assertEqual((await ws.receive_json(timeout=5))['seq'], 2)
            await ws.receive(timeout=5)
            self.assertTrue(any(getattr(row, 'touches', []) and row.touches[0].pressure == 512 for row in self.controller.inputs))
            async with self.client.get('/controller') as response:
                self.assertEqual(response.status, 401)
        for attempt in range(20):
            if getattr(self.controller, 'last_release', None) and len(self.controller.last_release.touches) == 10:
                break
            await asyncio.sleep(.05)
        self.assertEqual(len(self.controller.last_release.touches), 10)
        self.assertTrue(all(point.pressure == 0 for point in self.controller.last_release.touches))
        self.assertTrue(any(isinstance(row, upstream.ec.KeyboardEvent) and row.key == 'Shift' and row.eventType == 1 for row in self.controller.inputs))

    async def test_controller_rejects_out_of_bounds_touch_and_wrong_frame_ack(self):
        for payload in (upstream.ec.InputEvent(touch_event=upstream.ec.TouchEvent(touches=[upstream.ec.Touch(identifier=0,x=2,y=0,pressure=512)])).SerializeToString(), {'ack':999}):
            async with self.client.ws_connect('/controller', protocols=['local-test-token']) as ws:
                await ws.receive_json(timeout=5); await ws.receive(timeout=5)
                if isinstance(payload, bytes): await ws.send_bytes(payload)
                else: await ws.send_json(payload)
                message = await ws.receive(timeout=5)
                self.assertIn(message.type, (aiohttp.WSMsgType.CLOSE, aiohttp.WSMsgType.CLOSED))
            await asyncio.sleep(.1)

    async def test_signals_use_authenticated_websocket_and_no_external_ice(self):
        async with self.client.ws_connect('/api/v1/emulator/ws-jsep', protocols=['local-test-token'],
            headers={'Origin':'https://android-desktop.local'}) as ws:
            start = await ws.receive_json(timeout=5)
            self.assertEqual(start,{'start':{'iceServers':[]}})
            offer = await ws.receive_json(timeout=5)
            self.assertEqual(offer['sdp']['type'],'offer')
            await ws.send_json({'sdp':{'type':'answer','sdp':'fake-answer'}})
            await ws.send_json({'bye':True})
            await asyncio.wait_for(self.rtc.teardown.wait(),5)
        self.assertTrue(any('sdp' in m for m in self.rtc.messages))


if __name__ == '__main__':
    unittest.main()
