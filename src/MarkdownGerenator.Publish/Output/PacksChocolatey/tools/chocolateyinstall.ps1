$packageName = 'Markdown-Generator'
$exeName = 'MarkdownGenerator.exe'
$url = 'https://github.com/juniorgasparotto/MarkdownGenerator/releases/download/1.0.0/MarkdownGenerator.zip'
$binRoot = Get-ToolsLocation
$installPath = Join-Path $binRoot $packageName

# Download and unzip file
Install-ChocolateyZipPackage -PackageName $packageName `
                             -Url $url `
                             -UnzipLocation $installPath

# Create environment path in windows
Install-ChocolateyPath $installPath 'user'

# Create shortcut in desktop
#Write-Host "Create Desktop ShortCurt..."
#$desktop = $([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::DesktopDirectory))
#$desktop = Join-Path $desktop "$packageName.lnk"
#Write-Host $desktop
#Install-ChocolateyShortcut -shortcutFilePath $desktop -targetPath "$installPath/$exeName"

# Create shortcut in start menu
#Write-Host "Create StartMenu ShortCurt..."
#$programs = [environment]::GetFolderPath([environment+specialfolder]::Programs)
#$programs = Join-Path $programs "$packageName.lnk"
#Write-Host $programs
#Install-ChocolateyShortcut -shortcutFilePath $programs -targetPath "$installPath/$exeName"