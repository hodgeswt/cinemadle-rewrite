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

type Media struct {
	Title string `json:"title"`
	Id    string `json:"id"`
}

func MediaOfTheDay(c *gin.Context, tmdbClient *tmdb.TmdbClient, config *datamodel.Config, logger *logw.Logger, cache *cache.Cache) {
	logger.Debug("+media_controller.MediaOfTheDay")
	defer logger.Debug("-media_controller.MediaOfTheDay")

    mediaType := c.Param("type")

	// Only currently supported media type
	if mediaType != "movie" {
		logger.Debugf("media_controller.MediaOfTheDay: found unspported media type %s", mediaType)
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
		logger.Errorf("media_controller.MediaOfTheDay: error loading location %s: %v", location, err)
		c.JSON(500, datamodel.ErrorResponse{
			Message: "Unable to load the requested media. Try again later.",
		})
		c.Done()
		return
	}

	date := c.Param("date")

	dateFormat := "2006-01-02"

	parsed, err := time.ParseInLocation(dateFormat, date, loc)

	cacheKey := fmt.Sprintf("api-v1-media-%s-%s", mediaType, parsed.Format(dateFormat))

	cached, cacheErr := cache.Get(cacheKey)

	if cacheErr == nil {
		var m Media
		err = json.Unmarshal([]byte(cached), &m)

		if err == nil {
			c.JSON(200, m)
			c.Done()
			return
		}
	}

	if err != nil {
		logger.Debugf("media_controller.MediaOfTheDay: found invalid date string %s", date)
		c.JSON(422, datamodel.ErrorResponse{
			Message: "Invalid date provided. Please follow YYYY-MM-DD format",
		})
		c.Done()
		return
	}

	now := time.Now().In(loc)
	parsed = parsed.In(loc)

	nowMidnight := time.Date(now.Year(), now.Month(), now.Day(), 0, 0, 0, 0, loc)
	parsedMidnight := time.Date(parsed.Year(), parsed.Month(), parsed.Day(), 0, 0, 0, 0, loc)

	if parsedMidnight.After(nowMidnight) {
		logger.Debugf("media_controller.MediaOfTheDay: found future date %s", date)
		c.JSON(404, datamodel.ErrorResponse{
			Message: fmt.Sprintf("Why are you requesting a future date? No cheating! Today: %v, Requested: %v", nowMidnight.Format(dateFormat), parsedMidnight.Format(dateFormat)),
		})
		c.Done()
		return
	}

    logger.Debugf("media_controller.MediaOfTheDay: tmdbClient initialized?: %t", tmdbClient.Initialized)

	id, err := util.MovieIdFromDate(date, tmdbClient, cache, &config.RandomizerOptions)
	if err != nil {
		logger.Errorf("media_controller.MediaOfTheDay: error getting movie id: %v", err)
		c.JSON(500, datamodel.ErrorResponse{
			Message: "Unable to get today's movie. Try again later.",
		})
		c.Done()
		return
	}

	r := Media{
		Title: fmt.Sprintf("Now: %v, Requested: %v", now, parsed),
		Id:    fmt.Sprintf("%d", id),
	}

	j, err := json.Marshal(r)

	if err == nil {
		cache.Set(cacheKey, string(j))
	}

	c.JSON(200, r)
	c.Done()
}
