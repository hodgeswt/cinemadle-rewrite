//go:build unit
// +build unit

package util

import (
	"fmt"
	"os"
	"testing"

	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/logw"
	"github.com/hodgeswt/utilw/pkg/rand"
	"github.com/stretchr/testify/assert"
)

func TestLCGParams(t *testing.T) {
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

	lcg, err := rand.NewLinearCongruentialGenerator(config.RandomizerOptions)

	if err != nil {
		assert.FailNow(t, fmt.Sprintf("Unable to create randomizer: %v", err))
	}

	found := make(map[int]struct{}, config.RandomizerOptions.Modulus)

	for i := 0; i < int(config.RandomizerOptions.Modulus); i++ {
		found[int(lcg.Next())] = struct{}{}
	}

	for i := 0; i < int(config.RandomizerOptions.Modulus); i++ {
		_, ok := found[i]

		if !ok {
			assert.FailNow(t, fmt.Sprintf("Expected %d to be generated; was not.", i))
		}
	}
}
