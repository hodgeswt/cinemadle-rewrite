FROM node:18-alpine AS frontend-builder

WORKDIR /app/frontend

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/. .

ARG VITE_API_ENDPOINT="http://192.168.0.23:5000"
ENV VITE_API_ENDPOINT=${VITE_API_ENDPOINT}

RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /app/backend

COPY backend-dotnet/*.csproj ./
RUN dotnet restore

COPY backend-dotnet/. .

RUN dotnet publish --configuration Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

WORKDIR /app

RUN mkdir -p /app/AppData
COPY --from=builder /app/backend/out ./
COPY --from=frontend-builder /app/frontend/build ./wwwroot

EXPOSE 5000

ENTRYPOINT ["dotnet", "backend-dotnet.dll"]
