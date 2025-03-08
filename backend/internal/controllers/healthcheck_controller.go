package controllers

import (
	"github.com/gin-gonic/gin"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type Health struct {
	Message string `json:"message"`
}

func HealthCheck(c *gin.Context, logger *logw.Logger) {
	logger.Debug("healthcheck ping")
	r := Health{
		Message: "alive",
	}

	c.JSON(200, r)
	c.Done()
}
