"""Verify the rendered FE stack and exercise its private initializer without starting Docker."""
import json
import os
from pathlib import Path
import runpy
import subprocess
from tempfile import TemporaryDirectory
from unittest.mock import patch


ROOT = Path(__file__).resolve().parent
CHECKPOINT = "bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0"


def main():
    result = subprocess.run(
        ["docker", "compose", "-f", str(ROOT / "compose.yaml"), "config", "--format", "json"],
        check=True, capture_output=True, text=True,
    )
    services = json.loads(result.stdout)["services"]
    ai = services["ai"]
    assert not ai.get("ports") and set(ai["networks"]) == {"internal"}
    assert not services["sql"].get("ports")
    assert not set(services) & {"scenarios"}
    assert ai["read_only"] and "ALL" in ai["cap_drop"]
    api_env = services["api"]["environment"]
    assert api_env["AiRealWorker__Enabled"] == "true"
    assert api_env["AiRealWorker__WorkerEndpoint"] == "http://ai:8999/"
    assert api_env["AiRealWorker__ServiceTokenFile"] == "/run/secrets/ai-token"
    with TemporaryDirectory() as directory:
        state = Path(directory) / "state"
        token = Path(directory) / "token"
        paths = {"/state": state, "/ai-token": token, "/catalog/accounts.json": ROOT / "real-ai-seed" / "accounts.json"}
        with patch("pathlib.Path", side_effect=paths.__getitem__), patch.dict(os.environ, RG_FE_OBJECT_PORT="29001"):
            runpy.run_path(str(ROOT / "init.py"))
            config = json.loads((state / "config.json").read_text())
            accounts = (state / "demo-accounts.json").read_bytes()
            assert len(json.loads(accounts)) == 14
            assert config["Anh02"]["MockEnabled"] is False
            assert config["AiRealWorker"]["Enabled"] is True
            assert config["AiRealWorker"]["CheckpointSha256"] == CHECKPOINT
            assert len(set(config["AiRealWorker"]["ClassMap"].values())) == 6
            assert config["MinioStorage"]["PublicEndpoint"] == "http://localhost:29001"
            assert (token / "ai-token").read_bytes() == (state / "ai-token").read_bytes()
            try:
                runpy.run_path(str(ROOT / "init.py"))
            except SystemExit as completed:
                assert completed.code == 0
            assert (state / "demo-accounts.json").read_bytes() == accounts
    print("Real AI handoff configuration and credential replay passed.")


if __name__ == "__main__":
    main()
