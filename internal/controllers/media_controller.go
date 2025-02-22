package controllers

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/hodgeswt/cinemadle-rewrite/internal/cache"
	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/cinemadle-rewrite/internal/tmdb"
	"github.com/hodgeswt/cinemadle-rewrite/internal/util"
	"github.com/hodgeswt/utilw/pkg/logw"
)

func GetMediaOfTheDay(mediaType string, date string, tmdbClient *tmdb.TmdbClient, config *datamodel.Config, logger *logw.Logger, cache *cache.Cache) (*datamodel.Media, *datamodel.ErrorBundle) {
	logger.Debug("+media_controller.GetMediaOfTheDay")
	defer logger.Debug("-media_controller.GetMediaOfTheDay")

	// Only currently supported media type
	if mediaType != "movie" {
		logger.Debugf("media_controller.GetMediaOfTheDay: found unspported media type %s", mediaType)
		return nil, &datamodel.ErrorBundle{
			Response: &datamodel.ErrorResponse{
				Message: "Please specify a valid media type. Currently accepted values: 'movie'",
			},
			Status: 422,
		}
	}

	location := config.Location
	if location == "" {
		location = "America/New_York"
	}

	logger.Debugf("Converting time for location: %s", location)
	loc, err := time.LoadLocation(location)

	if err != nil {
		logger.Errorf("media_controller.MediaOfTheDay: error loading location %s: %v", location, err)
		return nil, &datamodel.ErrorBundle{
			Response: &datamodel.ErrorResponse{
				Message: "Unable to load the requested media. Try again later.",
			},
			Status: 500,
		}
	}

	dateFormat := "2006-01-02"

	parsed, err := time.ParseInLocation(dateFormat, date, loc)

	cacheKey := fmt.Sprintf("api-v1-media-%s-%s", mediaType, parsed.Format(dateFormat))

	cached, cacheErr := cache.Get(cacheKey)

	if cacheErr == nil {
		var m datamodel.Media
		err = json.Unmarshal([]byte(cached), &m)

		if err == nil {
			return &m, nil
		}
	}

	if err != nil {
		logger.Debugf("media_controller.MediaOfTheDay: found invalid date string %s", date)
		return nil, &datamodel.ErrorBundle{
			Response: &datamodel.ErrorResponse{
				Message: "Invalid date provided. Please follow YYYY-MM-DD format",
			},
			Status: 422,
		}
	}

	now := time.Now().In(loc)
	parsed = parsed.In(loc)

	nowMidnight := time.Date(now.Year(), now.Month(), now.Day(), 0, 0, 0, 0, loc)
	parsedMidnight := time.Date(parsed.Year(), parsed.Month(), parsed.Day(), 0, 0, 0, 0, loc)

	if parsedMidnight.After(nowMidnight) {
		logger.Debugf("media_controller.MediaOfTheDay: found future date %s", date)
		return nil, &datamodel.ErrorBundle{
			Response: &datamodel.ErrorResponse{
				Message: fmt.Sprintf("Why are you requesting a future date? No cheating! Today: %v, Requested: %v", nowMidnight.Format(dateFormat), parsedMidnight.Format(dateFormat)),
			},
			Status: 404,
		}
	}

	logger.Debugf("media_controller.MediaOfTheDay: tmdbClient initialized?: %t", tmdbClient.Initialized)
	logger.Debugf("mediaController.RandomizerOptions: %+v", config.RandomizerOptions)

	id, err := util.MovieIdFromDate(date, tmdbClient, cache, &config.RandomizerOptions)
	if err != nil {
		logger.Errorf("media_controller.MediaOfTheDay: error getting movie id: %v", err)
		return nil, &datamodel.ErrorBundle{
			Response: &datamodel.ErrorResponse{
				Message: "Unable to get today's movie. Try again later.",
			},
			Status: 500,
		}
	}

	movie, err := tmdbClient.GetMovie(id)

	if err != nil {
		logger.Errorf("media_controller.MediaOfTheDay: error getting movie data: %v", err)
		return nil, &datamodel.ErrorBundle{
			Response: &datamodel.ErrorResponse{
				Message: "Unable to get today's movie. Try again later.",
			},
			Status: 500,
		}
	}

	j, err := json.Marshal(movie)

	if err == nil {
		cache.Set(cacheKey, string(j))
	}

	return movie, nil
}

func MediaOfTheDay(c *gin.Context, tmdbClient *tmdb.TmdbClient, config *datamodel.Config, logger *logw.Logger, cache *cache.Cache) {
	logger.Debug("+media_controller.MediaOfTheDay")
	defer logger.Debug("-media_controller.MediaOfTheDay")

	mediaType := c.Param("type")
	date := c.Param("date")

	movie, errorBundle := GetMediaOfTheDay(mediaType, date, tmdbClient, config, logger, cache)

	if movie == nil {
		if errorBundle == nil {
			c.JSON(500, datamodel.ErrorResponse{
				Message: "Unexpected server error.",
			})
			c.Done()
			return
		}

		c.JSON(errorBundle.Status, *errorBundle.Response)
		c.Done()
		return
	}

	c.JSON(200, *movie)
	c.Done()
}
