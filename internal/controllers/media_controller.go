package controllers

import (
	"fmt"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/hodgeswt/cinemadle-rewrite/internal/cache"
	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type Media struct {
	Title string `json:"title"`
}

func MediaOfTheDay(c *gin.Context, config *datamodel.Config, logger *logw.Logger, cache *cache.Cache) {
	mediaType := c.Param("type")

	// only currently supported media type
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
	loc, err := time.LoadLocation(config.Location)

	if err != nil {
		logger.Errorf("media_controller.MediaOfTheDay: error loading location: %v", err)
		c.JSON(500, datamodel.ErrorResponse{
			Message: "Unable to load the requested media. Try again later.",
		})
		c.Done()
		return
	}

	date := c.Param("date")

	dateFormat := "2006-01-02"

	parsed, err := time.ParseInLocation(dateFormat, date, loc)

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

	r := Media{
		Title: fmt.Sprintf("Now: %v, Requested: %v", now, parsed),
	}

	c.JSON(200, r)
	c.Done()
}
