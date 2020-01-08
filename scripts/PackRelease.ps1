[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $targetDir,
    [Parameter()]
    [string]
    $version,
    [Parameter()]
    [string]
    $targetDll,
    [Parameter()]
    [string]
    $channel,
    [Parameter(Mandatory = $false)]
    [string]
    $ci
)

$isCI = $ci -eq 'true';

if (Test-Path $targetDll) { Rename-Item $targetDll -NewName noah.rhp }
if (!$isCI) { Return }

$updateChannel = "latest"
$prerelease = "false"

if ($channel -ne "Release") { $updateChannel = "dev"; $prerelease = "true" }

$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Definition;
$projectRoot = [System.IO.Path]::GetDirectoryName($scriptsDir);


function GetHash ($filePath) {
    $hasher = Join-Path $projectRoot Hash\bin\Debug\hash.exe
    $hash = & $hasher $filePath;
    return $hash;
}

function WriteXML ($xmlFile, $sourceFile, $version) {
    $latest = Join-Path $targetDir $xmlFile;
    $hash = GetHash $sourceFile;
    $name = [System.IO.Path]::GetFileName($sourceFile)

    if (Test-Path $latest) { Remove-Item $latest -Force }

    $xml = New-Object System.XMl.XmlTextWriter($latest, $Null);
    $xml.Formatting = 'Indented';
    $xml.Indentation = 1;
    $xml.IndentChar = "`t";
    $xml.WriteStartDocument();
    $xml.WriteStartElement("root");
    $xml.WriteElementString("version", $version);
    $xml.WriteElementString("hash", $hash);
    $xml.WriteElementString("file", $name);
    $xml.WriteEndElement();
    $xml.WriteEndDocument();
    $xml.Flush();
    $xml.Close();
}

$winrhi = "NoahInRhino-" + $updateChannel + "-" + $version + ".rhi";
$macrhi = "NoahInRhino-" + $updateChannel + "-" + $version + ".macrhi";

$targetZip = Join-Path $targetDir NoahPlugin.zip;
$winPlugin = Join-Path $targetDir $winrhi;
$macPlugin = Join-Path $targetDir $macrhi;
$tmpExpandDir = Join-Path $targetDir NoahPlugin;

if (Test-Path $targetZip) { Remove-Item $targetZip -Force }
if (Test-Path $winPlugin) { Remove-Item $winPlugin -Force }
if (Test-Path $macPlugin) { Remove-Item $macPlugin -Force }
if (Test-Path $tmpExpandDir) { Remove-Item $tmpExpandDir -Force -Recurse }

Get-ChildItem $targetDir | Where-Object { $_.Extension -eq '.rhp' -or $_.Extension -eq '.dll' -or $_ -is [IO.DirectoryInfo] } | Compress-Archive -DestinationPath $targetZip -CompressionLevel "NoCompression";
Expand-Archive -Path $targetZip -DestinationPath $tmpExpandDir;

$tmpRhpDir = Join-Path $tmpExpandDir NoahPlugin.rhp

New-Item $tmpRhpDir -ItemType 'directory';

Get-ChildItem $tmpExpandDir | Where-Object { $_ -is [IO.FileInfo] } | Move-Item -Destination $tmpRhpDir;

$macPluginZip = Join-Path $targetDir NoahPlugin.mac.zip
Compress-Archive -Path $tmpExpandDir -DestinationPath $macPluginZip -CompressionLevel "NoCompression";

Rename-Item -Path $macPluginZip -NewName $macPlugin;
Rename-Item -Path $targetZip -NewName $winPlugin;

$channelDir = Join-Path $targetDir channel;
$channelWinXml = ".\channel\" + $updateChannel + "-win.xml";
$channelMacXml = ".\channel\" + $updateChannel + "-mac.xml";
New-Item $channelDir -ItemType 'directory';

WriteXML $channelWinXml $winPlugin $version
WriteXML $channelMacXml $macPlugin $version

function SetActionOutput($key, $val) {
    $output = "::set-output name=" + $key + "::" + $val;
    Write-Host $output;
}

Write-Host;
SetActionOutput version $version;
SetActionOutput channel $channel;
SetActionOutput prerelease $prerelease;
Write-Host;
