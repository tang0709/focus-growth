# Changelog

## V0.4 — Continuous sync, separately gated production
- Added five-second conditional polling, enable/pause, DPAPI offline queues, stable-ID field-level three-way merge, SHA conditional writes and operation deduplication. Production starts read-only and requires later explicit user approval through a separate native grant flow.
- Preserved local history, notes, theme, quadrant, timers and ten-minute statistics; checkpoints and stale-tab guards protect incoming changes. Same-field conflicts require a choice; deletion propagation is blocked.
- Validation: 85 automated checks passed. Actual isolated browser/GitHub tests verified both directions, transport outage/recovery, explicit conflicts and running-timer preservation. Personal reports and backups remain local.

## V0.3.1 — Scoped initial migration and readback
- Added a separate native one-time initializer. It validates snapshot-specific approval and encrypted backup/preview bindings, consumes a durable DPAPI approval record, then performs one fixed-path SHA-conditional initialization and exact readback. Existing production write locks remain intact; no force flag, general update endpoint or automatic retry exists.
- Added explicit paired remote readback in the normal webpage. It compares only protocol fields, displays revision/SHA/counts and never overwrites local data. Initialization does not enable continuous production writes.
- Validation: 104 local checks passed: initializer22, native25, preparation27, frontend13, asynchronous7, readback/preparation frontend10. Actual private initialization receipts, browser evidence, approval records and backups remain strictly local; no task contents are included in this repository.

## V0.3.1 — Production preparation only
- Added explicit read-only preparation pairing in the normal Pages workspace, encrypted local backups with restore validation, and a local-only migration difference preview. No production initialization or real-data upload is enabled.
- Enforced the production write lock in both native HTTP routes and GitHub transport. The executable cannot issue first-sync approval; a future approval must bind a fresh preview, backup and local/remote snapshot.
- Fixed edits being lost while a previous sync request is pending: keep the sent delta immutable, persist subsequent edits and promote them after acknowledgement. New local edits invalidate an existing preparation preview.
- Preserved the original app schema, storage key, UI, task timer, ten-minute statistics and import/export. Empty remote state never deletes local tasks or reschedules past plans.
- Validation: 89 local checks passed, including 27 preparation core/HTTP checks and 8 preparation frontend checks. Personal backup, preview and actual browser evidence remain local and excluded from Git and packages.

## V0.3 — Synthetic private sync preview
- Added a Windows Credential Manager backed loopback companion, explicit native pairing and short-lived origin-bound sessions.
- Added field-level projects/tasks/plans merge, SHA-conditional writes, operation IDs, DPAPI-backed offline queue and explicit per-field conflict choices with backups.
- Added isolated browser test storage, status and manual sync. Normal browser data stays local and production sync/state.json writes remain locked.
- Validation: 53 local checks passed; actual local Chrome tests confirmed bidirectional tasks/projects/plans, proportional plan height, timer pause/resume/stop and manual completion without uploading timer fields or fabricating actual work.
- GitHub Pages deployment succeeded. Actual Chrome 158.0.8127.37 testing confirmed explicit Pages-origin pairing, receipt of private synthetic data, and a Pages project edit written back to the isolated private fixture. No browser security controls were disabled; keep the reviewed local UI as a fallback.

## V0.2
- Migrated the existing standalone HTML prototype to index.html without changing its content.
- Preserved second-by-second task timer and separate ten-minute activity statistics.
- Windows global activity monitoring and live AI backend remain future features.

