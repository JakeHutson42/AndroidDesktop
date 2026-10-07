"""Local Phase 0 wrapper around Google's pinned gateway; media stays in the emulator."""
import asyncio
import hmac
import logging
import os
import sys
import json
from aiohttp import web
import grpc
from videobridge_gateway import upstream_gateway as upstream
from videobridge_gateway.controller_display import controller_stream, checked_image
from videobridge_gateway.controller_audio import audio_stream


@web.middleware
async def authorize(request, handler):
    origin = request.headers.get("Origin")
    if origin and origin != "https://android-desktop.local":
        raise web.HTTPForbidden()
    if request.method == "OPTIONS" and origin == "https://android-desktop.local":
        return web.Response(headers={"Access-Control-Allow-Origin": origin,
            "Access-Control-Allow-Headers": "Authorization, Content-Type", "Access-Control-Allow-Methods": "GET, POST"})
    token = request.app["session_token"]
    supplied = request.headers.get("Authorization", "").removeprefix("Bearer ")
    # WebSocket browser API cannot add headers; token is supplied via subprotocol.
    if request.path in ("/api/v1/emulator/ws-jsep", "/controller", "/audio"):
        supplied = request.headers.get("Sec-WebSocket-Protocol", "")
    if not token or not hmac.compare_digest(supplied, token):
        raise web.HTTPUnauthorized()
    response = await handler(request)
    response.headers["Access-Control-Allow-Origin"] = "https://android-desktop.local"
    response.headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type"
    response.headers["Cache-Control"] = "no-store"
    return response


async def main():
    # No token on command line, in navigation URL, logs or persisted reports.
    token = os.environ["ANDROID_DESKTOP_SESSION_TOKEN"]
    port = int(os.environ["ANDROID_DESKTOP_GATEWAY_PORT"])
    props = upstream.parse_discovery_file(os.environ["ANDROID_DESKTOP_DISCOVERY_FILE"])
    if not props.get("grpc.token"):
        raise RuntimeError("This prototype requires a token-authenticated emulator discovery file.")
    upstream.DISCOVERY_PROPS = props
    channel = grpc.aio.insecure_channel("127.0.0.1:" + props["grpc.port"])
    upstream.EMULATOR_CHANNEL = channel
    upstream.VIDEOBRIDGE_CHANNEL = channel
    stopped = asyncio.Event()
    app = web.Application(middlewares=[authorize])
    app["session_token"] = token
    app["videobridge_token"] = props["grpc.token"]

    async def status(request):
        if request.method == "OPTIONS":
            return web.Response()
        return await upstream.handle_status(request)

    async def probe(request):
        # Actually invoke RTC. UNIMPLEMENTED is a failed compatibility check.
        stub = upstream.rtc_grpc.RtcStub(channel)
        metadata = upstream.get_emulator_metadata()
        config = upstream.ice.IceServerConfig()
        try:
            response = await stub.RequestRtcStream(
                upstream.rtc.RtcStreamRequest(ice_server_config=config), metadata=metadata, timeout=10)
        except grpc.aio.AioRpcError as error:
            # Report only the status code: RPC details may contain credentials or private data.
            return web.json_response({"rtcRequestAccepted": False, "mediaVerified": False,
                                      "rpcStatus": error.code().name}, status=502)
        await stub.SendJsepMessage(upstream.rtc.SendJsepMessageRequest(
            jsep_msg=upstream.rtc.JsepMsg(id=response.id, message='{"bye":true}')),
            metadata=metadata, timeout=5)
        return web.json_response({"rtcRequestAccepted": True, "mediaVerified": False})

    async def probe_controller(request):
        stub = upstream.ec_grpc.EmulatorControllerStub(channel)
        try:
            image = await stub.getScreenshot(upstream.ec.ImageFormat(format=upstream.ec.ImageFormat.PNG),
                metadata=upstream.get_emulator_metadata(), timeout=10)
            checked_image(image)
            return web.json_response({"controllerAccepted": bool(image.image), "width": image.format.width,
                "height": image.format.height, "bytes": len(image.image), "mediaVerified": False})
        except grpc.aio.AioRpcError as error:
            return web.json_response({"controllerAccepted": False, "rpcStatus": error.code().name}, status=502)

    async def rotate(request):
        body = await request.json()
        angle = int(body["degrees"])
        if angle not in (0, 90, 180, 270):
            raise web.HTTPBadRequest()
        stub = upstream.ec_grpc.EmulatorControllerStub(channel)
        model = upstream.ec.PhysicalModelValue(
            target=upstream.ec.PhysicalModelValue.ROTATION,
            value=upstream.ec.ParameterValue(data=[0, 0, angle]),
            interpolation=upstream.ec.PhysicalModelValue.STEP)
        await stub.setPhysicalModel(model, metadata=upstream.get_emulator_metadata(), timeout=5)
        return web.json_response({"requestedDegrees": angle})

    async def shutdown(request):
        stopped.set()
        return web.json_response({"stopping": True})

    async def release(request):
        data = await request.json()
        keys = data.get('keys', [])
        if len(keys) > 64 or any(not isinstance(key,str) or len(key) > 32 for key in keys):
            raise web.HTTPBadRequest()
        await upstream.release_touches()
        stub = upstream.ec_grpc.EmulatorControllerStub(channel)
        for key in keys:
            await stub.sendKey(upstream.ec.KeyboardEvent(key=key,eventType=1),
                metadata=upstream.get_emulator_metadata(),timeout=5)
        return web.json_response({"releaseRequested": True, "receptionVerified": False})

    app.router.add_route("*", "/api/v1/emulator/status", status)
    app.router.add_get("/api/v1/emulator/ws-jsep", upstream.handle_websocket_jsep)
    app.router.add_get("/probe", probe)
    app.router.add_get("/probe-controller", probe_controller)
    app.router.add_get("/controller", lambda request: controller_stream(request, channel))
    app.router.add_get("/audio", lambda request: audio_stream(request, channel))
    app.router.add_post("/rotate", rotate)
    app.router.add_post("/shutdown", shutdown)
    app.router.add_post("/release", release)
    app.router.add_options("/release", lambda request: web.Response())
    runner = web.AppRunner(app, access_log=None)
    try:
        await runner.setup()
        await web.TCPSite(runner, "127.0.0.1", port).start()
        print("Phase 0 loopback gateway ready", flush=True)
        await stopped.wait()
    finally:
        for socket in list(app.get('controller_sockets', ())):
            await socket.close(code=1001, message=b'Gateway stopping')
        await runner.cleanup()
        await channel.close()


if __name__ == "__main__":
    # Google's sample logs JSEP at INFO; keep it out of normal diagnostics.
    logging.getLogger().setLevel(logging.WARNING)
    if sys.argv[1:] == ["--self-test"]:
        from importlib.metadata import version
        print(json.dumps({"python": sys.version.split()[0], "aiohttp": version("aiohttp"),
            "grpcio": version("grpcio"), "protobuf": version("protobuf"),
            "protocolImports": True, "androidConnected": False, "mediaVerified": False}))
    else:
        asyncio.run(main())
