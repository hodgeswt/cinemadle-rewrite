package main

import (
	"os"
	"os/signal"

	"github.com/hodgeswt/cinemadle-rewrite/internal/server"
	"github.com/hodgeswt/utilw/pkg/logw"
)

func main() {
	log, _ := logw.NewLogger("cinemadle-server", nil)

	log.Debug("+main.main")

	app := &server.CinemadleServer{}
	err := app.MakeServer(log)

	if err != nil {
		panic(err)
	}

	go app.Run()

	// Watch for C-c in case we need
	// custom cleanup
	sigint := make(chan os.Signal, 2)
	signal.Notify(sigint, os.Interrupt)
	ok := watch(log, sigint, app)

	os.Exit(deferred(log, ok))
}

// Place to put all desired deferred functions
// relevant to the main() function. Required because
// main() calls os.Exit, which breaks defer chain.
//
// returns: exit code 0 if ok, 1 otherwise
func deferred(log *logw.Logger, ok bool) int {
	log.Debug("-main.main")

	if ok {
		return 0
	} else {
		return 1
	}
}

// Watches for sigint and handles any cleanup operations
//
// returns: true if successful shutdown, false if not
func watch(log *logw.Logger, sigint chan os.Signal, app *server.CinemadleServer) bool {
	log.Debug("+main.watch")
	defer log.Debug("-main.watch")

	<-sigint

	err := app.Shutdown()

	if err != nil {
		log.Errorf("main.watch: %v", err)
		return false
	}

	return true
}
