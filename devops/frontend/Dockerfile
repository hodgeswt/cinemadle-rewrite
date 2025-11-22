FROM node:20-bookworm AS base
WORKDIR /app

# Install dependencies separately for better layer caching
FROM base AS deps
WORKDIR /app/frontend
COPY frontend/package*.json ./
RUN npm ci --include=dev

FROM deps AS builder
WORKDIR /app/frontend
COPY frontend/ ./

ARG VITE_API_ENDPOINT="http://localhost:5000"
ENV VITE_API_ENDPOINT=${VITE_API_ENDPOINT}
ARG VITE_STRIPE_FRONTEND_KEY=""
ENV VITE_STRIPE_FRONTEND_KEY=${VITE_STRIPE_FRONTEND_KEY}
ARG VITE_PRODUCT_IDS=""
ENV VITE_PRODUCT_IDS=${VITE_PRODUCT_IDS}

RUN echo "API ENDPOINT IS: $VITE_API_ENDPOINT"
RUN echo "VITE_API_ENDPOINT=$VITE_API_ENDPOINT" > .env

RUN npm run build

FROM nginx:1.27-alpine
WORKDIR /usr/share/nginx/html
COPY --from=builder /app/frontend/build ./
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
