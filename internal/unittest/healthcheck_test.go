//go:build unit
// +build unit

package unittest

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/hodgeswt/cinemadle-rewrite/internal/controllers"
	"github.com/hodgeswt/cinemadle-rewrite/internal/server"
	"github.com/stretchr/testify/assert"
)

func TestHealthcheck200(t *testing.T) {
	app := &server.CinemadleServer{}
	r := app.MakeServer()

	w := httptest.NewRecorder()
	req, _ := http.NewRequest("GET", "/api/v1/healthcheck", nil)
	r.ServeHTTP(w, req)

    expectedResponse := controllers.Health{
        Message: "alive",
    }
    expectedJson, _ := json.Marshal(expectedResponse)

	assert.Equal(t, 200, w.Code)
	assert.Equal(t, string(expectedJson), w.Body.String())
}
