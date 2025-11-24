FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrator

WORKDIR /app

COPY backend-dotnet/*.csproj ./
RUN dotnet restore
RUN dotnet nuget locals all --clear
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY backend-dotnet/. .
COPY devops/migrator/migrate-container.sh ./migrate-container.sh

RUN sed -i 's/\r$//' migrate-container.sh
RUN mkdir -p $HOME/.local/share

ENTRYPOINT ["/bin/sh", "./migrate-container.sh"]
