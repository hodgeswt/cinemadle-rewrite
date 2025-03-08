package datamodel

type GuessOptions struct {
	YearYellowThreshold   int `json:"yearYellowThreshold"`
	RatingYellowThreshold int `json:"ratingYellowThreshold"`
	GenreYellowThreshold  int `json:"genreYellowThreshold"`
	CastYellowThreshold   int `json:"castYellowThreshold"`
	CrewYellowThreshold   int `json:"crewYellowThreshold"`
}

func (it *GuessOptions) Validate() bool {
	if it.YearYellowThreshold <= 0 {
		return false
	}

	if it.RatingYellowThreshold <= 0 {
		return false
	}

	if it.GenreYellowThreshold <= 0 {
		return false
	}

	if it.CastYellowThreshold <= 0 {
		return false
	}

	if it.CrewYellowThreshold <= 0 {
		return false
	}

	return true
}
