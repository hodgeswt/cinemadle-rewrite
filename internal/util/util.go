package util

import (
	"fmt"
	"strconv"
	"strings"

	"github.com/hodgeswt/cinemadle-rewrite/internal/cache"
	"github.com/hodgeswt/cinemadle-rewrite/internal/tmdb"
	"github.com/hodgeswt/utilw/pkg/rand"
)

func IntCsvToIntArr(intCsv string) ([]int64, error) {
	intStrs := strings.Split(intCsv, ",")
	out := []int64{}

	for _, intStr := range intStrs {
		i, err := strconv.ParseInt(intStr, 10, 64)

		if err != nil {
			return nil, err
		}

		out = append(out, i)
	}

	return out, nil
}

func IntArrToIntCsv(ints []int64) string {
	out := ""

	for _, i := range ints {
		out = out + fmt.Sprintf(",%d", i)
	}

	if len(out) == 0 {
		return out
	}

	return out[1:]
}

func MovieIdFromDate(
	date string,
	tmdbClient *tmdb.TmdbClient,
	cache *cache.Cache,
	lcgOpts *rand.LinearCongruentialGeneratorOptions,
) (int64, error) {
	movies := []int64{}

	cacheKey := "topMovies"
	cachedMovies, cacheErr := cache.Get(cacheKey)

	needLoad := true

	if cacheErr == nil {
		movieSplit, err := IntCsvToIntArr(cachedMovies)

		if err == nil {
			needLoad = false
			movies = movieSplit
		}
	}

	if needLoad {
		movieSplit, err := tmdbClient.GetTopMovieList()

		if err != nil {
			return -1, err
		}

		csv := IntArrToIntCsv(movieSplit)
		cache.Set(cacheKey, csv)

		movies = movieSplit
	}

	fmt.Printf("%v\n", movies)

	lcg, err := rand.NewLinearCongruentialGenerator(lcgOpts)

	if err != nil {
		return -1, err
	}

    stripped := strings.ReplaceAll(date, "-", "")
    i, err := strconv.ParseInt(stripped, 10, 64)

    if err != nil {
        return -1, err
    }

    i = i % lcg.Modulus

	return movies[lcg.At(i)], nil
}
