package tmdb

import (
	"errors"
	"strconv"

	"github.com/cyruzin/golang-tmdb"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type TmdbClient struct {
	client         *tmdb.Client
	selectionCount int
	pageLimit      int
	logger         *logw.Logger
}

type TmdbOptions struct {
	ApiKey         string `json:"apiKey"`
	SelectionCount int    `json:"selectionCount"`
	PageLimit      int    `json:"pageLimit"`
}

var ErrPageLimitMet = errors.New("ErrPageLimitMet")

func NewTmdbClient(options *TmdbOptions, logger *logw.Logger) (*TmdbClient, error) {
	client, err := tmdb.Init(options.ApiKey)

	if err != nil {
		return nil, err
	}

	return &TmdbClient{
		client:         client,
		selectionCount: options.SelectionCount,
		logger:         logger,
	}, nil
}

func (it *TmdbClient) getDiscoverMoviePage(params map[string]string, page int) ([]int64, error) {
	it.logger.Debug("+tmdb.getDiscoverMoviePage")
	defer it.logger.Debug("-tmdb.getDiscoverMoviePage")

	if page >= it.pageLimit {
		it.logger.Debug("tmdb.getDiscoverMoviePage: met or exceeded page limit")
		return nil, ErrPageLimitMet
	}

	params["page"] = strconv.Itoa(page)
	results, err := it.client.GetDiscoverMovie(params)

	if err != nil {
		it.logger.Errorf("tmdb.getDiscoverMoviePage: error with TMDB %v", err)
		return nil, err
	}

	movies := []int64{}

	for _, movie := range results.Results {
		movies = append(movies, movie.ID)
	}

	return movies, nil
}

func (it *TmdbClient) GetTopMovieList(params map[string]string) ([]int64, error) {
	it.logger.Debug("+tmdb.GetTopMovieList")
	defer it.logger.Debug("-tmdb.GetTopMovieList")

	page := 0

	movies := []int64{}
	count := 0

	for {
		next, err := it.getDiscoverMoviePage(params, page)

		if err == ErrPageLimitMet {
			break
		} else if err != nil {
			return nil, err
		}

		for _, movieId := range next {
			if count == it.selectionCount {
				break
			}

			movies = append(movies, movieId)
			count++
		}

		if count == it.selectionCount {
			break
		}
	}

	return movies, nil
}
