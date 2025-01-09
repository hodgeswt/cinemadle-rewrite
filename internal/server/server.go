package server

import (
	"flag"
	"fmt"
	"strconv"

	"github.com/gin-gonic/gin"
	"github.com/hodgeswt/cinemadle-rewrite/internal/controllers"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type CinemadleServer struct {
	server *gin.Engine
	port   string
    logger *logw.Logger
}

func (it *CinemadleServer) MakeServer() *gin.Engine {
    logger, _ := logw.NewLogger("cinemadle-server", nil)

    it.logger = logger
    it.server = gin.Default()
	it.createEndpoints()

	return it.server
}

func (it *CinemadleServer) Run() error {
	it.logger.Debugf("+server.Run")
	defer it.logger.Debugf("-server.Run")

    port := flag.Int("port", 8080, "the port to run the server on")
    flag.Parse()

    it.port = strconv.Itoa(*port)
    it.server.Run(fmt.Sprintf(":%s", it.port))

    return nil
}

func (it *CinemadleServer) createEndpoints() {
	v1 := it.server.Group("/api/v1")
	{
		v1.GET("/healthcheck", controllers.HealthCheck)
	}
}
