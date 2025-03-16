.PHONY: build
build:
	docker build . -t cinemadle-server -f Containerfile

.PHONY: run
run:
	docker compose up

.PHONY: run-local
run-local:
	go run ./cmd/server/main.go
