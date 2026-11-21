$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
# In a repository checkout, pin the reviewed root frontend into the local bundle.
# In the outputs delivery bundle, web/ already contains the reviewed source.
if ((Test-Path -LiteralPath (Join-Path $repoRoot '.git')) -and (Test-Path -LiteralPath (Join-Path $repoRoot 'index.html'))) {
    New-Item -ItemType Directory -Force -Path (Join-Path $sourceRoot 'web') | Out-Null
    foreach ($name in @('index.html','sync.js')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination (Join-Path $sourceRoot "web/$name")
    }
}
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$binary = Join-Path $sourceRoot 'FocusGrowthSync.exe'
$sources = @('Core.cs','Transport.cs','Bridge.cs','Program.cs','Tests.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
& $compiler /nologo /target:winexe /optimize+ "/out:$binary" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:System.Security.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Compile failed' }
Write-Output 'Build completed. No credentials included.'
