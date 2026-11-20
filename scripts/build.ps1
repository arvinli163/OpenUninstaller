$ErrorActionPreference="Stop"
$root=Split-Path -Parent $PSScriptRoot
dotnet restore "$root\OpenUninstaller.sln"
dotnet build "$root\OpenUninstaller.sln" -c Release --no-restore
