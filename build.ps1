param([string]$RuntimeVersion = '10.0.14')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Push-Location $PSScriptRoot
try {
    dotnet run --project tests/Trainer.Tests/Trainer.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Regression checks failed.' }
    $artifactRoot = Join-Path $PSScriptRoot 'artifacts'
    $packageName = 'tekken6-community-v3.0.0-win-x64'
    $packagePath = Join-Path $artifactRoot $packageName
    New-Item -ItemType Directory -Force -Path $packagePath | Out-Null
    dotnet publish src/Trainer/Trainer.csproj -c Release -r win-x64 --self-contained true `
        "-p:RuntimeFrameworkVersion=$RuntimeVersion" -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None -p:DebugSymbols=false -o $packagePath
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    foreach ($document in @('README.md','LICENSE','THIRD_PARTY_NOTICES.md')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $document) -Destination $packagePath
    }
    $nugetLocation = (dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
    $noticePath = Join-Path $packagePath 'licenses'
    New-Item -ItemType Directory -Force -Path $noticePath | Out-Null
    foreach ($pack in @('microsoft.netcore.app.runtime.win-x64','microsoft.windowsdesktop.app.runtime.win-x64')) {
        $runtimePath = Join-Path (Join-Path $nugetLocation $pack) $RuntimeVersion
        $notices = @(Get-ChildItem -LiteralPath $runtimePath -File | Where-Object { $_.Name -match '^(LICENSE(\.TXT)?|THIRD-PARTY-NOTICES\.TXT)$' })
        if (-not ($notices | Where-Object { $_.Name -match '^LICENSE' })) { throw "Missing runtime license: $runtimePath" }
        foreach ($notice in $notices) {
            Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $noticePath ("$pack-" + $notice.Name))
        }
    }
    $archivePath = Join-Path $artifactRoot "$packageName.zip"
    Compress-Archive -LiteralPath $packagePath -DestinationPath $archivePath -Force
    $checksums = foreach ($path in @((Join-Path $packagePath 'Tekken6.Community.exe'), $archivePath)) {
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $([IO.Path]::GetFileName($path))"
    }
    $checksums | Set-Content -LiteralPath (Join-Path $artifactRoot 'SHA256SUMS.txt') -Encoding ascii
    Get-Item -LiteralPath $archivePath | Select-Object Name,Length
}
finally { Pop-Location }
