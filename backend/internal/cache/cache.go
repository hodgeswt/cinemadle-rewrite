package cache

import (
	"context"
	"errors"
	"os"
	"time"

	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/logw"
	"github.com/redis/go-redis/v9"
)

var (
	ErrCacheSet  = errors.New("ErrCacheSet")
	ErrCacheGet  = errors.New("ErrCacheGet")
	ErrCacheMiss = errors.New("ErrCacheMiss")
)

type Cache struct {
	cache   *redis.Client
	timeout int
	logger  *logw.Logger
	ctx     context.Context
}

func NewCache(ctx context.Context, logger *logw.Logger, config *datamodel.Config) *Cache {
	logger.Debug("+cache.NewCache")
	defer logger.Debug("-cache.NewCache")

	cachePassword, _ := os.LookupEnv("CINEMADLE_CACHE_PASSWORD")

	r := redis.NewClient(&redis.Options{
		Addr:     config.CacheAddress,
		Password: cachePassword,
		DB:       0,
	})

	pong, err := r.Ping(ctx).Result()

	if err != nil {
		logger.Errorf("Error connecting to cache: %v", err.Error())
	} else {
		logger.Debugf("Cache ping: %s", pong)
	}

	return &Cache{
		cache:   r,
		timeout: config.CacheTimeout,
		logger:  logger,
		ctx:     ctx,
	}
}

func (it *Cache) Set(key string, value string) error {
	it.logger.Debug("+cache.Set")
	defer it.logger.Debug("-cache.Set")

	err := it.cache.Set(it.ctx, key, value, time.Duration(it.timeout)*time.Minute).Err()

	if err != nil {
		it.logger.Errorf("cache.Set: %v", err)
		return ErrCacheSet
	}

	return nil
}

func (it *Cache) Get(key string) (string, error) {
	it.logger.Debug("+cache.Get")
	defer it.logger.Debug("-cache.Get")

	r, err := it.cache.Get(it.ctx, key).Result()

	if err != nil {
		it.logger.Errorf("cache.Get: %v", err)

		if err == redis.Nil {
			return "", ErrCacheMiss
		}

		return "", ErrCacheGet
	}

	return r, nil
}
