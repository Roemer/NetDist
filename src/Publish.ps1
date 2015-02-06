function GetMsBuildPath() {
    $lib = [System.Runtime.InteropServices.RuntimeEnvironment]
    $rtd = $lib::GetRuntimeDirectory()
    return Join-Path $rtd msbuild.exe
}

### Main starts here
$releaseFolders = @(".\WpfClient\bin\Release", ".\WpfServer\bin\Release", ".\WpfServerAdmin\bin\Release")
foreach ($releaseFolder in $releaseFolders) {
    Write-Output "Clearing: $releaseFolder"
	# Delete the folder
	if (Test-Path $releaseFolder) {
		Remove-Item -Recurse -Force $releaseFolder -ea SilentlyContinue
	}
}
# Compile as release build
Write-Output "Building solution"
$msBuildPath = GetMsBuildPath
$buildArgs = @{
    FilePath = $msBuildPath
    ArgumentList = "NetDist.sln", "/t:rebuild", "/p:Configuration=Release", "/v:minimal"
    #RedirectStandardOutput = $BuildLog
    Wait = $true
}
Start-Process @buildArgs
# Creating zips
Write-Output "Creating zips"
$dstZipFolder = (get-item $releaseFolder).parent.parent.parent.FullName
[Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem")
foreach ($releaseFolder in $releaseFolders) {
    Write-Output "Zipping: $releaseFolder"
    # Remove settings file
    $settingFilePath = Join-Path $releaseFolder "settings.json"
    if (Test-Path $settingFilePath) {
        Remove-Item $settingFilePath
    }
    # Remove log folder
    $logPath = Join-Path $releaseFolder "Log"
    if (Test-Path $logPath) {
        Remove-Item -Recurse -Force $logPath
    }
    # Delete old zip
    $zipFileName = (get-item $releaseFolder).parent.parent.Name + ".zip"
    $dstZipFile = Join-Path $dstZipFolder $zipFileName
    if (Test-Path $dstZipFile) {
        Remove-Item $dstZipFile
    }
    # Create zip file
    [System.IO.Compression.ZipFile]::CreateFromDirectory($releaseFolder, $dstZipFile)
}
# Open the containing folder
Start-Process $dstZipFolder
