"""Generate full target files and unified diffs from checked source snippets."""

import difflib
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RF05 = ROOT / "planning/refactor/rf05"


def replace_once(content, old, new, path):
    if content.count(old) != 1:
        raise ValueError(f"{path}: expected one source snippet, found {content.count(old)}")
    return content.replace(old, new, 1)


def build():
    targets = {}
    generator = "planning/refactor/tools/build_rf04_sources.py"
    old = (ROOT / generator).read_text(encoding="utf-8")
    new = replace_once(old, "import json\n", "import json\nimport os\nfrom rf05_readiness import apply_readiness\n", generator)
    new = replace_once(new, "ROOT = Path(__file__).resolve().parents[3]",
                       'ROOT = Path(os.environ["ROADGUARD_ROOT"]).resolve() if os.environ.get("ROADGUARD_ROOT") else Path(__file__).resolve().parents[3]', generator)
    new = replace_once(new, 'cwd=ROOT, text=True).strip()', 'cwd=Path(os.environ.get("ROADGUARD_GIT_ROOT", ROOT)), text=True).strip()', generator)
    new = replace_once(new, '    write_product(groups["fr"] + groups["br"],',
                       '    apply_readiness([row for group in groups.values() for row in group], ROOT, PLAN / "04-readiness-decisions.json", PLAN / "04-source-crosswalk.json")\n    write_product(groups["fr"] + groups["br"],', generator)
    new = replace_once(new, '        output += [f"Review gate: {row[\'missing_decision\']}. Blocked: {row[\'blocked_checkpoint\']}.", ""]',
                       '        for approved in row["readiness"].get("approved_scopes", []):\n            output += [f"Confirmed implementation scope ({approved[\'decision_id\']} / {\', \'.join(approved[\'claim_ids\'])}): {html.escape(approved[\'scope\'])}. Historical excerpt text remains unconfirmed outside this scope.", ""]\n        if row["readiness"]["state"] == "READY":\n            output += [f"Ready for scoped implementation. Planned checks (not executed proof): {\'; \'.join(row[\'readiness\'][\'required_evidence\'])}.", ""]\n        else:\n            output += [f"Review gate: {row[\'readiness\'].get(\'reason\', row[\'missing_decision\'])}. Checkpoint: {row[\'readiness\'].get(\'checkpoint\', row[\'blocked_checkpoint\'])}.", ""]', generator)
    new = replace_once(new, 'Authority: `{row[\'authority\']}`; confirmed overlap',
                       'Historical authority: `{row[\'source_authority\']}`; current authority: `{row[\'authority\']}`; confirmed overlap', generator)
    new = replace_once(new, '**{claim[\'id\']}** (`{claim[\'authority\']}`):',
                       '**{claim[\'id\']}** (origin `{claim[\'authority\']}`, current `{claim[\'current_authority\']}`):', generator)
    new = replace_once(new, '"planning/refactor/02-decision-register.md"]',
                       '"planning/refactor/02-decision-register.md", "planning/refactor/04-decision-index.json", "planning/refactor/04-readiness-decisions.json", "planning/refactor/tools/rf05_readiness.py"]', generator)
    targets[generator] = (old, new)

    verifier = "planning/refactor/tools/verify_rf04.py"
    old = (ROOT / verifier).read_text(encoding="utf-8")
    new = replace_once(old, "import json\n", "import json\nimport os\nfrom rf05_readiness import validate_readiness\n", verifier)
    new = replace_once(new, "ROOT = Path(__file__).resolve().parents[3]",
                       'ROOT = Path(os.environ["ROADGUARD_ROOT"]).resolve() if os.environ.get("ROADGUARD_ROOT") else Path(__file__).resolve().parents[3]', verifier)
    new = replace_once(new, 'assert row["implementation_readiness"].startswith("BLOCKED_") and row["missing_decision"]',
                       'validate_readiness(row, ROOT)\n        assert row["implementation_readiness"] == row["readiness"]["state"]\n        registry = json.loads((PLAN / "04-readiness-decisions.json").read_text(encoding="utf-8"))["decisions"]\n        if row["id"] in registry:\n            assert row["readiness"] == registry[row["id"]]', verifier)
    new = replace_once(new, 'if related:\n            assert row["authority"].startswith("MIXED_")\n        else:\n            assert row["authority"] == "HISTORICAL_SOURCE_ONLY"',
                       'if related:\n            assert row["source_authority"].startswith("MIXED_")\n        else:\n            assert row["source_authority"] == "HISTORICAL_SOURCE_ONLY"', verifier)
    targets[verifier] = (old, new)

    target = "tests/Tooling/Verify-AgentSetup.ps1"
    old = (ROOT / target).read_text(encoding="utf-8")
    new = """[CmdletBinding()]
param ([switch]$SelfTest)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$argsList = @('planning/refactor/rf05/checks/verify_guidance.py', '--root', $repo, '--mode', 'all')
if ($SelfTest) { $argsList += '--self-test' }
python @argsList
if ($LASTEXITCODE -ne 0) { throw 'RF-05 guidance validation failed.' }
"""
    targets[target] = (old, new)

    target = "tests/Documentation/Verify-P102Docs.ps1"
    old = (ROOT / target).read_text(encoding="utf-8")
    new = """[CmdletBinding()]
param ([string]$RepoRoot = '', [switch]$SelfTestNegative)
$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path }
$argsList = @('planning/refactor/rf05/checks/verify_guidance.py', '--root', $RepoRoot, '--mode', 'docs')
if ($SelfTestNegative) { $argsList += '--self-test-negative' }
python @argsList
if ($LASTEXITCODE -ne 0) { throw 'RF-05 documentation validation failed.' }
"""
    targets[target] = (old, new)

    target = ".github/workflows/ci.yml"
    old = (ROOT / target).read_text(encoding="utf-8")
    new = replace_once(old, "      - run: pwsh -File tests/Documentation/Verify-P102Docs.ps1\n",
                       "      - uses: actions/setup-python@v5\n        with:\n          python-version: '3.11'\n      - run: pwsh -File tests/Documentation/Verify-P102Docs.ps1\n      - run: pwsh -File tests/Tooling/Verify-AgentSetup.ps1\n      - run: python planning/refactor/rf05/checks/test_readiness.py\n", target)
    new = replace_once(new, "      - run: python planning/refactor/rf05/checks/test_readiness.py\n",
                       "      - run: python planning/refactor/rf05/checks/test_readiness.py\n      - run: python planning/refactor/rf05/checks/test_guidance.py\n      - run: python planning/refactor/rf05/checks/test_repository_readiness.py\n      - run: python planning/refactor/rf05/checks/verify_repository_readiness.py --root .\n", target)
    targets[target] = (old, new)

    manifest = []
    for path, (before, after) in targets.items():
        output = RF05 / "patches/targets" / (path + ".draft")
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_bytes(after.encode("utf-8"))
        patch = "".join(difflib.unified_diff(before.splitlines(keepends=True), after.splitlines(keepends=True),
                                             fromfile="a/" + path, tofile="b/" + path))
        patch_path = RF05 / "patches" / (path.replace("/", "__") + ".patch")
        patch_path.write_bytes(patch.encode("utf-8"))
        manifest.append({"source": path, "target": path, "action": "replace",
                         "source_sha256": hashlib.sha256((ROOT / path).read_bytes()).hexdigest(),
                         "proposed_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
                         "draft": output.relative_to(ROOT).as_posix(),
                         "patch": patch_path.relative_to(ROOT).as_posix()})
    (RF05 / "patches/patch-manifest.json").write_text(json.dumps({"files": manifest}, indent=2) + "\n", encoding="utf-8")
    print(f"PATCHES {len(manifest)}")


if __name__ == "__main__":
    build()
