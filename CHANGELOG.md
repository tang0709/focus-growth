# Changelog

## V0.4 — Fix false future-time errors when moving existing actual blocks
- Fix the actual-time editor rejecting upward repositioning, shortening, or state-only changes when an already recorded input-backed ten-minute slot ends slightly later than the current clock time.
- For edits of an existing actual block on the current date, validate *only newly introduced future minutes*: retain or shorten an existing slot's future-facing tail if the revised end is no later than its original end; reject any extension that goes beyond both the existing end and current time. Future calendar dates remain blocked.
- This does not grant an additional ten minutes of future activity credit and does not change how keyboard/mouse activity is grouped into ten-minute intervals. Existing overlap protection, task attribution, duration updates, and audit-history behavior remain intact.
- Confirmed syntax, 17 focused time-edit/overlap/future-date tests and one simulated upward mouse drag against a 04:20–04:30 slot at 04:25. Real-browser acceptance remains pending.

## V0.4 — Ctrl+Z undo and redo
- Add Ctrl+Z (Cmd+Z on macOS) to undo the most recent explicit workspace edit, with a bounded stack of up to 20 operations. Add Ctrl+Y / Ctrl+Shift+Z for redo and visible Undo/Redo controls in the day timeline. Preserve native text-field undo while a form input or editable control has focus.
- Capture user actions before pointer or keyboard activation and commit a history entry only after the revised state is saved. Covered collections include tasks, projects, plans, actual activity, notes, goals, theme, quadrants and archived-task visibility. The undo record stays in this browser tab memory rather than introducing another persistent copy of private records.
- Never treat passively collected browser or desktop input activity as a manual undo step. Do not rewind running timers or monitor sessions. Restore only the collections changed by the manual action, validate before applying and verify local persistence; stop instead of silently overwriting unexpected state.
- Clear stale undo history when remote task sync applies a differing state. Prevent undo/redo from removing a previously synced or in-flight project, task or plan when it would violate the continuous sync no-deletion rule. Notify the existing scoped sync mechanism for supported task/project/plan reversals. Raw activity remains browser-local.
- Include archive and restore actions in manual undo history; user action checkpoints also cover plan repositioning, actual record editing and deletion. A refresh resets the in-memory undo stack. Source parsing and isolated history, 20-step bound, stale-data, archive, timer safety, original-deletion-restoration, redo and plan-move simulations passed. Live browser acceptance remains pending.

## V0.4 — Actual record delete and edit button repair
- Fix actual-work editor Save and Delete buttons whose inline onclick values contained unescaped JSON double quotes, truncating the HTML event handler and making buttons unresponsive. HTML-escape the quoted date argument so the DOM decodes it to an executable JavaScript string.
- Replace browser-native confirm on record deletion with a visible inline confirmation panel, cancel action, and focus/scroll for accessibility. Do not delete anything on the initial click.
- On confirmation, verify original activity fragments and boundaries, require local storage freshness, validate the deletion draft, retain the removed fragments in browser-local deletion history, recalculate associated task actual duration while preserving completion and plan data, and verify persisted output before reporting success.
- Guard previously deleted task intervals against automatic completion-timeline recovery. Scoped task-duration changes may sync through the existing connector, while raw activity and deletion history remain local.
- Confirmed fifteen isolated deletion-draft assertions, nine confirmation/cancel/persistence simulations, JavaScript syntax, and HTML attribute decoding for both buttons. Live browser acceptance pending. No user task records were deleted during development.

## V0.4: Repeated plan assignment and actual editor reliability
- A plan already recorded once can be explicitly reassigned to a different unassigned actual block. The previous task association is cleared but its original activity, date, duration and state remain, so work minutes are not duplicated.
- Dragging onto a nearby unassigned block has a conservative six-minute snap range. Other tasks' associations are not overwritten.
- Actual-block pointer gestures capture on the edited block, and live timeline redraw is deferred during a drag.
- Editing actual start, end, task or quadrant now verifies browser persistence before reporting success. Validation and save errors remain visible in the editor.
- Syntax and isolated regression simulations passed. Live browser acceptance is still pending. Private task and activity records were not changed in this code fix.

