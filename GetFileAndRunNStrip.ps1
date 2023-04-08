$cvrPath = $env:CVRPATH
$cvrExecutable = "ChilloutVR.exe"
$cvrDefaultPath = "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR"

if ($cvrPath -and (Test-Path "$cvrPath\$cvrExecutable")) {
    # Found ChilloutVR.exe in the existing CVRPATH
    Write-Host ""
    Write-Host "Found the ChilloutVR folder on: $cvrPath"
}
else {
    # Check if ChilloutVR.exe exists in default Steam location
    if (Test-Path "$cvrDefaultPath\$cvrExecutable") {
        # Set CVRPATH environment variable to default Steam location
        Write-Host "Found the ChilloutVR on the default steam location, setting the CVRPATH Env Var at User Level!"
        [Environment]::SetEnvironmentVariable("CVRPATH", $cvrDefaultPath, "User")
        $env:CVRPATH = $cvrDefaultPath
        $cvrPath = $env:CVRPATH
    }
    else {
        Write-Host "[ERROR] ChilloutVR.exe not found in CVRPATH or the default Steam location."
        Write-Host "        Please define the Environment Variable CVRPATH pointing to the ChilloutVR folder!"
        return
    }
}

$scriptDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$managedLibsFolder = $scriptDir + "\ManagedLibs"

if (!(Test-Path $managedLibsFolder)) {
    New-Item -ItemType Directory -Path $managedLibsFolder
}

Write-Host "Copying ml_prm from CVR Mods folder to ManagedLibs folder..."
Copy-Item $cvrPath\Mods\ml_prm.dll -Destination $managedLibsFolder

# Create an array to hold the file names to strip
$dllsToStrip = @('ml_prm.dll')

# Check if NStrip.exe exists in the current directory
if(Test-Path -Path ".\NStrip.exe") {
  $nStripPath = ".\NStrip.exe"
}
else {
  # Try to locate NStrip.exe in the PATH
  $nStripPath = Get-Command -Name NStrip.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
  if($null -eq $nStripPath) {
      # Display an error message if NStrip.exe could not be found
      Write-Host "Could not find NStrip.exe in the current directory nor in the PATH." -ForegroundColor Red
      Write-Host "Visit https://github.com/bbepis/NStrip/releases/latest to grab a copy." -ForegroundColor Red
      return
  }
}

# Loop through each DLL file to strip and call NStrip.exe
foreach($dllFile in $dllsToStrip) {
  $dllPath = Join-Path -Path $managedLibsFolder -ChildPath $dllFile
  & $nStripPath -p -n $dllPath $dllPath
}

Write-Host ""
Write-Host "Copied and Nstripped the ml_prm"
Write-Host ""
Write-Host "Press any key to exit"
$HOST.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | OUT-NULL
$HOST.UI.RawUI.Flushinputbuffer()
