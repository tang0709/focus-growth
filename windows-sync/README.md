# Focus & Growth V0.4 — trusted Windows companion

## Local input-activity monitoring (source update, rebuild required)

The Pages workspace now counts work only when an input-backed 10-minute activity slot exists. Clicking "Start work" alone does not count elapsed wall time as activity. The browser fallback observes trusted keydown, pointerdown and wheel events only while the page is visible. It does not observe other applications.

The Windows companion source adds `DesktopActivity.cs`, using the Windows `GetLastInputInfo` idle counter. It records only a calendar date and ten-minute slot when an input occurred. It does not log key identities, typed text, screen contents, application names or window titles. Records remain under the current Windows user's DPAPI-encrypted local storage and outside the GitHub task-sync projection. Collection is armed by an authenticated browser request, stops on request, and has a 12-hour arm expiry as a safety limit. Samples are collected every five seconds while armed even if the browser is backgrounded. This detects Windows-session keyboard **or mouse** input, not verified productive work.

New local routes: authenticated GET `/v1/activity`, authenticated POST `/v1/activity/arm` and `/v1/activity/stop` with empty JSON bodies. They retain the existing fixed loopback host, exact Origin, bearer session, trusted-device authorization and `X-FG-Client` checks. No remote write or new permission scope was added.

**Deployment and validation:** changes to C# do not update the previously running Windows executable. Review the diff, preserve encrypted backups and task data, run `windows-sync/build.cmd` on Windows, run `FocusGrowthSyncV04.exe --selftest`, and test first with `--synthetic-v04`. Verify idle for 20 minutes creates zero blocks; typing in another application for two separate 10-minute slots creates exactly two local blocks; closing the browser while the tray app runs preserves those active slots for subsequent retrieval; native disconnect creates no fabricated blocks; no typed content appears in local or remote data; existing continuous sync and session revocation remain functional. Only then replace/restart the production executable using the original trusted-device workflow. Do not change PAT access or grant new GitHub scopes. If the collector was not rebuilt, browser-only activity is still safe and native monitoring is unavailable.

Older `source:work-session` automatic records have not been deleted, altered or reclassified. They lack input evidence and should be reviewed before treating prior daily totals as verified. Approximate retrospectively marked tasks do not create actual activity records.

Use the same Windows account with existing Credential Manager entry `FocusGrowthSync/GitHub`. The PAT stays native. Run `FocusGrowthSyncV04.exe --production-readonly` for the existing Pages workspace. `--synthetic-v04` uses isolated browser storage and a private `sync/tests/v04-<device>.json` file. The name production-readonly remains for compatibility: actual writes depend on native authorization and saved enable/pause state.

## Permanent trust and revocation

Following explicit Windows pairing approval, the browser creates a non-exportable RSA signing key in IndexedDB. The native service stores the matching public key and trusted identity in DPAPI CurrentUser storage. No persistent bearer or GitHub PAT is stored in localStorage. Thirty-second single-use challenges authenticate reconnection; memory-only bearer tokens expire after15 minutes and are automatically replaced using signed proof. Trusted identities and production write grants have no fixed expiry and remain valid until revoked. Earlier15-day/24-hour/7-day proposals are superseded by explicit user instructions.

Refresh and reopening the same browser profile recover the connection. Web Locks prevent two tabs taking over the same device connection. Clearing browser site data or changing Windows identity requires pairing again. Windows DPAPI, private repository identity, fixed path, allowed fields, conditional SHA, operation IDs, conflicts, encrypted backups and durable queues remain enforced. Authentication anomalies revoke the affected browser identity; repository/credential failures pause processing. Same-origin malicious code or a compromised Windows account remains outside the trusted boundary.

The native service preserves its previous enable/pause preference on restart. Closing/minimizing the window hides it to the system tray. Tray menu: show window, pause/resume, trusted browser list with selective revocation, revoke production authorization, optional Windows sign-in startup, and exit. Startup is opt-in and initially off; it uses the current-user Run key, without changing firewall or browser security. Keep the executable at its configured path. Exit preserves the preference. Revoke authorization to prevent future automatic processing; pause preserves the queue.

The webpage offers pause, unpair, and revoke production authorization. Unpair revokes the device and deletes only its browser authentication identity, never tasks/history/queues. Native controls remain available if browser authentication fails. New pairing code pauses communication but does not delete trusted identities; explicit revoke controls perform revocation.

## Native production authorization

After explicit user approval, pair the real Pages workspace and verify its fresh complete encrypted backup and protocol projection against the private remote. A local-only manifest has exactly four fields: consentReference, expectedRemoteSha, expectedProjectionDigest, expectedBackupSha256. Run `FocusGrowthSyncV04.exe --authorize-production <absolute-manifest-path>`. The user checks the initially unchecked native scope checkbox and presses the grant button. This is permanent allowed-field authorization until revoked; it does not expose a PAT. Then explicitly enable the attached real workspace. Never initialize an already initialized state or reuse the old initial-migration approval.

## Development and tests

`build.cmd` uses the Windows Framework compiler, explicit source files and the current repository root web files. Development tests: executable `--selftest`, then Node scripts under tests. The tests/SessionTests.cs standalone runner covers device trust, proof replay, origin checks, encrypted persistence, permanent validity, signature anomaly revocation and nonce expiry with synthetic keys. Personal backups, consent manifests, real task reports and runtime storage must never be packaged or committed.

## Release validation limits

Automated native/core and frontend checks pass; actual isolated testing verified pairing, refresh reconnection, enabled native restart reconnection, browser→GitHub and GitHub→browser. The user subsequently requested proceeding with formal activation before completing remaining pause-state restart and old-credential revocation UI demonstrations. Those remaining final demonstrations are deferred, not recorded as completed. No real task is modified for write testing.
