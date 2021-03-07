Write-Host 'Building with release configuration...'
dotnet publish -c Release

Write-Host 'Reading version...'
$versionXmlResult = Select-Xml -Path api.csproj -XPath /Project/PropertyGroup/Version
$version = $versionXmlResult.Node.InnerText
Write-Output "Version: $version"

Write-Host 'Creating archive...'
Compress-Archive -Path bin/Release/netcoreapp3.1/publish/* -DestinationPath pkg/"api-$version.zip"