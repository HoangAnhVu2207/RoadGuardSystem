"""Package the exact local Docker images and replay their FE dataset on another machine."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

HERE = Path(__file__).resolve().parent
DOCKER = ["docker", "--context", "desktop-linux"]
SERVICES = {"init", "api", "ai", "sql", "mailpit", "minio"}
PROJECT = "rg-fe-real-replay"
PORTS = {"api": 38081, "minio": 39001, "mailpit": 38026}
DRIVERS = ("accounts.json", "manifest.json", "check_local.py", "check_offline.py", "real_flow.py",
           "real_flow_api.py", "real_flow_foundation.py", "real_flow_verify.py", "demo_flow.py",
           "demo_flow_api.py", "demo_flow_repairs.py", "real_field.py", "review_queues.py")


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def require(value, reason):
    if not value:
        raise ValueError(reason)


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def docker(*args):
    return subprocess.check_output([*DOCKER, *map(str, args)], text=True).strip()


def copy(source, target):
    require(source.is_file() and not source.is_symlink(), "Required package input is missing or linked: " + source.name)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, target)


def portable_compose(compose_file, project):
    config = json.loads(docker("compose", "-p", project, "-f", compose_file, "config", "--format", "json"))
    config["services"].pop("scenarios", None)
    require(set(config["services"]) == SERVICES, "Unexpected Compose services")
    config.pop("name", None)
    for resources in ("networks", "volumes"):
        for definition in config[resources].values():
            definition.pop("name", None)
            require(not definition.get("external"), "External resources are not portable")
    images = {}
    for service, settings in config["services"].items():
        inspected, = json.loads(docker("inspect", project + "-" + service + "-1"))
        labels = inspected["Config"]["Labels"]
        require(labels["com.docker.compose.project"] == project and labels["com.docker.compose.service"] == service,
                "Unexpected source container")
        require(all(volume.get("type") == "volume" for volume in settings.get("volumes", [])), "Unexpected host mount")
        image_id = inspected["Image"]
        require(re.fullmatch(r"sha256:[0-9a-f]{64}", image_id), "Invalid image ID")
        tag = "roadguard/fe-" + service + ":" + image_id[7:23]
        images[service] = {"tag": tag, "id": image_id}
        settings.pop("build", None)
        settings.update(image=tag, pull_policy="never")
        if service in PORTS:
            port, = settings["ports"]
            require(port["host_ip"] == "127.0.0.1", "Unexpected port binding")
            port["published"] = str(PORTS[service])
        else:
            require(not settings.get("ports"), "Unexpected public service")
    config["services"]["init"]["environment"]["RG_FE_OBJECT_PORT"] = str(PORTS["minio"])
    return config, images


def assets(folder, target, manifest):
    for name, facts in manifest["files"].items():
        source = (folder / name).resolve()
        require(source.is_relative_to(folder.resolve()) and source.is_file() and
                source.stat().st_size == facts["bytes"] and digest(source) == facts["sha256"],
                "Input checksum failed: " + name)
        copy(source, target / name)


def build(args):
    real = read(args.evidence / "real-flow.json")
    demo = read(args.evidence / "demo-flow.json")
    offline = read(args.evidence / "offline-flow.json")
    normal = read(args.evidence / "real-normal-repair.json")["projects"]
    fields = read(args.evidence / "real-field.json")["projects"]
    queues = read(args.evidence / "review-queues.json")["projects"]
    readiness = read(args.evidence / "local-readiness.json")
    require(real.get("status") == "PASS" and readiness.get("loginCount") == 14, "Real upload and login evidence incomplete")
    codes = {"DKD", "DS2"}
    require(all(set(r["projects"]) == codes for r in (real, demo, offline)), "Evidence must cover both projects")
    require(all(str(p.get("status", "")).startswith("PASS") for p in demo["projects"].values()),
            "DEMO evidence incomplete")
    for code, count in (("DKD", 3), ("DS2", 2)):
        repair = normal[code]
        require(len(fields[code]) == count and all(m["status"].upper() == "COMPLETED" for m in fields[code])
                and len(queues[code]) == 5 and repair["state"] == "Confirmed"
                and repair["defectId"] == real["projects"][code]["pmReview"]["defectId"]
                and offline["projects"][code]["status"] == "PASS" and offline["projects"][code]["replay"],
                "Incomplete field, Normal, review queue or offline evidence for " + code)
    config, images = portable_compose(args.compose, args.project)
    output = args.output.resolve()
    require(not output.exists(), "Output already exists")
    output.parent.mkdir(parents=True, exist_ok=True)
    manifest = read(HERE / "manifest.json")
    with tempfile.TemporaryDirectory(dir=output.parent, prefix=".rg-package-") as tmp:
        stage = Path(tmp) / "bundle"
        stage.mkdir()
        assets(args.assets, stage / "assets", manifest)
        for filename in DRIVERS:
            copy(HERE / filename, stage / "drivers" / filename)
        for code, project in demo["projects"].items():
            clip = "1123-part-00" if code == "DKD" else "11234"
            for kind in ("before", "after"):
                name = clip + "-" + kind + ".jpg"
                image = args.evidence / "demo-images" / name
                require(digest(image) == project["photoSha256"][kind], "DEMO image checksum mismatch")
                copy(image, stage / "demo-images" / name)
        for filename in ("SeedOffline.dll", "SeedOffline.deps.json", "SeedOffline.runtimeconfig.json"):
            require((args.offline_publish / filename).is_file(), "Missing published .NET driver " + filename)
        for item in args.offline_publish.rglob("*"):
            if item.is_file() and (item.suffix in {".dll", ".so", ".dylib"} or item.name in
                                   {"SeedOffline.deps.json", "SeedOffline.runtimeconfig.json"}):
                copy(item, stage / "offline" / item.relative_to(args.offline_publish))
        write(stage / "compose.json", config)
        write(stage / "images.json", images)
        copy(Path(__file__), stage / "package_local.py")
        copy(HERE.parent / "REAL-AI-FE-HANDOFF.md", stage / "HUONG-DAN-FE.md")
        inference = {}
        for code, row in real["projects"].items():
            result = row["inference"]
            inference[code] = {"detections": result["detectionCount"], "telemetryStatus": result["telemetryStatus"],
                               "resultSha256": result["rawResult"]["sha256"], "modelVersionId": result["rawResult"]["modelVersionId"]}
        summary = {"runtime": readiness, "realAi": real["status"], "modelSha256": manifest["modelSha256"],
                   "inference": inference, "demo": demo["status"], "documentedGaps": demo.get("gaps", []),
                   "offlineProjects": sorted(offline["projects"]), "stage": "LOCAL_CANDIDATE",
                   "completedRealAiMeasurements": sum(map(len, fields.values())),
                   "confirmedRealAiNormalRepairs": len(normal),
                   "pendingPmMeasurements": sum(map(len, queues.values())),
                   "pendingSupervisorProposals": sum(map(len, queues.values()))}
        write(stage / "acceptance.json", summary)
        (stage / "README.md").write_text(
            "# RoadGuard FE local candidate\n\nUse Docker Desktop on Windows with Linux containers, Python 3.11+ "
            "with requests installed and the ASP.NET Core 8 runtime. The project uses loopback ports "
            "38081 (BE), 39001 (objects) and 38026 (Mailpit).\n\n"
            "Run from this folder in PowerShell.\n\n```powershell\n"
            "python package_local.py replay --work ../rg-fe-data\n```\n\n"
            "The command verifies all checksums, loads the six images, starts Docker, provisions fourteen local "
            "accounts and replays the video, Reporter, PM, Supervisor, Crew and offline workflows. "
            "Use the same work directory for subsequent runs. It holds private credentials, journals and receipts. "
            "Open rg-fe-data/private/TAI-KHOAN-DOCKER-LOCAL.md for the local accounts. "
            "Fresh volumes generate new credentials and workflow IDs. Real candidate IDs and frame IDs "
            "for each project are in rg-fe-data/real-flow.json. See acceptance.json for verified coverage "
            "and remaining FastTrack requirements.\n\n"
            "To stop while keeping your data run `docker --context desktop-linux compose "
            "-p rg-fe-real-replay -f compose.json stop`.\n", encoding="utf-8")
        for entry in images.values():
            docker("image", "tag", entry["id"], entry["tag"])
        docker("image", "save", "-o", stage / "images.tar", *(item["tag"] for item in images.values()))
        checks = {p.relative_to(stage).as_posix(): digest(p) for p in sorted(stage.rglob("*")) if p.is_file()}
        write(stage / "checksums.json", checks)
        stage.rename(output)
    print("Package checksum manifest SHA256", digest(output / "checksums.json"))


def replay(args):
    try:
        import requests
    except ImportError:
        raise SystemExit("Install the Python requests package before replaying the seed.") from None
    work = args.work.resolve()
    require(not work.is_relative_to(HERE), "Choose a work directory outside the portable package")
    for name, sha in read(HERE / "checksums.json").items():
        file = (HERE / name).resolve()
        require(file.is_relative_to(HERE) and digest(file) == sha, "Package checksum mismatch: " + name)
    docker("image", "load", "-i", HERE / "images.tar")
    for service, item in read(HERE / "images.json").items():
        require(docker("image", "inspect", "--format", "{{.Id}}", item["tag"]) == item["id"],
                "Loaded image ID mismatch: " + service)
    compose = ["compose", "-p", PROJECT, "-f", HERE / "compose.json"]
    private = work / "private"
    private.mkdir(mode=0o700, parents=True, exist_ok=True)
    if os.name == "nt":
        user = subprocess.check_output(["whoami"], text=True).strip()
        subprocess.run(["icacls", str(private), "/inheritance:r", "/grant:r", user + ":(OI)(CI)F"],
                       check=True, stdout=subprocess.DEVNULL)
    creds = private / "demo-accounts.json"
    docker(*compose, "up", "-d", "--no-build", "--pull", "never", "--wait", "--wait-timeout", "360")
    new = private / "new-accounts.json"
    docker("cp", PROJECT + "-init-1:/state/demo-accounts.json", new)
    try:
        if creds.exists():
            require(digest(creds) == digest(new), "Work directory belongs to different Docker volumes")
        new.replace(creds)
        creds.chmod(0o600)
    finally:
        new.unlink(missing_ok=True)
    shutil.copytree(HERE / "demo-images", work / "demo-images", dirs_exist_ok=True)
    api = "http://127.0.0.1:" + str(PORTS["api"])
    drivers = HERE / "drivers"
    def run(name, *options):
        subprocess.run([sys.executable, str(drivers / name), *map(str, options)], check=True)
    run("check_local.py", "--api", api, "--mailpit", "http://127.0.0.1:" + str(PORTS["mailpit"]),
        "--credentials", creds, "--receipt", work / "local-readiness.json")
    real_flow_args = ("--api", api, "--storage", "http://127.0.0.1:" + str(PORTS["minio"]),
                      "--compose-project", PROJECT, "--credentials", creds, "--assets", HERE / "assets",
                      "--output", work / "real-flow.json")
    run("real_flow.py", *real_flow_args)
    run("real_field.py", "--api", api, "--storage-port", PORTS["minio"], "--work", work, "--media", HERE / "assets" / "media")
    run("demo_flow.py", "--origin", api, "--storage-port", PORTS["minio"], "--work", work, "--media", HERE / "assets" / "media")
    run("check_offline.py", "--api", api, "--work", work, "--dll", HERE / "offline" / "SeedOffline.dll")
    run("review_queues.py", "--api", api, "--storage-port", PORTS["minio"], "--work", work, "--media", HERE / "assets" / "media")
    run("real_flow.py", *real_flow_args, "--phase", "verify")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_subparsers(dest="mode", required=True)
    export = modes.add_parser("build")
    export.add_argument("--compose", type=Path, required=True)
    export.add_argument("--project", default="rg-real-fe-check")
    for name in ("assets", "offline-publish", "evidence", "output"):
        export.add_argument("--" + name, type=Path, required=True)
    modes.add_parser("replay").add_argument("--work", type=Path, required=True)
    args = parser.parse_args()
    if args.mode == "build":
        build(args)
    else:
        replay(args)


if __name__ == "__main__":
    main()
