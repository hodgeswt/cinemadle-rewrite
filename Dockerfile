FROM denoland/deno:latest AS frontend-builder

WORKDIR /app/frontend

COPY frontend/package*.json ./
RUN deno install

COPY frontend/. .

ARG VITE_API_ENDPOINT="http://192.168.0.23:5000"
ENV VITE_API_ENDPOINT=${VITE_API_ENDPOINT}
ARG VITE_STRIPE_FRONTEND_KEY=""
ENV VITE_STRIPE_FRONTEND_KEY=${VITE_STRIPE_FRONTEND_KEY}
ARG VITE_PRODUCT_IDS=""
ENV VITE_PRODUCT_IDS=${VITE_PRODUCT_IDS}

RUN echo "API ENDPOINT IS: $VITE_API_ENDPOINT"
RUN echo "VITE_API_ENDPOINT=$VITE_API_ENDPOINT" > .env

RUN deno task build

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

ARG CINEMADLE_ADMIN_EMAIL=""
ENV CINEMADLE_ADMIN_EMAIL=${CINEMADLE_ADMIN_EMAIL}
ARG CINEMADLE_TEST_MODE="false"
ENV CINEMADLE_TEST_MODE=${CINEMADLE_TEST_MODE}

EXPOSE 5000

ENTRYPOINT ["dotnet", "backend-dotnet.dll"]
