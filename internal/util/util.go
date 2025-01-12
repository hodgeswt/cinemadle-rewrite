package util

import (
	"fmt"
	"strconv"

	"github.com/hodgeswt/cinemadle-rewrite/internal/cache"
)

func MovieIdFromDate(date string, cache *cache.Cache) (int, error) {
	cacheKey := fmt.Sprintf("movieId-%s", date)
	r, cacheErr := cache.Get(cacheKey)

	if cacheErr == nil {
		id, err := strconv.Atoi(r)

		if err == nil {
			return id, nil
		}
	}



	return 0, nil
}
