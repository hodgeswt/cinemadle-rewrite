FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /app/backend

COPY backend-dotnet/*.csproj ./
RUN dotnet restore

COPY backend-dotnet/. .

RUN dotnet publish --configuration Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

WORKDIR /app

RUN mkdir -p /app/AppData
COPY --from=builder /app/backend/out ./

# Needed so LocalApplicationData exists
RUN mkdir -p $HOME/.local/share

ARG CINEMADLE_ADMIN_EMAIL=""
ENV CINEMADLE_ADMIN_EMAIL=${CINEMADLE_ADMIN_EMAIL}
ARG CINEMADLE_TEST_MODE="false"
ENV CINEMADLE_TEST_MODE=${CINEMADLE_TEST_MODE}

EXPOSE 5000

ENTRYPOINT ["dotnet", "backend-dotnet.dll"]
