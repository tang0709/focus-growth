param([string]$ManifestPath = (Join-Path $PSScriptRoot 'PRODUCTION-DRY-RUN.json'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
$report = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$manifest = if ($report.backup) { $report.backup } else { $report }
$encrypted = [IO.File]::ReadAllBytes($manifest.path)
$plain = [Security.Cryptography.ProtectedData]::Unprotect($encrypted, $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
try {
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $digest = ([BitConverter]::ToString($hasher.ComputeHash($plain))).Replace('-', '').ToLowerInvariant() } finally { $hasher.Dispose() }
    if ($digest -ne $manifest.sha256) { throw 'Backup checksum mismatch. Source data remains unchanged.' }
    $db = [Text.Encoding]::UTF8.GetString($plain) | ConvertFrom-Json
    if ($db.version -ne 1) { throw 'Unsupported browser database version.' }
    [pscustomobject]@{result='PASS';sha256=$digest;version=$db.version;projects=$db.projects.Count;tasks=$db.tasks.Count;plans=$db.plans.Count;restoredOnlyInMemory=$true;browserModified=$false}
} finally { [Array]::Clear($plain, 0, $plain.Length) }
# Deliberately no export, overwrite, browser import or cloud write operation.
