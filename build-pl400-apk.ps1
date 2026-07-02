$ErrorActionPreference = "Stop"

$unityCandidates = @(
  "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe",
  "C:\Program Files\Unity 2022.3.62f3\Editor\Unity.exe",
  "C:\UnityEditors\2022.3.62f3\Editor\Unity.exe"
)

$unity = $unityCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $unity) {
  throw "Unity Editor no encontrado."
}

$apkPath = "$PSScriptRoot\Builds\PL400-RPG.apk"
$log = "$PSScriptRoot\unity-android-build.log"

# Unity.exe es GUI: Start-Process -Wait espera de verdad a que termine.
Start-Process -FilePath $unity -Wait -ArgumentList @(
  "-batchmode",
  "-nographics",
  "-quit",
  "-projectPath", "`"$PSScriptRoot\PL400Unity`"",
  "-executeMethod", "BuildApk.BuildAndroid",
  "-logFile", "`"$log`""
)

$compileError = Select-String -Path $log -Pattern "error CS" -Quiet
$buildOk = Select-String -Path $log -Pattern "Result: Success|Build Successful" -Quiet
$apkExists = Test-Path $apkPath

if ($compileError -or -not $buildOk -or -not $apkExists) {
  Get-Content $log -Tail 120
  if ($compileError) { throw "Error de compilacion C#. Revisa unity-android-build.log" }
  throw "El build no termino correctamente. Revisa unity-android-build.log"
}

Write-Host "Build OK." -ForegroundColor Green
Get-Item $apkPath | Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}}, LastWriteTime
