//go:build unit
// +build unit

package tmdb

import (
	"fmt"
	"testing"

	"github.com/hodgeswt/cinemadle-rewrite/internal/unittest"
	"github.com/stretchr/testify/assert"
)

const rotsId = 1895

func TestApiKeyValid(t *testing.T) {

	config, logger := unittest.CommonLoadConfig(t)

	tmdbClient, err := NewTmdbClient(config.TmdbOptions, logger)

	if err != nil {
		assert.FailNow(t, fmt.Sprintf("Unable to initialize tmdb client: %v", err))
	}

	assert.True(t, tmdbClient.Initialized)

	_, err = tmdbClient.client.GetMovieDetails(rotsId, nil)

	assert.Nil(t, err)
}