## V0.4 — Drop onto unassigned actual work and move planned blocks
- Fix plan-to-actual dragging to resolve its target from the pointer's exact time against browser-local activity, not from the HTML drag/drop event target. Dropping onto an unassigned input-backed or manually adjusted block now assigns the planned task to that existing continuous block, inherits its entire time and quadrant state, and adds no new work minutes. Existing task associations remain protected.
- Use pointer-based dragging for the body of each plan block. Move up or down within the plan lane to reschedule its start while retaining duration, or move across to the actual lane to confirm work. A short click retains plan editing; existing plan selection mode and edge resizing remain available.
- Preserve the original plan when assigning it to actual time. Task actual minutes are derived from existing local activity, with completion updated only when all related plan occurrences are accounted for. Changes pass through the existing scoped sync notification without synchronizing raw activity.
- Source parsing, ten isolated existing-block assignment assertions, eleven simulated pointer gesture assertions, and final integration checks passed. Production-browser acceptance is not yet confirmed.

## V0.4 — Inherit existing work-state blocks and adjust actual placement
- Drag a planned event onto an existing actual work block to adopt the existing block's complete time range and quadrant state, including focus/投入区, without duplicating or changing previously measured activity. Associate the task, derive its actual minutes from the local actual timeline, and mark a leaf task complete only when all associated plan occurrences are accounted for.
- Allow users to reposition an existing actual work block by dragging its body; drag its top or bottom edge to adjust duration. Use five-minute snapping by default, Alt for one-minute precision, and Shift-drag for the existing multi-block selection. A single click opens a precise start/end-time editor.
- Prevent accidental overlap and future-time recording. Keep existing plans unchanged, preserve associated state and task, distinguish manually adjusted input evidence, and save original activity fragments in local adjustment history so provenance remains available.
- Update task actual-duration changes through existing scoped V0.4 sync hooks. Activity detail, original evidence, adjustment history, and quadrant state remain browser-local, not published to the public code repo or remote task sync.
- JavaScript syntax, 20 isolated transformation checks, and source-integration checks passed; live browser drag/drop acceptance remains pending.

## V0.4 — Drag plans directly into actual timeline
- In the side-by-side day timeline, drag a plan block onto the actual lane. Use the drop position as the actual start time, retaining the planned duration, while leaving the original plan untouched.
- Treat this as an explicit, user-confirmed manual actual entry (not proof of keyboard or mouse activity). Use the existing neutral light gray; add minute-accurate slices within ten-minute slots without exceeding the current time.
- Reject overlap with other or unassigned activity, prevent repeated conversion of the same plan, and preserve records already associated with the same task. Mark the task complete only after all its planned occurrences have actual coverage or explicit drop receipts, and do not finish a currently running task timer.
- Route permitted task completion and actual-duration fields through the existing scoped sync hooks. Activity, plan-drop receipts and current work state remain browser-local.
- JavaScript syntax and nineteen isolated execution-draft regression checks passed. Live browser acceptance remains pending.

## V0.4 — Smooth seconds on work monitor
- Show the running work monitor's elapsed session time in HH:MM:SS with second-by-second ticks instead of a clock frozen on ten-minute work-credit increments.
- Display valid input-backed activity time separately as ten-minute credited blocks. Unobserved periods never create activity records, even though the wall-clock monitor continues running.
- Preserve existing browser-local work history, authorized sync scope, task-timer behavior, and desktop activity collection. JavaScript syntax and eight isolated clock/accounting checks passed; live browser acceptance pending.

## V0.4 — Hide empty task groups
- Hide project and independent-task headers whenever the current task tree contains no visible descendants, including after all child tasks have been archived or filtered out by search. Collapsed project headings remain visible if any matching, unarchived child exists. No tasks, archives, projects, timers, or sync records are deleted.
- JavaScript syntax and eight isolated task-tree regression checks pass. Live browser acceptance remains pending.

## V0.4 — Explicit retrospective actual repair
- Add a workbench button "补齐回溯实际" that invokes the existing authorized V0.4 sync and then imports any newly arrived private retrospective estimate plans into browser-local actual records.
- Report how many tagged plans are present, already imported, recorded locally, and skipped because of existing overlapping activity. If private plans are absent locally, show a clear sync/revision/conflict diagnostic instead of falsely reporting success.
- Preserve the native sync authorization gate, read-only states, existing activity, import idempotency, private data boundaries and historic schedules. The six real private plans were re-tested with an isolated local-state simulation: 270 estimated minutes on November 23 and 180 on November 24, 450 total, with no duplicates on a second import. This does not verify a user's live browser state.

