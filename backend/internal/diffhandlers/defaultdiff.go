package diffhandlers

import (
	"errors"
	"strconv"
	"strings"

	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/funct"
	"github.com/hodgeswt/utilw/pkg/logw"
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

func listComp[T comparable](a []T, b []T) string {
	if len(a) == len(b) {
		match := true

		for i := range a {
			if a[i] != b[i] {
				match = false
				break
			}
		}

		if match {
			return "green"
		}
	}

	m := map[T]struct{}{}

	for _, x := range b {
		m[x] = struct{}{}
	}

	for _, x := range a {
		if _, ok := m[x]; ok {
			return "yellow"
		}
	}

	return "grey"
}

func (it *DefaultDiff) HandleMovieDiff(guess *datamodel.Media, target *datamodel.Media, guessOptions *datamodel.GuessOptions, logger *logw.Logger) (*datamodel.Guess, error) {
	logger.Debug("+defaultdiff.HandleMovieDiff")
	defer logger.Debug("-defaultdiff.HandleMovieDiff")

	castColor := listComp(guess.Cast, target.Cast)
	genreColor := listComp(guess.Genres, target.Genres)

	targetYear, err := strconv.ParseInt(target.Year, 10, 64)

	if err != nil {
		logger.Errorf("Error parsing target year: %v", err)
		return nil, ErrIntParse
	}

	guessedYear, err := strconv.ParseInt(guess.Year, 10, 64)

	if err != nil {
		logger.Errorf("Error parsing guessed year: %v", err)
		return nil, ErrIntParse
	}

	yearDiff := int(guessedYear - targetYear)

	targetRating := mapRating(target.Rating)
	guessedRating := mapRating(guess.Rating)

	ratingDiff := guessedRating - targetRating

	ratingColor, err := mapDistance("rating", ratingDiff, guessOptions)
	if err != nil {
		return nil, err
	}

	yearColor, err := mapDistance("year", yearDiff, guessOptions)
	if err != nil {
		return nil, err
	}

	castNames, err := funct.Map(guess.Cast, personToName)
	if err != nil {
		return nil, err
	}

	yearDirection := 0

	if yearDiff < -1 * guessOptions.YearTwoArrow {
		yearDirection = 2
	} else if yearDiff < 0 {
		yearDirection = 1
	} else if yearDiff > guessOptions.YearTwoArrow {
		yearDirection = -2
	} else if yearDiff > 0 {
		yearDirection = -1
	}

	guessOutput := &datamodel.Guess{
		Fields: map[string]datamodel.Field{
			"rating": datamodel.Field{
				Color:     ratingColor,
				Direction: 0, // Don't show direction on rating
				Values:    []string{guess.Rating},
			},
			"year": datamodel.Field{
				Color:     yearColor,
				Direction: yearDirection,
				Values:    []string{guess.Year},
			},
			"genre": datamodel.Field{
				Color:     genreColor,
				Direction: 0, // Don't show direction on genre
				Values:    guess.Genres,
			},
			"cast": datamodel.Field{
				Color:     castColor,
				Direction: 0, // Don't show direction on cast
				Values:    castNames,
			},
		},
	}

	return guessOutput, nil
}

var _it IDiffHandler = new(DefaultDiff)
