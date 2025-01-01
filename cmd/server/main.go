package main

import (
	"log"
	"os"

	"github.com/hodgeswt/cinemadle-rewrite/internal/server"
)

func main() {
	app := &server.CinemadleServer{}
    _ = app.MakeServer()
    if err := app.Run(os.Args); err != nil {
        log.Fatalf("Error: %v", err)
    }
}
