//go:build unit
// +build unit

package datamodel

import (
	"fmt"
	"testing"

	"github.com/stretchr/testify/assert"
)

func TestApiKeyNotPrinted_NotNested(t *testing.T) {
	d := map[string]string{
		"testKey": "testValue",
	}

	x := &TmdbOptions{
		ApiKey:          "Do not print",
		PageLimit:       10,
		SelectionCount:  10,
		DiscoverOptions: d,
	}

	xStr := fmt.Sprintf("%v", x)
	assert.Equal(t, "{SelectionCount: 10, PageLimit: 10, DiscoverOptions: map[testKey:testValue]}", xStr)
}

func TestApiKeyNotPrinted_Nested(t *testing.T) {
	d := map[string]string{
		"testKey": "testValue",
	}

	x := &TmdbOptions{
		ApiKey:          "Do not print",
		PageLimit:       10,
		SelectionCount:  10,
		DiscoverOptions: d,
	}

	y := map[string]*TmdbOptions{
		"options": x,
	}

	xStr := fmt.Sprintf("%v", y)
	assert.Equal(t, "map[options:{SelectionCount: 10, PageLimit: 10, DiscoverOptions: map[testKey:testValue]}]", xStr)
}
