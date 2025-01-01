package server

import (
	"errors"
	"fmt"
	"strconv"

	"github.com/hodgeswt/utilw/pkg/argparse"
)

var PORTARG = "port"

type PortArg struct {
	value string
	parsed bool
}

func (it *PortArg) Name() string {
	return PORTARG
}

func (it *PortArg) Value() []string {
	return []string{it.value}
}

func (it *PortArg) Parameters() int {
	return 1
}

func (it *PortArg) IsRequired() bool {
	return false
}

func (it *PortArg) Parsed() bool {
	return it.parsed
}

func (it *PortArg) Valid() bool {
	return it.parsed && it.value != ""
}

func (it *PortArg) Parse(arg string, data ...string) error {
	if !(arg == "-p" || arg == "--port") {
		return argparse.NoMatch
	}

	if len(data) != 1 {
		m := fmt.Sprintf("Invalid arguments provided for port: %v", data)
		return errors.New(m)
	}

    _, err := strconv.Atoi(data[0])

    if err != nil {
        m := fmt.Sprintf("Non-numeric argument provided for port: %s", data[0])
        return errors.New(m)
    }

    it.value = data[0]
    it.parsed = true

    return nil
}

func (it *PortArg) String() string {
    m := "{Argument: %s, Required: %t, Parameters: %v"

    if it.Parsed() {
        m = m + ", Value: %s}"
        return fmt.Sprintf(m, it.Name(), it.IsRequired(), it.Parameters(), it.Value())
    }

    m = m + "}"

    return fmt.Sprintf(m, it.Name(), it.IsRequired(), it.Parameters())
}
