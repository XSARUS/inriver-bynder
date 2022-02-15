param (
    [Parameter(Mandatory=$true)][string]$version,
    [Parameter(Mandatory=$true)][string]$source,
    [Parameter(Mandatory=$true)][string]$project
 )

$date = (Get-Date).ToString("dd-MM-yy");

$versionPath = (Get-Item -Path '.\' -Verbose).FullName + '\' + $version;
$destinationPath = $versionPath + '\' + $project + '_' + $date + '.zip'

New-Item -ErrorAction Ignore -ItemType directory -Path $versionPath

If(Test-path $destinationPath) {
	Remove-item $destinationPath
}

Add-Type -assembly "system.io.compression.filesystem"
[Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" )
[io.compression.zipfile]::CreateFromDirectory($source, $destinationPath)