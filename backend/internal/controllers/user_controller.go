package controllers

import (
	"strconv"

	"github.com/gin-gonic/gin"
	"github.com/hodgeswt/cinemadle-rewrite/internal/datamodel"
	"github.com/hodgeswt/cinemadle-rewrite/internal/db"
	"github.com/hodgeswt/cinemadle-rewrite/internal/util"
	"github.com/hodgeswt/utilw/pkg/logw"
)

func LoadGuesses(c *gin.Context, db db.Db, logger *logw.Logger) {
	logger.Debug("+user_controller.LoadGuesses")
	defer logger.Debug("-user_controller.LoadGuesses")

	uid := c.GetHeader("x-uuid")

	if uid == "" {
		logger.Error("user_controller.LoadGuesses: x-uuid header not found")
		c.JSON(400, datamodel.ErrorResponse{
			Message: "X-UUID header must be provided",
		})
		c.Done()
		return
	}

	statement := "SELECT * FROM guesses WHERE guid = $1 AND datetime::date = CURRENT_DATE"
	rows, err := db.Query(statement, uid)

	if err != nil {
		logger.Errorf("user_controller.LoadGuesses: error in db: %v", err)
		c.JSON(500, datamodel.ErrorResponse{
			Message: "Unable to retrieve user information",
		})
		c.Done()
		return
	}

	if len(rows) == 0 {
		c.JSON(204, []any{})
		c.Done()
		return
	}

	guesses := []string{}

	for _, row := range rows {
		dbGuess := &datamodel.DbGuesses{}
		logger.Debugf("user_controller.LoadGuesses: row: %+v", row)
		err = util.MapToStruct(row, dbGuess)

		if err != nil {
			logger.Errorf("user_controller.LoadGuesses: error serializing db response: %v", err)
			c.JSON(500, datamodel.ErrorResponse{
				Message: "Invalid data received from DB",
			})
			c.Done()
			return
		}

		guesses = append(guesses, strconv.Itoa(dbGuess.GuessId))
	}

	c.JSON(200, guesses)
	c.Done()
}
