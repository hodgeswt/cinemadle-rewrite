FROM golang:1.23-alpine AS builder

RUN apk add --no-cache git

WORKDIR /app

COPY go.mod go.sum ./

RUN go mod download

COPY . .

RUN go build cmd/server/main.go -o cinemadle-server

FROM alpine:latest

RUN apk add --no-cache ca-certificates

WORKDIR /root/

ENV CINEMADLE_CONFIG="./config-release.json"

COPY --from=builder /app/cinemadle-server .

EXPOSE 8080

CMD ["./cinemadle-server"]
