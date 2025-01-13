package datamodel

type Media struct {
	Title    string   `json:"title"`
	Id       int      `json:"id"`
	ImageUrl string   `json:"imageUrl"`
	Cast     []Person `json:"cast"`
	Crew     []Person `json:"crew"`
	Genres   []string `json:"genres"`
	Year     string   `json:"year"`
	Rating   string   `json:"rating"`
}

type Person struct {
	Name string `json:"name"`
	Role string `json:"role"`
}
