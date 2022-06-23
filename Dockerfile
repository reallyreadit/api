# Based off Debian 10 / buster
FROM mcr.microsoft.com/dotnet/sdk:3.1
RUN apt update && apt install fonts-noto-color-emoji

# Add Mono to fix OmniSharp C# .NET language support inside the container,
# when combining with the OmniSharp C# VSCode extension v1.25+
# See
# - https://github.com/microsoft/vscode-dev-containers/issues/1474
# - https://www.mono-project.com/download/vs/#download-lin-debian
RUN apt install -y apt-transport-https dirmngr gnupg ca-certificates \
	&& apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
	&& echo "deb https://download.mono-project.com/repo/debian vs-buster main" | tee /etc/apt/sources.list.d/mono-official-vs.list \
	&& apt update -y \
	&& apt install -y mono-devel

ENTRYPOINT ["/bin/sh", "-c", "cd /api && dotnet restore && dotnet watch run"]