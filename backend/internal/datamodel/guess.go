package datamodel

type Guess struct {
	Fields map[string]Field `json:"fields"`
}

type Field struct {
	Color     string   `json:"color"`
	Direction int      `json:"direction"`
	Values    []string `json:"values"`
}
