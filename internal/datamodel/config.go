package datamodel

import (
	"encoding/json"
	"errors"
	"io"
	"os"

	"github.com/hodgeswt/utilw/pkg/logw"
)

type Config struct {
	CacheAddress string `json:"cacheAddress"`
	CacheTimeout int    `json:"cacheTimeout"`
	Location     string `json:"location"`
}

var ErrLoadingConfig = errors.New("ErrLoadingConfig")

func LoadConfig(logger *logw.Logger) (*Config, error) {
	logger.Debug("+config.LoadConfig")
	defer logger.Debug("-config.LoadConfig")

	path := "config.json"
	envPath, ok := os.LookupEnv("CINEMADLE_CONFIG")
	if ok {
		path = envPath
	}

	f, err := os.Open(path)

	if err != nil {
		logger.Errorf("config.LoadConfig: %v", err)
		return nil, ErrLoadingConfig
	}

	defer f.Close()

	b, err := io.ReadAll(f)

	if err != nil {
		logger.Errorf("config.LoadConfig: %v", err)
		return nil, ErrLoadingConfig
	}

	var config Config
	err = json.Unmarshal(b, &config)

	if err != nil {
		logger.Errorf("config.LoadConfig: %v", err)
		return nil, ErrLoadingConfig
	}

	return &config, nil
}
