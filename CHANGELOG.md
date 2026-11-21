# Changelog

## V0.4 — Nested task folding and alignment
- Replace static child-task arrows with independently collapsible, persistent branches. Use fixed-width marker and checkbox columns with uniform indentation. Preserve task history, scheduling, completion, archiving and sync. Syntax and eight mocked checks pass; live browser acceptance pending.

## V0.4 — Flat project order controls
- Separate add-task button from a compact two-row ordering control of matching height. Preserve existing project ordering and add-task behavior. Syntax and seven mocked checks pass; live browser acceptance pending.

## V0.4 — Project title rename
- Double-click a project name (or press F2 when focused) to open a name-only editor with selected text and Enter-to-save. Toggle collapse in place to retain double-click target; preserve project IDs, other fields, hierarchy and sync hooks. Syntax and five mocked checks pass; live browser acceptance pending.

## V0.4 — Confirm full plan execution
- Add explicit selected-day plan confirmation, copying ended plans into local ten-minute actual activity records in neutral light gray by default. Preserve existing activity, current timers and entered actuals; do not mark partially covered, future or running tasks newly complete. Completion and actual fields use existing sync hooks, activity remains local. Syntax and ten mocked checks pass; live browser acceptance pending.

## V0.4 — Compact project controls
- Align move-to-top, add task and move-to-bottom in a single vertical control column; keep arrows visually subtle and preserve boundary disabling. Syntax and seven mocked checks pass; live browser acceptance pending.

## V0.4 — Completed tasks last
- Show unfinished tasks before completed tasks in daily lists, optional tasks and each project-tree sibling group. Preserve relative order within each status, hierarchy, schedules, archives and underlying data. Syntax and eight mocked regression checks pass; live browser acceptance pending.

## V0.4 — Project display order
- Add compact per-project move-to-top and move-to-bottom buttons in the task tree, with boundary disabling and persistent browser-local order. Preserve independent tasks, collapse state, archiving, underlying data and sync. Syntax and nine mocked regression checks pass; live browser acceptance pending.

## V0.4 — Project group collapse
- Make project arrows and names functional keyboard-accessible expand/collapse controls; persist per-group state in browser-local storage. Preserve archiving, task records, progress and sync. Syntax and eight mocked regression checks pass; live browser acceptance pending.

## V0.4 — Completed task archive
- Add one-click archive and searchable project-grouped archive dialog with individual and bulk restore. Store visibility preferences locally; preserve completion, hierarchy, plans, activity, timers and progress. Incomplete tasks and completed parents with incomplete descendants remain visible. Syntax and seven mocked regression checks pass; live browser acceptance pending.

## V0.4 — Resizable timeline viewport
- Add a bottom vertical resize grip with keyboard support, double-click reset and locally remembered height. Preserve the 24-hour time scale and task durations. Script syntax and five mocked height checks pass; real browser acceptance pending.

## V0.4 — Neutral plan timeline colors
- Give planned blocks neutral gray fill, border and text independently of the four actual-activity state colors. No task data or sync changes.

## V0.4 — Optional daily maintenance and local retrospective entry
- Support daily optional task templates using existing category and stable-ID fields; create a dated instance only when requested, exclude optional work from core targets and project completion metrics.
- Add reviewed, locally stored activity backfill from completed project plans over a selected date range. Preserve existing activity slots, exclude future slots, require manual confirmation, and retain protocol privacy boundaries.
- Script syntax, mocked daily-instance deduplication and activity slot mapping verified. Actual browser acceptance remains pending.

## V0.4 — Date navigation fix
- Clarify the today navigation button: highlight Today only for the actual local date; otherwise show Return to Today.
- Match selected-date task and overview labels and week/month headings to the viewed period, preserving actual-today activity counters.
- Validated script syntax and four mocked date-render cases covering today, yesterday, tomorrow and a previous month. Browser acceptance pending.

## V0.4 — Permanent trusted devices
- Permanent, revocable Windows/browser trust using non-exportable browser signing keys, DPAPI public-key registration and single-use signed challenges. Short-lived memory-only bearer tokens automatically renew; no PAT or permanent bearer reaches browser storage.
- Added reconnect, saved enable/pause preference, system tray, trusted-browser management, native/web revocation and opt-in Windows sign-in startup.
- Preserved existing protocol whitelist, backups, offline queues, conflict checks and deletion protection. New explicit native confirmation grants permanent production scope; prior migration consent is not reused.
- User requested formal activation before remaining pause/restart and old-credential revocation UI demonstrations; these remain deferred. Synthetic write tests never change production tasks.

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

