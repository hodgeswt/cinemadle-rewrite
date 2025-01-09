package main

import (
	"log"

	"github.com/hodgeswt/cinemadle-rewrite/internal/server"
)

func main() {
	app := &server.CinemadleServer{}
    _ = app.MakeServer()
    if err := app.Run(); err != nil {
        log.Fatalf("Error: %v", err)
    }
}
