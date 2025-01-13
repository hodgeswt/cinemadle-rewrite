//go:build unit
// +build unit

package util

import (
	"fmt"
	"testing"

	"github.com/hodgeswt/cinemadle-rewrite/internal/unittest"
	"github.com/hodgeswt/utilw/pkg/rand"
	"github.com/stretchr/testify/assert"
)

func TestLCGParams(t *testing.T) {
	config, _ := unittest.CommonLoadConfig(t)

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
