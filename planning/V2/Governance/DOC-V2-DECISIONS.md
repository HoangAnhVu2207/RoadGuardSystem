# DOC-V2-DECISIONS

## Task metadata

- Owner/branch: `anh` / `anh`
- Delivery status: `DONE`
- Started: `2026-09-28`
- Source: `RoadGuard_Prompt_Hop_Nhat_Sua_Docs_Ke_Hoach_V2_3 (2).md` sections B and appendices D/E/F.
- Scope: decision register, current requirement/flow/workshop/gap labels and trace; no runtime implementation.

## Acceptance criteria

- D01-D28 and 32-44 appear individually with source, statement, superseded label, business/contract status, remaining gate and affected tasks.
- Q02/Q04/Q17 and other answered questions are not presented as missing business decisions.
- Q03 method thresholds, wire schemas and runtime/provider verification remain separate gates.
- Historical snapshots remain historical; no old PASS/DONE evidence is rewritten.
- Documentation links/manifest/contract guards pass after edits.

## Delivery status

`DONE`

## Completion history

### 2026-09-28 - IN_PROGRESS

Đã đọc đầy đủ prompt nguồn, lập register từng quyết định và bắt đầu sửa các nhãn OPEN hiện hành. Chưa đánh `DONE` trước khi requirement/flow/workshop/gap và validation evidence đồng bộ.

### 2026-09-28 - DONE

- Đã sửa: register từng dòng D01-D28 và 32-44; appendix E/F authority; overview Q crosswalk; BR/FR/PF/SQ; UC/US/AC/UAT; policy/offline workshop; FE/auth/sync gaps; planning README.
- AC: 28/28 D rows và 13/13 configuration/ownership rows có source/status/gate/tasks; Q02/Q04/Q17 không còn business OPEN trong tài liệu hiện hành; Q03, wire schemas và runtime/provider verification vẫn tách riêng.
- Verification: row-count check `D_rows=28`, `config_rows=13`, no missing D; `check_contracts.py` PASS giữ hash `a98f43c662e574b37b2619b25e25b894419a0f0725c8af7e66c2c3c7e3143992`; `validate_package.py` `STRUCTURAL_CHECKS_PASS` với 229 links, 153 schemas, 133 operations, 8 positive/4 negative fixtures; `test_contract_guard.py` 7/7 PASS; manifest 67 files/0 mismatches; `git diff --check` không có whitespace error.
- Corrected issue: link register ban đầu đi lên thiếu một cấp từ V2 subfolders; validator fail đúng một link đầu tiên. Đã sửa toàn bộ link mới sang `../../../../planning/...` và rerun PASS.
- Chưa xác minh: method/material production thresholds; real video/SRT/route JSON bytes; provider/CRS/calibration; backend implementation; SQL/deployed schema; API/device/browser/performance/restore/UAT. Các mục này không bị ghi `VERIFIED`.
