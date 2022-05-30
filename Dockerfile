FROM mcr.microsoft.com/dotnet/sdk:3.1
RUN apt update && apt install fonts-noto-color-emoji
ENTRYPOINT ["/bin/sh", "-c", "cd /api && dotnet restore && dotnet watch run"]