package datamodel

type GuessOptions struct {
	YearYellowThreshold int `json:"yearYellowThreshold"`
	RatingYellowThreshold int `json:"ratingYellowThreshold"`
}

func (it *GuessOptions) Validate() bool {
	if it.YearYellowThreshold <= 0 {
		return false
	}

	if it.RatingYellowThreshold <= 0 {
		return false
	}

	return true
}
