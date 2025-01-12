package tmdb

import (
	"errors"
	"fmt"
	"strconv"

	"github.com/cyruzin/golang-tmdb"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type TmdbClient struct {
	client          *tmdb.Client
	selectionCount  int
	pageLimit       int
	logger          *logw.Logger
	discoverOptions map[string]string
}

type TmdbOptions struct {
	ApiKey          string            `json:"apiKey"`
	SelectionCount  int               `json:"selectionCount"`
	PageLimit       int               `json:"pageLimit"`
	DiscoverOptions map[string]string `json:"discoverOptions"`
}

func (it *TmdbOptions) String() string {
	return fmt.Sprintf("{SelectionCount: %d, PageLimit: %d, DiscoverOptions: %v}", it.SelectionCount, it.PageLimit, it.DiscoverOptions)
}

var ErrPageLimitMet = errors.New("ErrPageLimitMet")

func NewTmdbClient(options *TmdbOptions, logger *logw.Logger) (*TmdbClient, error) {
	client, err := tmdb.Init(options.ApiKey)

	if err != nil {
		return nil, err
	}

	logger.Debugf("tmdb.NewTmdbClient: received opts %+v", *options)

	return &TmdbClient{
		client:          client,
		selectionCount:  options.SelectionCount,
		logger:          logger,
		discoverOptions: options.DiscoverOptions,
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

func (it *TmdbClient) GetTopMovieList() ([]int64, error) {
	it.logger.Debug("+tmdb.GetTopMovieList")
	defer it.logger.Debug("-tmdb.GetTopMovieList")

	page := 0

	movies := []int64{}
	count := 0

	for {
		next, err := it.getDiscoverMoviePage(it.discoverOptions, page)

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
