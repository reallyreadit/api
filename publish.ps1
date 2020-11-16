Write-Host 'Building with release configuration...'
dotnet publish -c Release

Write-Host 'Reading version...'
$versionXmlResult = Select-Xml -Path api.csproj -XPath /Project/PropertyGroup/Version
$version = $versionXmlResult.Node.InnerText
Write-Output "Version: $version"

Write-Host 'Creating archive...'
$publishDir = 'bin/Release/netcoreapp3.1/publish/'
$archiveFileName = "api-$version.zip"
$archivePath = Join-Path $publishDir $archiveFileName
Compress-Archive (Join-Path $publishDir *) $archivePath

Write-Host 'Uploading archive...'
Write-S3Object -ProfileName reallyreadit -Region us-east-2 -BucketName aws.reallyread.it -Key "web-sites/$archiveFileName" -File $archivePath

Write-Host 'Deleting archive...'
Remove-Item $archivePath