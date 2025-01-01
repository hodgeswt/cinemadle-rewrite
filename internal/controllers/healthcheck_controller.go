package controllers

import "github.com/gin-gonic/gin"

type Health struct {
	Message string `json:"message"`
}

func HealthCheck(c *gin.Context) {
	r := Health{
		Message: "alive",
	}

	c.JSON(200, r)
	c.Done()
}
