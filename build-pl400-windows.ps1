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

$exePath = "$PSScriptRoot\Builds\PL400-RPG-Windows\PL400-RPG.exe"
$log = "$PSScriptRoot\unity-windows-build.log"

# Unity.exe es GUI: con "&" PowerShell NO espera. Start-Process -Wait espera de verdad.
Start-Process -FilePath $unity -Wait -ArgumentList @(
  "-batchmode",
  "-nographics",
  "-quit",
  "-projectPath", "`"$PSScriptRoot\PL400Unity`"",
  "-executeMethod", "BuildApk.BuildWindows",
  "-logFile", "`"$log`""
)

# Unity en batchmode puede devolver codigo != 0 aunque el build sea correcto.
# Validamos por el log + que el EXE existe.
$compileError = Select-String -Path $log -Pattern "error CS" -Quiet
$buildOk = Select-String -Path $log -Pattern "Result: Success|Build Successful" -Quiet
$exeExists = Test-Path $exePath

if ($compileError -or -not $buildOk -or -not $exeExists) {
  Get-Content $log -Tail 120
  if ($compileError) { throw "Error de compilacion C#. Revisa unity-windows-build.log" }
  throw "El build no termino correctamente. Revisa unity-windows-build.log"
}

Write-Host "Build OK." -ForegroundColor Green
Get-Item $exePath | Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}}, LastWriteTime
