# Focus & Growth one-time initial migration

This is a separate native initializer. It does not unlock the companion's normal production write routes or Cloud.Write. It never offers a server, arbitrary destination, deletion, general updates, automatic retries or a force flag.

Build with build.cmd after building the reviewed Windows companion. The companion assembly is a local dependency and contains the original schema, projection, DPAPI and Credential Manager integration. Run `FocusGrowthFirstMigration.exe --selftest` for synthetic core and in-memory GitHub HTTP tests; no network or credential is used by those tests.

The operational entry is `--initialize-once <local approval manifest> <fresh explicit browser export>`. A flag alone cannot grant access: the locally attested user approval must identify the reviewed preview, backup, full and projected local snapshot, remote SHA and record counts, and specify the single private repository/path. The initializer decrypts the reviewed backup and preview, validates their bindings, compares the fresh normal browser export (maximum age90 seconds), verifies private repository identity, and requires the exact empty revision0/uninitialized remote.

Before the single conditional PUT, it durably consumes a DPAPI-protected approval record with FileMode.CreateNew, keyed by the preview and original remote SHA. It then rechecks the local snapshot and remote SHA. Only whitelist projects/tasks/plans are serialized. A failure or uncertain network result consumes the attempt and stops; never automatically retry. Read the remote operationIds to investigate uncertainty. Existing initialized or nonempty data cannot be overwritten, even if another consent ID is supplied.

Manifest field names: repository, path, scope, approvalEvidence, previewReport, consentId, approvedBinding, counts, authorizationUtc. The manifest and all user-specific exports, reports, approval records and backups stay local and must not be shipped or committed. Do not treat a hand-edited manifest as new human consent. The calling operator must have the user's explicit snapshot-specific approval.

Credential: existing Windows Generic Credential FocusGrowthSync/GitHub. PAT is read only into native memory and sent only to api.github.com over TLS1.2 with redirects disabled. Never place it in arguments, browser APIs, source or logs.

After successful initialization, use the companion and normal webpage's explicit read-only verification button to compare remote whitelist fields with local data. This comparison does not import, complete, reschedule or change browser data. Continuous production bidirectional writes remain disabled; an initialization approval does not authorize indefinite future task changes. Bidirectional regression and write tests use isolated synthetic workspaces.

Backups and the consumed record are in the current Windows user's LocalAppData/FocusGrowthSync directories. Keep the successful receipt and the same Windows user for DPAPI recovery. No restoration or overwriting of browser data is performed by this program.
