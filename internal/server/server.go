package server

import (
	"errors"
	"fmt"

	"github.com/gin-gonic/gin"
	"github.com/hodgeswt/cinemadle-rewrite/internal/controllers"
	"github.com/hodgeswt/utilw/pkg/argparse"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type CinemadleServer struct {
	server *gin.Engine
	port   string
}

func getArgs() []argparse.Argument {
	x := new(PortArg)
	return []argparse.Argument{x}
}

func (it *CinemadleServer) MakeServer() *gin.Engine {
	fmt.Println("Hello, server!")

	it.server = gin.Default()

	it.createEndpoints()

	return it.server
}

func (it *CinemadleServer) Run(args []string) error {
	logw.Debugf("+server.Run, args: %v", args)
	defer logw.Debugf("-server.Run")

    parsed, err := argparse.Parse(args, getArgs(), true)

    if err != nil {
        return err
    }

    allValid := true
    invalid := []argparse.Argument{}
    for _, argument := range parsed {
        if !argument.Valid() {
            logw.Errorf("Invalid argument: %v", argument)
            invalid = append(invalid, argument)
            allValid = false
        }
    }

    if !allValid {
        return errors.New("Invalid arguments provided")
    }

    if parsed["port"] != nil {
        it.port = parsed["port"].Value()[0]
    } else {
        it.port = "8080"
    }

	it.server.Run(fmt.Sprintf(":%s", it.port))

    return nil
}

func (it *CinemadleServer) createEndpoints() {
	v1 := it.server.Group("/api/v1")
	{
		v1.GET("/healthcheck", controllers.HealthCheck)
	}
}
