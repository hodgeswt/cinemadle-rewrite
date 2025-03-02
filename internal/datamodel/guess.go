package datamodel

type Guess struct {
	Colors     map[string]string `json:"colors"`
	Directions map[string]int    `json:"directions"`
}
