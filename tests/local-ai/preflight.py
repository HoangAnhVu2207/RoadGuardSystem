"""Read-only local acceptance gate; never starts Docker or changes its storage."""
import argparse
import hashlib
import json
import shutil
import subprocess
from pathlib import Path

GIB = 1024 ** 3
CHECKPOINTS = {
    'weights/trained/RoadGuard_YOLO11_UAVPDD2023_FT95_v1.pt':
        'bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0',
    'weights/pretrained/yolo11m-road-seg.pt':
        '1874b4751102a17e697e5747180e5fda17134cbe3bf72c07c5aee5633213eae8',
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data-root', type=Path, default=Path('D:/RoadGuard-AI-LocalTest'))
    parser.add_argument('--video', type=Path, required=True)
    parser.add_argument('--docker-backing-path', type=Path,
                        help='Verified Windows directory containing Docker Desktop VHDX; do not guess.')
    args = parser.parse_args()
    blockers, checks = [], {}
    workspace = Path(__file__).resolve().parents[2]
    ai = workspace / 'AI/roadguard-ai-engine'
    root = args.data_root.resolve()
    # This harness is intentionally restricted to the owner-approved local drive.
    if root.drive.upper() != 'D:' or root == Path(root.anchor):
        blockers.append('UNAPPROVED_LOCAL_DATA_ROOT')
    else:
        free = shutil.disk_usage(root.anchor).free
        checks['data_disk_free_gib'] = round(free / GIB, 2)
        checks['incremental_budget_gib'] = 40
        checks['free_floor_gib'] = 30
        if free < 70 * GIB:
            blockers.append('INSUFFICIENT_LOCAL_DISK_BUDGET')
    for relative, expected in CHECKPOINTS.items():
        file = ai / relative
        valid = file.is_file() and not file.is_symlink()
        if valid:
            with file.open('rb') as handle:
                valid = hashlib.file_digest(handle, 'sha256').hexdigest() == expected
        checks[Path(relative).name] = 'HASH_VERIFIED' if valid else 'MISSING_OR_CHANGED'
        if not valid:
            blockers.append('CHECKPOINT_MISSING_OR_CHANGED')
    checks['video_present'] = args.video.is_file()
    if not checks['video_present']:
        blockers.append('VIDEO_MISSING')
    if shutil.which('docker') is None:
        blockers.append('DOCKER_CLI_MISSING')
    else:
        try:
            result = subprocess.run(['docker', 'info', '--format', '{{json .DockerRootDir}}'],
                                    capture_output=True, timeout=15, check=True, text=True)
            checks['docker_engine'] = 'AVAILABLE'
            checks['docker_root'] = json.loads(result.stdout)
            # Linux's path alone does not identify the Windows VHDX backing drive.
            backing = args.docker_backing_path
            if backing is None or not backing.is_dir():
                blockers.append('DOCKER_BACKING_DISK_CAPACITY_NOT_VERIFIED')
            else:
                backing_free = shutil.disk_usage(backing).free
                checks['docker_backing_free_gib'] = round(backing_free / GIB, 2)
                if backing_free < 70 * GIB:
                    blockers.append('INSUFFICIENT_DOCKER_BACKING_DISK_BUDGET')
        except (subprocess.SubprocessError, OSError, ValueError):
            checks['docker_engine'] = 'UNAVAILABLE'
            blockers.append('DOCKER_ENGINE_UNAVAILABLE')
    print(json.dumps({'status': 'BLOCKED' if blockers else 'CAPACITY_AND_INPUT_CHECKS_PASSED',
                      'checks': checks, 'blockers': sorted(set(blockers)),
                      'effects': 'READ_ONLY'}, indent=2))
    return 1 if blockers else 0


if __name__ == '__main__':
    raise SystemExit(main())