## V0.4 — Private retrospective timeline estimates
- Add a generic, private-data-triggered retrospective estimator for explicitly tagged completed-task plans. The public source never contains personal task names or historical dates; private `sync/state.json` holds the task and plan data.
- On a verified incoming sync, materialize only tagged plans into browser-local actual timeline records, preserving all existing activity. Use `source=retrospective-estimate`, mark the blocks as approximate with dashed borders, retain either neutral gray or the user-selected focus quadrant, and record idempotent local import history and skipped overlaps.
- Daily, weekly, monthly and yearly statistics label the portion of total recorded time that is retrospective estimation rather than observed input. No activity details are added to the authorized GitHub sync projection.
- The user authorized approximating schedule intervals for November 23 and November 24, 2026. Exact hours are illustrative, not independently verified. Existing plans, measured activity, timers and privacy data remain unchanged until the browser reads the new projection. Seven mocked regression checks and JavaScript parse check pass; live browser acceptance pending.

## V0.4 — Input-backed work accounting and completed visa milestones
- Mark remaining four PhD visa material tasks complete on the user's approximate 2026-11-24 completion date in the private task repository. Actual work durations and exact completion timestamps remain unasserted. Previous plans remain available for review.
- Stop recording unattended wall time as work when an independent work session remains active. Record trusted keyboard, pointer-button and wheel activity in ten-minute blocks within the visible webpage; no passive pointer-movement counting or modifier-key-only counting. Clicking dashboard controls is not regarded as work.
- Restrict task timer actuals to evidence-backed activity; the seconds counter remains a wall-clock reference. Preserve the manual old-timer recovery path for explicitly supplied historical times.
- Add opt-in, authenticated Windows local activity collection using GetLastInputInfo and DPAPI-encrypted calendar-slot evidence (no typed text or key identities). It covers activity in other Windows applications while the trusted companion is armed. Raw activity and quadrant states stay out of the private GitHub data projection.
- WARNING: Windows source changes require a local native rebuild and explicit real-machine validation. The previously running native EXE is unchanged. Earlier unconditional `work-session` records remain intact but are not verified activity.

## V0.4 — Task timer actual records
- Persist actual task intervals when pausing, stopping, switching or completing a task, with task identity and state, minute-resolution activity, cumulative seconds, and non-overwrite overlap protection. Distinguish stopping from completion; reveal saved records and offer explicit old-timer recovery with user-supplied times. Sync only allowed task duration/completion fields; raw timer and activity stay local. Syntax and 16 focused mocked checks passed before final integration; browser acceptance pending.

## V0.4 — Completion-based timeline entries
- On explicit task completion, infer an interval from the preceding same-day actual task or task timer, update a sole plan or add one, and record minute-precision local actuals. Preserve other tasks and occupied activity; allow nonoverlapping partial slots, update statistics and editing ranges, and reconcile synchronized completions. Syntax and 14 focused mocked checks passed; live browser acceptance pending. Activity remains local.

## V0.4 — Direct plan execution
- Remove redundant confirmation dialogs from full and selected plan execution. Keep validation, stale-storage protection, elapsed-time limits, non-overwrite behavior and result notifications. Syntax and six mocked checks pass; live browser acceptance pending.

## V0.4 — Selective plan execution
- Add an explicit plan marquee mode; confirm only selected plan IDs into actual activity, using the existing light-gray plan-confirmed style and sync hook. Preserve original plans, unselected tasks, existing activity and normal drag/resize outside selection mode. Syntax and ten mocked checks pass; live browser acceptance pending.

## V0.4 — Independent statistics periods
- Add day, week, month and year analysis with independent date controls, previous/next period navigation, locally remembered selection and in-page drilldown. Scope activity metrics, trends, quadrant and task category totals to the selected period; distinguish all-history estimates. Preserve original workbench date and task data. Syntax and 14 mocked regression checks pass, including leap-year and cross-year ranges; live browser acceptance pending.

## V0.4 — Independent work-session recording
- Start work immediately without selecting a task. Show a live dashed gray preview and persist unassigned actual activity in existing ten-minute slots, including cross-midnight sessions and reload recovery. Allow later assignment via existing actual-block editor. Preserve occupied slots, task timers and local privacy. Syntax and 11 mocked checks pass; live browser acceptance pending.

## V0.4 — Actual timeline marquee selection
- Drag a mouse selection rectangle in the actual timeline to select entire intersected activity blocks and assign a shared state. Preserve individual click editing and all other task and activity fields; reject stale selections. State records remain browser-local. Syntax and 11 mocked regression checks pass; live browser acceptance pending.

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

