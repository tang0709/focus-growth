# Focus & Growth V0.4 Windows companion

Continuous projects/tasks/plans synchronization is implemented. Production starts **paused and without write authorization**. Pairing alone only permits encrypted local backup and read-only comparison. This release does not issue production consent.

## Run

Use Windows 10/11, .NET Framework 4.x and the same Windows account that owns the existing Generic Credential `FocusGrowthSync/GitHub`. Never paste a GitHub token into the website, a command, or a configuration file.

- Run `FocusGrowthSyncV04.exe --production-readonly` (or double-click). Open the existing https://tang0709.github.io/focus-growth/ page and its V0.4 sync panel. Pair using the temporary native code, confirm the exact Pages origin in Windows, then use read-only verification. Production port: 18421.
- For disposable tests only: run `FocusGrowthSyncV04.exe --synthetic-v04`; open `http://127.0.0.1:18420/?syncTest=1`. Pair, then add the clearly marked virtual sample. The browser uses `start-workbench-sync-test-v04`, and GitHub uses a device-specific `sync/tests/v04-<device>.json`. Production data is never used as a test fixture.
- Native Pause stops write processing while keeping the encrypted queue. Browser Pause also stops incoming application. Closing/restarting the companion starts paused. Keep one paired tab per workspace. Pairing sessions expire after 15 minutes; New pairing code pauses the service and revokes previous sessions.
- `build.cmd` compiles the explicit source list using the Windows Framework compiler. `FocusGrowthSyncV04.exe --selftest` runs synthetic tests without GitHub credentials and writes count-only test results beside the executable. `node tests/frontend.cjs`, `node tests/frontend-async.cjs`, `node tests/preparation-frontend.cjs`, and `node tests/continuous-frontend.cjs` run the JavaScript tests (Node required for development only).

This is a user-session Windows companion, not an installed Windows SCM service. No startup entry, OS-wide activity monitoring, firewall change, browser security bypass, or credential prompt is installed.

## Data and safety

Only the existing protocol's allowed project/task/plan fields can cross the private repository boundary. Full local browser backups and queues are DPAPI CurrentUser encrypted under `%LOCALAPPDATA%/FocusGrowthSync/v04-production` or `v04-synthetic`. Keep the same Windows profile and retain these directories. Full restore is a separate, explicitly confirmed operation; do not overwrite browser data to test recovery. Each saved checkpoint is decrypted and digest-verified without overwriting the source.

Field-level three-way merge uses stable IDs, an immutable in-flight operation and durable subsequent edits. Writes use the current GitHub blob SHA and unique operation IDs. Same-field conflicts require an explicit choice; deletions are blocked. Empty, uninitialized, regressed or unsupported production snapshots cannot clear local records. Local activity, history, task timer, notes, theme and quadrant state remain in the browser; plan intervals never become actual work records. Completion changes for a running timer are blocked until reviewed.

The native program polls approximately every five seconds using conditional reads. The visible paired page checks the native cache every five seconds; browser background throttling can delay display. Offline and transient failures back off, preserve operations and retry. Permission/rate-limit errors are not bypassed by repeated manual sync. The test network buttons inject transport failures only; they do not disable Windows networking. A long outage/expired session may require pairing again before browser delivery.

Exact Origin/Host checks, loopback-only listeners and short-lived memory-only pairing sessions protect the local API. The GitHub PAT is read only by the native transport and never returned. A compromised Windows user account or malicious code on the trusted website remains outside these protections.

## Later production activation — requires new explicit approval

Do not perform these steps during the V0.4 test/preparation phase.

1. Obtain explicit user authorization for ongoing allowed-field production writes. Confirm the current real browser profile, local backup recovery proof, remote private identity, and exact read-only baseline. Resolve differences through a new reviewed preview; never force a baseline.
2. A local operator creates a **local-only**, four-field consent manifest: `consentReference`, `expectedRemoteSha`, `expectedProjectionDigest`, `expectedBackupSha256`. Use the current verified native attach backup and protocol projection digests. Do not include a PAT or task contents. Never publish the manifest.
3. Run `FocusGrowthSyncV04.exe --authorize-production <absolute-manifest-path>`. This separate native entry validates the encrypted backup and remote baseline. The user must check the initially unchecked scope checkbox and press the grant button. There is no HTTP grant endpoint or force flag.
4. The DPAPI grant is scoped to this device, the private repository, `sync/state.json` and allowed fields, expires after seven days, and leaves the service paused. Start/re-pair the companion, verify baseline, then explicitly Enable / Resume. Expiry stops writes. Pause is available in both native and web UI; pausing preserves queued work.

The previous one-time migration authorization cannot enable this flow. Never initialize the already initialized remote again. Keep synthetic fixtures separate from production. Shared reports and Git commits must contain only generic code, counts and digests.
