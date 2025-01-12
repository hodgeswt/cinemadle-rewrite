//go:build unit
// +build unit

package unittest

import (
	"encoding/json"
	"fmt"
	"net/http"
	"net/http/httptest"
	"os"
	"testing"

	"github.com/hodgeswt/cinemadle-rewrite/internal/controllers"
	"github.com/hodgeswt/cinemadle-rewrite/internal/server"
	"github.com/hodgeswt/utilw/pkg/logw"
	"github.com/stretchr/testify/assert"
)

type EnvVars struct {
	LogLevelW       string
	CinemadleConfig string
}

func GetPreviousEnv() *EnvVars {
	l, _ := os.LookupEnv("LOGLEVELW")
	c, _ := os.LookupEnv("CINEMADLE_CONFIG")

	return &EnvVars{
		LogLevelW:       l,
		CinemadleConfig: c,
	}
}

func PrepareTest(t *testing.T) *server.CinemadleServer {
	os.Setenv("LOGLEVELW", "ERROR")
	os.Setenv("CINEMADLE_CONFIG", "../../config.json")

	logger, err := logw.NewLogger("cinemadle-unittest", nil)

	if err != nil {
		assert.FailNow(t, fmt.Sprintf("Unable to create logger: %v", err.Error()))
	}

	app := &server.CinemadleServer{}
	err = app.MakeServer(logger, true)

	if err != nil {
		assert.FailNow(t, fmt.Sprintf("Unable to create logger: %v", err.Error()))
	}

	return app
}

func CleanupTest(vars *EnvVars) {
	os.Setenv("LOGLEVELW", vars.LogLevelW)
	os.Setenv("CINEMADLE_CONFIG", vars.CinemadleConfig)
}

func TestHealthcheck200(t *testing.T) {
	vars := GetPreviousEnv()
	defer CleanupTest(vars)

	app := PrepareTest(t)

	w := httptest.NewRecorder()
	req, _ := http.NewRequest("GET", "/api/v1/healthcheck", nil)

	router, err := app.GetRouter()

	if err != nil {
		assert.FailNow(t, fmt.Sprintf("Unable to create logger: %v", err.Error()))
	}

	router.ServeHTTP(w, req)

	expectedResponse := controllers.Health{
		Message: "alive",
	}
	expectedJson, _ := json.Marshal(expectedResponse)

	assert.Equal(t, 200, w.Code)
	assert.Equal(t, string(expectedJson), w.Body.String())
}
