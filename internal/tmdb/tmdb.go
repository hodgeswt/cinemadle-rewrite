package tmdb

import (
	"os"

	"github.com/cyruzin/golang-tmdb"
)

type TmdbClient struct {
	client *tmdb.Client
}

func NewTmdbClient() (*TmdbClient, error) {
	client, err := tmdb.Init(os.Getenv("TMDB_API_KEY"))

	if err != nil {
		return nil, err
	}

	return &TmdbClient{
		client: client,
	}, nil
}
