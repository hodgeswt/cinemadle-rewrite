package diffhandlers

import (
	"errors"

	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/utilw/pkg/logw"
	"github.com/wI2L/jsondiff"
)

type IDiffHandler interface {
	HandleMovieDiff(patch jsondiff.Patch, guess *datamodel.Media, guessOptions *datamodel.GuessOptions, logger *logw.Logger) (*datamodel.Guess, error)
}

var (
	ErrIntParse         = errors.New("ErrIntParse")
	ErrFieldDistanceMap = errors.New("ErrFieldDistanceMap")
)
