package datamodel

type ErrorBundle struct {
	Response *ErrorResponse
	Status   int
}

type ErrorResponse struct {
	Message string `json:"message"`
}
