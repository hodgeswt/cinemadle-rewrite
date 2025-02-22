package controllers

import (
	"encoding/json"
	"fmt"
	"strconv"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/hodgeswt/cinemadle-rewrite/internal/cache"
	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/cinemadle-rewrite/internal/tmdb"
	"github.com/hodgeswt/utilw/pkg/logw"
	"github.com/wI2L/jsondiff"
)

func Guess(c *gin.Context, tmdbClient *tmdb.TmdbClient, config *datamodel.Config, logger *logw.Logger, cache *cache.Cache) {
	logger.Debug("+guess_controller.Guess")
	defer logger.Debug("-guess_controller.Guess")

	mediaType := c.Param("type")
	logger.Debug(fmt.Sprintf("guess_controller.Guess: mediaType %s", mediaType))

	// Only currently supported media type
	if mediaType != "movie" {
		logger.Debugf("guess_controller.Guess: found unspported media type %s", mediaType)
		c.JSON(422, datamodel.ErrorResponse{
			Message: "Please specify a valid media type. Currently accepted values: 'movie'",
		})
		c.Done()
		return
	}

	location := config.Location
	if location == "" {
		location = "America/New_York"
	}

	logger.Debugf("Converting time for location: %s", location)
	loc, err := time.LoadLocation(location)

	if err != nil {
		logger.Errorf("guess_controller.Guess: error loading location %s: %v", location, err)
		c.JSON(500, datamodel.ErrorResponse{
			Message: "Unable to load guess data for requested media. Try again later.",
		})
		c.Done()
		return
	}

	date := c.Param("date")
	logger.Debug(fmt.Sprintf("guess_controller.Guess: date %s", date))

	dateFormat := "2006-01-02"

	parsed, err := time.ParseInLocation(dateFormat, date, loc)

	if err != nil {
		logger.Debugf("guess_controller.Guess: found invalid date string %s", date)
		c.JSON(422, datamodel.ErrorResponse{
			Message: "Invalid date provided. Please follow YYYY-MM-DD format",
		})
		c.Done()
		return
	}

	media, errorBundle := GetMediaOfTheDay(mediaType, date, tmdbClient, config, logger, cache)

	if media == nil {
		if errorBundle == nil {
			c.JSON(500, datamodel.ErrorResponse{
				Message: "Unable to load target media for the day.",
			})
			c.Done()
			return
		}

		c.JSON(errorBundle.Status, *errorBundle.Response)
		c.Done()
		return
	}

	idStr := c.Param("id")
	logger.Debug(fmt.Sprintf("guess_controller.Guess: id %s", idStr))

	id, err := strconv.ParseInt(idStr, 10, 64)

	if err != nil {
		c.JSON(422, datamodel.ErrorResponse{
			Message: "Invalid ID provided; must be int64",
		})
		c.Done()
		return
	}

	cacheKey := fmt.Sprintf("api-v1-guess-%s-%s-%s", mediaType, parsed.Format(dateFormat), idStr)

	cached, cacheErr := cache.Get(cacheKey)

	if cacheErr == nil {
		var g datamodel.Guess
		err = json.Unmarshal([]byte(cached), &g)

		if err == nil {
			c.JSON(200, g)
			c.Done()
			return
		}
	}

	logger.Debugf("guess_controller.Guess: tmdbClient initialized?: %t", tmdbClient.Initialized)
	logger.Debugf("mediaController.RandomizerOptions: %+v", config.RandomizerOptions)

	guessedMedia, err := tmdbClient.GetMovie(id)

	if err != nil {
		logger.Errorf("guess_controller.Guess: error getting movie data: %v", err)
		c.JSON(500, datamodel.ErrorResponse{
			Message: "Unable to get guessed movie by ID (is it valid?)",
		})
		c.Done()
		return

	}

	patch, err := jsondiff.Compare(media, guessedMedia)

	j, err := json.Marshal(patch)

	if err == nil {
		cache.Set(cacheKey, string(j))
	}

	c.JSON(200, patch)
	c.Done()
}
