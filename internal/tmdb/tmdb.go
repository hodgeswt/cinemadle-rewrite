package tmdb

import (
	"errors"
	"fmt"
	"reflect"
	"strconv"
	"strings"

	"github.com/cyruzin/golang-tmdb"
	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/funct"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type TmdbClient struct {
	client           *tmdb.Client
	selectionCount   int
	pageLimit        int
	logger           *logw.Logger
	discoverOptions  map[string]string
	castAndCrewLimit int
	Initialized      bool
}

var (
	ErrPageLimitMet         = errors.New("ErrPageLimitMet")
	ErrClientNotInitialized = errors.New("ErrClientNotInitialized")
	ErrInTmdbRequest        = errors.New("ErrInTmdbRequest")
)

func NewTmdbClient(options *datamodel.TmdbOptions, logger *logw.Logger) (*TmdbClient, error) {
	client, err := tmdb.Init(options.ApiKey)

	if err != nil {
		return nil, err
	}

	logger.Debugf("tmdb.NewTmdbClient: received opts %+v", options)

	return &TmdbClient{
		client:           client,
		selectionCount:   options.SelectionCount,
		pageLimit:        options.PageLimit,
		logger:           logger,
		discoverOptions:  options.DiscoverOptions,
		castAndCrewLimit: options.CastAndCrewLimit,
		Initialized:      true,
	}, nil
}

// Converts genre type to string of genre name
func genreMapper(x any) (string, error) {
	v := reflect.ValueOf(x)
	nameField := v.FieldByName("Name")
	if !nameField.IsValid() || nameField.Kind() != reflect.String {
		return "", ErrInTmdbRequest
	}
	return nameField.String(), nil
}

// Converts person record to name and role
func personMapper(x any) (datamodel.Person, error) {
	v := reflect.ValueOf(x)
	nameField := v.FieldByName("Name")
	roleField := v.FieldByName("KnownForDepartment")

	if !nameField.IsValid() || nameField.Kind() != reflect.String || !roleField.IsValid() || roleField.Kind() != reflect.String {
		return datamodel.Person{}, ErrInTmdbRequest
	}

	return datamodel.Person{
		Name: nameField.String(),
		Role: roleField.String(),
	}, nil
}

// Gets movie from TMDB by ID, converts to our internal datamodel
func (it *TmdbClient) GetMovie(movieId int64) (*datamodel.Media, error) {
	it.logger.Debug("+tmdb.GetMovie")
	defer it.logger.Debug("-tmdb.GetMovie")

	id := int(movieId)

	if !it.Initialized {
		return nil, ErrClientNotInitialized
	}

	movieDetails, err := it.client.GetMovieDetails(id, nil)

	if err != nil {
		it.logger.Errorf("tmdb.GetMovie: error in request %v", err)
		return nil, ErrInTmdbRequest
	}

	movieCredits, err := it.client.GetMovieCredits(id, nil)

	if err != nil {
		it.logger.Errorf("tmdb.GetMovie: error in request %v", err)
		return nil, ErrInTmdbRequest
	}

	movieReleases, err := it.client.GetMovieReleaseDates(id)
	if err != nil {
		it.logger.Errorf("tmdb.GetMovie: error in request %v", err)
		return nil, ErrInTmdbRequest
	}

	movie := new(datamodel.Media)

	movie.Title = movieDetails.Title
	movie.Id = id
	movie.Year = strings.Split(movieDetails.ReleaseDate, "-")[0]

	mappedGenres, err := funct.Map(movieDetails.Genres, genreMapper)
	if err != nil {
		it.logger.Errorf("tmdb.GetMovie: error formatting genres")
		return nil, ErrInTmdbRequest
	}
	movie.Genres = mappedGenres

	cast, err := funct.Map(movieCredits.Cast, personMapper)
	if err != nil {
		it.logger.Errorf("tmdb.GetMovie: error formatting cast")
		return nil, ErrInTmdbRequest
	}

	limit := min(it.castAndCrewLimit, len(cast)-1)
	if limit < 0 {
		it.logger.Errorf("tmdb.GetMovie: insufficient cast info")
		return nil, ErrInTmdbRequest
	}
	movie.Cast = cast[0:limit]

	crew, err := funct.Map(movieCredits.Crew, personMapper)
	if err != nil {
		it.logger.Errorf("tmdb.GetMovie: error formatting crew")
		return nil, ErrInTmdbRequest
	}

	limit = min(it.castAndCrewLimit, len(crew)-1)
	if limit < 0 {
		it.logger.Errorf("tmdb.GetMovie: insufficient cast info")
		return nil, ErrInTmdbRequest
	}
	movie.Crew = crew[0:limit]

	movie.Rating = "UNK"
	for _, release := range movieReleases.Results {
		if release.Iso3166_1 != "US" {
			continue
		}

		found := false
		for _, rating := range release.ReleaseDates {
			if rating.Certification != "" {
				movie.Rating = rating.Certification
				found = true
				break
			}
		}

		if found {
			break
		}
	}

	movie.ImageUrl = fmt.Sprintf("https://image.tmdb.org/t/p/original%s", movieDetails.BackdropPath)

	return movie, nil
}

// Gets page of results from discover movie from TMDB api
func (it *TmdbClient) getDiscoverMoviePage(params map[string]string, page int) ([]int64, error) {
	if !it.Initialized {
		return nil, ErrClientNotInitialized
	}

	if (page - 1) >= it.pageLimit {
		it.logger.Debugf("tmdb.getDiscoverMoviePage: met or exceeded page limit: %d/%d", page, it.pageLimit)
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

// Gets the Top X movies as determined by configuration
func (it *TmdbClient) GetTopMovieList() ([]int64, error) {
	it.logger.Debug("+tmdb.GetTopMovieList")
	defer it.logger.Debug("-tmdb.GetTopMovieList")

	if !it.Initialized {
		return nil, ErrClientNotInitialized
	}

	page := 1

	movies := []int64{}
	count := 0

	for {
		it.logger.Debugf("tmdb.GetTopMovieList: requesting page %d", page)
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

		page++
	}

	return movies, nil
}
