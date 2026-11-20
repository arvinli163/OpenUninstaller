param([ValidateSet("portable","installer")][string]$Mode="portable",[string]$Runtime="win-x64")
$ErrorActionPreference="Stop";$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root "artifacts\$Mode";New-Item -ItemType Directory -Force $out|Out-Null
dotnet publish "$root\src\OpenUninstaller\OpenUninstaller.csproj" -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $out
Write-Host "Published: $out"
