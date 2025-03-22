package datamodel

type DbGuesses struct {
	Id       int    `json:"id"`
	Guid     string `json:"guid"`
	GuessId  int    `json:"guess_id"`
	TargetId int    `json:"target_id"`
	Datetime string `json:"datetime"`
}
