package diffhandlers

import (
	"strconv"
	"strings"

	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/logw"
	"github.com/wI2L/jsondiff"
)

type DefaultDiff struct{}

var ratingMap = map[string]int{
	"G":     0,
	"PG":    1,
	"PG-13": 2,
	"R":     3,
	"NC-17": 4,
}

func mapRating(rating string) int {
	v, ok := ratingMap[strings.ToUpper(rating)]

	if !ok {
		return -1
	}

	return v
}

func mapDirection(diff int) int {
	if diff == 0 {
		return 0
	}

	if diff < 0 {
		return 1
	}

	return -1
}

func mapDistance(field string, distance int, guessOptions *datamodel.GuessOptions) (string, error) {
	c := -1

	switch field {
	case "rating":
		c = guessOptions.RatingYellowThreshold
	case "year":
		c = guessOptions.YearYellowThreshold
	default:
		return "", ErrFieldDistanceMap
	}

	if c < 0 {
		c = c * -1
	}

	if distance == 0 {
		return "green", nil
	} else if distance <= c {
		return "yellow", nil
	} else {
		return "grey", nil
	}
}

func (it *DefaultDiff) HandleMovieDiff(patch jsondiff.Patch, guess *datamodel.Media, guessOptions *datamodel.GuessOptions, logger *logw.Logger) (*datamodel.Guess, error) {
	logger.Debug("+defaultdiff.HandleMovieDiff")
	defer logger.Debug("-defaultdiff.HandleMovieDiff")

	yearDiff := 0
	ratingDiff := 0

	for _, v := range patch {
		logger.Debugf("defaultdiff.HandleMovieDiff: Considering patch: %v", v)

		if strings.Contains(v.Path, "year") {
			logger.Debugf("defaultDiff.HandleMovieDiff: Handling year %v", v.Value)
			targetYear, err := strconv.ParseInt(v.Value.(string), 10, 64)

			if err != nil {
				logger.Errorf("Error parsing target year from patch: %v", err)
				return nil, ErrIntParse
			}

			guessedYear, err := strconv.ParseInt(guess.Year, 10, 64)

			if err != nil {
				logger.Errorf("Error parsing guessed year from struct: %v", err)
				return nil, ErrIntParse
			}

			logger.Debugf("defaultDiff.HandleMovieDiff: Found guessed year %d and target year %d", guessedYear, targetYear)
			yearDiff = int(guessedYear - targetYear)

			continue
		}

		if strings.Contains(v.Path, "rating") {
			logger.Debugf("defaultDiff.HandleMovieDiff: Handling rating: %v", v.Value)

			targetRating := mapRating(v.Value.(string))
			guessedRating := mapRating(guess.Rating)

			logger.Debugf("defaultDiff.HandleMovieDiff: Found guessed rating %d and target rating %d", guessedRating, targetRating)

			ratingDiff = guessedRating - targetRating
		}

	}

	ratingColor, err := mapDistance("rating", ratingDiff, guessOptions)
	if err != nil {
		return nil, err
	}

	yearColor, err := mapDistance("year", yearDiff, guessOptions)
	if err != nil {
		return nil, err
	}

	ratingDirection := mapDirection(ratingDiff)
	yearDirection := mapDirection(yearDiff)

	guessOutput := &datamodel.Guess{
		Fields: map[string]datamodel.Field{
			"rating": datamodel.Field{
				Color:     ratingColor,
				Direction: ratingDirection,
			},
			"year": datamodel.Field{
				Color:     yearColor,
				Direction: yearDirection,
			},
		},
	}

	return guessOutput, nil
}

var _it IDiffHandler = new(DefaultDiff)
