package diffhandlers

import (
	"errors"
	"strconv"
	"strings"

	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/funct"
	"github.com/hodgeswt/utilw/pkg/logw"
	"github.com/wI2L/jsondiff"
)

var (
	ErrInPersonMapping = errors.New("ErrInPersonMapping")
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
	case "genre":
		c = guessOptions.GenreYellowThreshold
	case "crew":
		c = guessOptions.CrewYellowThreshold
	case "cast":
		c = guessOptions.CastYellowThreshold
	default:
		return "", ErrFieldDistanceMap
	}

	if distance < 0 {
		distance = distance * -1
	}

	if distance == 0 {
		return "green", nil
	} else if distance <= c {
		return "yellow", nil
	} else {
		return "grey", nil
	}
}

func personToName(a any) (string, error) {
	person, ok := a.(datamodel.Person)

	if !ok {
		return "", ErrInPersonMapping
	}

	return person.Name, nil
}

func (it *DefaultDiff) HandleMovieDiff(patch jsondiff.Patch, guess *datamodel.Media, guessOptions *datamodel.GuessOptions, logger *logw.Logger) (*datamodel.Guess, error) {
	logger.Debug("+defaultdiff.HandleMovieDiff")
	defer logger.Debug("-defaultdiff.HandleMovieDiff")

	yearDiff := 0
	ratingDiff := 0
	castDiff := 0
	genreDiff := 0

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

			yearDiff = int(guessedYear - targetYear)
			logger.Debugf("defaultDiff.HandleMovieDiff: Found guessed year %d and target year %d, diff: %d", guessedYear, targetYear, yearDiff)

			continue
		}

		if strings.Contains(v.Path, "rating") {
			logger.Debugf("defaultDiff.HandleMovieDiff: Handling rating: %v", v.Value)

			targetRating := mapRating(v.Value.(string))
			guessedRating := mapRating(guess.Rating)

			logger.Debugf("defaultDiff.HandleMovieDiff: Found guessed rating %d and target rating %d", guessedRating, targetRating)

			ratingDiff = guessedRating - targetRating
			continue
		}

		if strings.Contains(v.Path, "cast") {
			logger.Debugf("defaultDiff.HandleMovieDiff: Handling cast: %v", v.Value)

			castDiff++
			continue
		}

		if strings.Contains(v.Path, "genre") {
			logger.Debugf("defaultDiff.HandleMovieDiff: Handling genre: %v", v.Value)

			genreDiff++
			continue
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

	genreColor, err := mapDistance("genre", genreDiff, guessOptions)
	if err != nil {
		return nil, err
	}

	castColor, err := mapDistance("cast", castDiff, guessOptions)
	if err != nil {
		return nil, err
	}

	castNames, err := funct.Map(guess.Cast, personToName)
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
				Values:    []string{guess.Rating},
			},
			"year": datamodel.Field{
				Color:     yearColor,
				Direction: yearDirection,
				Values:    []string{guess.Year},
			},
			"genre": datamodel.Field{
				Color:     genreColor,
				Direction: -2,
				Values:    guess.Genres,
			},
			"cast": datamodel.Field{
				Color:     castColor,
				Direction: -2,
				Values:    castNames,
			},
		},
	}

	return guessOutput, nil
}

var _it IDiffHandler = new(DefaultDiff)
