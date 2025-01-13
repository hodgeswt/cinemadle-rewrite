package datamodel

import "fmt"

type TmdbOptions struct {
	ApiKey          string            `json:"apiKey"`
	SelectionCount  int               `json:"selectionCount"`
	PageLimit       int               `json:"pageLimit"`
	DiscoverOptions map[string]string `json:"discoverOptions"`
}

func (it *TmdbOptions) String() string {
	return fmt.Sprintf("{SelectionCount: %d, PageLimit: %d, DiscoverOptions: %v}", it.SelectionCount, it.PageLimit, it.DiscoverOptions)
}
