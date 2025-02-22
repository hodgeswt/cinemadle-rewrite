.PHONY: build
build:
	docker build . -t cinemadle-server -f Containerfile

.PHONY: run
run:
	docker compose up
