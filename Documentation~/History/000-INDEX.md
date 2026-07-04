# 000 — History Index

This folder keeps historical context in numbered, consolidated files.

## Numbering policy

| Range | Meaning |
|---:|---|
| 000 | History navigation. |
| 010 | Phase and roadmap history. |
| 020 | ADR history. |
| 030 | Guide history. |
| 040 | Cleanup/deletion history. |
| 050 | Consumer project separation cleanup. |

## Files

| File | Purpose |
|---|---|
| [`010-Phase-History.md`](010-Phase-History.md) | High-level project phase history and closed/superseded lanes. |
| [`020-ADR-History.md`](020-ADR-History.md) | Consolidated ADR list by phase/code. Detailed ADRs remain in `../ADRs/`. |
| [`030-Guide-History.md`](030-Guide-History.md) | Historical guide consolidation and replacement map. |
| [`040-Removed-Files.md`](040-Removed-Files.md) | Files intentionally removed from the active documentation surface. |
| [`050-Consumer-Project-Separation-Cleanup.md`](050-Consumer-Project-Separation-Cleanup.md) | Controlled cleanup report for package/QA/FIRSTGAME role separation. |

## Current-vs-history rule

- Use `../Current/` and `../Guides/Usage/index.html` for current usage.
- Use this folder only to understand why the framework reached its current shape.
- Use `../ADRs/` for detailed decision text when the summary is not enough.
