param([switch]$Export, [switch]$Test)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.tools\dotnet-home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.tools\nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBuildEnableWorkloadResolver = 'false'
$env:DOTNET_ROLL_FORWARD = 'LatestMinor'
$godot = Join-Path $PSScriptRoot '.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe'
& dotnet build 'Prototype\Atomica.csproj' --nologo
if ($LASTEXITCODE) { throw 'C# build failed' }
if ($Test) {
    & dotnet run --project 'Tests\Atomica.Tests.csproj'
    if ($LASTEXITCODE) { throw 'Tests failed' }
}
if ($Export) {
    New-Item -ItemType Directory -Force -Path 'build\Atomica-v02','build\qa' | Out-Null
    $export = Start-Process -FilePath $godot -ArgumentList '--headless','--path','Prototype','--export-release','"Windows Desktop"','../build/Atomica-v02/Atomica.exe','--quit' -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput 'build\qa\export.log' -RedirectStandardError 'build\qa\export-errors.log'
    if ($export.ExitCode -or (Select-String -LiteralPath 'build\qa\export.log','build\qa\export-errors.log' -Pattern 'ERROR:')) { throw 'Godot export failed; inspect build\qa\export.log and export-errors.log' }
    Copy-Item -LiteralPath 'Prototype\Maps' -Destination 'build\Atomica-v02' -Recurse -Force
    Copy-Item -LiteralPath 'Prototype\tuning.json','README.md','THIRD_PARTY_NOTICES.txt','VALIDATION.md' -Destination 'build\Atomica-v02' -Force
}
