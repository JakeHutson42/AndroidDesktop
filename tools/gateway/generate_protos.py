from pathlib import Path
import grpc_tools
from grpc_tools import protoc

root = Path(__file__).resolve().parents[2]
protocols = root / 'src' / 'AndroidDesktop' / 'Protocols'
output = Path(__file__).resolve().parent / 'videobridge_gateway' / 'proto'
includes = Path(grpc_tools.__file__).resolve().parent / '_proto'
code = protoc.main(['protoc', f'-I{protocols}', f'-I{includes}',
    f'--python_out={output}', f'--grpc_python_out={output}',
    str(protocols/'emulator_controller.proto'), str(protocols/'ice_config.proto'), str(protocols/'rtc_service_v2.proto')])
raise SystemExit(code)
