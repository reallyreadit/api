# Copyright (C) 2022 reallyread.it, inc.
#
# This file is part of Readup.
#
# Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
#
# Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
#
# You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

Write-Host 'Building with release configuration...'
dotnet publish -c Release

Write-Host 'Reading version...'
$versionXmlResult = Select-Xml -Path api.csproj -XPath /Project/PropertyGroup/Version
$version = $versionXmlResult.Node.InnerText
Write-Output "Version: $version"

Write-Host 'Creating archive...'
# Create the pkg directory if it doesn't exist yet. Should be safe https://stackoverflow.com/questions/16906170/create-directory-if-it-does-not-exist#comment90975264_16911470
New-Item -ItemType Directory -Force -Path pkg
Compress-Archive -Path bin/Release/netcoreapp3.1/publish/* -DestinationPath pkg/"api-$version.zip"