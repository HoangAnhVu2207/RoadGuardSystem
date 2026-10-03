"""Run the disposable Postman API after verifying its effective SQL target.

Credentials/storage/JWT settings are process environment variables only.
Build API and Seeder first. No migration or seed occurs in this launcher.
"""
import argparse
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--api-dll", type=Path, default=ROOT / "RoadGuardSystem.API/bin/Debug/net8.0/RoadGuardSystem.eAPI.dll")
    args = parser.parse_args()
    env = os.environ.copy()
    connection = env.get("ROADGUARD_CONNECTION_STRING")
    if not connection:
        raise SystemExit("Set ROADGUARD_CONNECTION_STRING privately; expected .\\HANHNAV / RoadGuardPostmanTest")
    check = subprocess.run(["dotnet", str(ROOT / "tools/RoadGuardSystem.Seeder/bin/Debug/net8.0/RoadGuardSystem.Seeder.dll"),
                            "--postman-disposable", "--verify-only"], env=env, capture_output=True, text=True)
    if check.returncode:
        raise SystemExit("SQL target verification failed; API not started")
    env.update({"RoadGuardDatabase__ConnectionString": connection,
                "RoadGuardDatabase__InitializeOnStartup": "false",
                "RoadGuardDatabase__SeedDevelopmentUsers": "false",
                "ASPNETCORE_ENVIRONMENT": "Development"})
    env.setdefault("ASPNETCORE_URLS", "http://127.0.0.1:5112")
    print("Verified API target: .\\HANHNAV / RoadGuardPostmanTest; startup migration/seed disabled", flush=True)
    raise SystemExit(subprocess.call(["dotnet", str(args.api_dll.resolve())], cwd=args.api_dll.resolve().parent, env=env))
