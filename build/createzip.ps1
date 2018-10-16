param (
    [Parameter(Mandatory=$true)][string]$version,
    [Parameter(Mandatory=$true)][string]$source,
    [Parameter(Mandatory=$true)][string]$project
 )

$versionPath = (Get-Item -Path '.\' -Verbose).FullName + '\' + $version;
$destinationPath = $versionPath + '\' + $project + '.zip'

New-Item -ErrorAction Ignore -ItemType directory -Path $versionPath

If(Test-path $destinationPath) {
	Remove-item $destinationPath
}

Add-Type -assembly "system.io.compression.filesystem"
[Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" )
[io.compression.zipfile]::CreateFromDirectory($source, $destinationPath)