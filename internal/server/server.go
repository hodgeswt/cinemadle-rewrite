package server

import (
	"context"
	"errors"
	"flag"
	"fmt"
	"net/http"
	"strconv"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/hodgeswt/cinemadle-rewrite/internal/cache"
	"github.com/hodgeswt/cinemadle-rewrite/internal/controllers"
	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type CinemadleServer struct {
	router   *gin.Engine
	server   *http.Server
	port     string
	logger   *logw.Logger
	cache    *cache.Cache
	ctx      context.Context
	cancel   context.CancelFunc
	config   *datamodel.Config
	testMode bool
}

func (it *CinemadleServer) GetRouter() (*gin.Engine, error) {
	if !it.testMode {
		return nil, errors.New("ErrAccessRawRouterOutsideTestMode")
	}

	return it.router, nil
}

func (it *CinemadleServer) MakeServer(logger *logw.Logger, testMode bool) error {
	config, err := datamodel.LoadConfig(logger)

	if err != nil {
		return err
	}

	ctx, cancel := context.WithCancel(context.Background())

	it.testMode = testMode
	it.config = config
	it.ctx = ctx
	it.cancel = cancel
	it.cache = cache.NewCache(ctx, logger, config)
	it.logger = logger
	it.router = gin.Default()
	it.createEndpoints()

	port := flag.Int("port", 8080, "the port to run the server on")
	flag.Parse()

	it.port = strconv.Itoa(*port)
	it.server = &http.Server{
		Addr:    fmt.Sprintf(":%s", it.port),
		Handler: it.router.Handler(),
	}

	return nil
}

func (it *CinemadleServer) Run() {
	it.logger.Debugf("+server.Run")
	defer it.logger.Debugf("-server.Run")

	httpErr := make(chan error, 1)
	go func(ch chan<- error) {
		if err := it.server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			ch <- err
		}
	}(httpErr)

	err := <-httpErr
	it.logger.Errorf("server.Run: %v", err)
}

func (it *CinemadleServer) Shutdown() error {
	it.logger.Debug("+server.Shutdown")
	defer it.logger.Debug("-server.Shutdown")

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	if err := it.server.Shutdown(ctx); err != nil {
		it.logger.Errorf("server.Shutdown: Error during graceful shutdown: %v", err)
		return nil
	}

	it.cancel()

	select {
	case <-ctx.Done():
		it.logger.Warn("server.Shutdown: 10s timeout reached during graceful shutdown")
		return errors.New("ServerShutdownTimeoutErr")
	default:
		return nil
	}

}

func (it *CinemadleServer) createEndpoints() {
	v1 := it.router.Group("/api/v1")
	{
		v1.GET("/healthcheck", func(c *gin.Context) {
			controllers.HealthCheck(c, it.logger)
		})
		v1.GET("/media/:type/:date", func(c *gin.Context) {
			controllers.MediaOfTheDay(c, it.config, it.logger, it.cache)
		})
	}
}
