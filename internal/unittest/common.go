//go:build unit
// +build unit

package unittest

import (
	"fmt"
	"os"
	"testing"

	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/logw"
	"github.com/stretchr/testify/assert"
)

func CommonLoadConfig(t *testing.T) (*datamodel.Config, *logw.Logger) {
	origL := os.Getenv("LOGLEVELW")
	origC := os.Getenv("CINEMADLE_CONFIG")
	defer func() {
		os.Setenv("LOGLEVELW", origL)
		os.Setenv("CINEMADLE_CONFIG", origC)
	}()

	os.Setenv("LOGLEVELW", "DEBUG")
	os.Setenv("CINEMADLE_CONFIG", "../../config-release.json")

	logger, err := logw.NewLogger("cinemadle-unittest", nil)

	if err != nil {
		assert.FailNow(t, fmt.Sprintf("Unable to create logger: %v", err))
	}

	config, err := datamodel.LoadConfig(logger)

	if err != nil {
		assert.FailNow(t, fmt.Sprintf("Unable to load config: %v", err))
	}

	return config, logger
}
