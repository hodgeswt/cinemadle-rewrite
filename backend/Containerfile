FROM golang:1.23-alpine AS builder

RUN apk add --no-cache git

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .
RUN go build -o cinemadle-server ./cmd/server/main.go

FROM alpine:latest
RUN apk add --no-cache ca-certificates
RUN apk add --no-cache tzdata

WORKDIR /root/
ENV CINEMADLE_CONFIG=config-release.json
COPY --from=builder /app/cinemadle-server /app/logw.json /app/config-release.json .

EXPOSE 8080

CMD ["./cinemadle-server"]
