package diffhandlers

import (
	"errors"

	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/logw"
)

type IDiffHandler interface {
	HandleMovieDiff(guess *datamodel.Media, target *datamodel.Media, guessOptions *datamodel.GuessOptions, logger *logw.Logger) (*datamodel.Guess, error)
}

var (
	ErrIntParse         = errors.New("ErrIntParse")
	ErrFieldDistanceMap = errors.New("ErrFieldDistanceMap")
)
