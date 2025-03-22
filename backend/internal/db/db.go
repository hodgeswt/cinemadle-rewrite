package db

import (
	"database/sql"
	"errors"
	"fmt"
	"os"

	"github.com/hodgeswt/utilw/pkg/logw"
	_ "github.com/lib/pq"
)

var (
	ErrNotInitialized = errors.New("ErrNotInitialized")
	ErrConnection     = errors.New("ErrConnection")
	ErrDb             = errors.New("ErrDb")
)

type Db interface {
	Init(logger *logw.Logger)
	Ping() bool
	Exec(statement string, args ...any) bool
	Query(query string, args ...any) ([]map[string]any, error)
}

type Pg struct {
	logger      *logw.Logger
	host        string
	port        string
	user        string
	password    string
	dbName      string
	initialized bool
	connStr     string
}

func (it *Pg) Init(logger *logw.Logger) {
	it.logger = logger
	it.host = os.Getenv("POSTGRES_HOST")
	it.port = os.Getenv("POSTGRES_PORT")
	it.user = os.Getenv("POSTGRES_USER")
	it.password = os.Getenv("POSTGRES_PASSWORD")
	it.dbName = "cinemadle"
	it.connStr = fmt.Sprintf("host=%s port=%s user=%s password=%s dbname=%s sslmode=disable", it.host, it.port, it.user, it.password, it.dbName)

	it.initialized = true
}

func (it *Pg) Exec(statement string, args ...any) bool {
	it.logger.Debug("+db.Exec")
	defer it.logger.Debug("-db.Exec")

	if !it.initialized {
		it.logger.Error("db.Exec: connection not initialized")
		return false
	}

	db, err := sql.Open("postgres", it.connStr)

	if err != nil {
		it.logger.Errorf("Error connecting to db: %v", err)
		return false
	}

	_, err = db.Exec(statement, args...)

	if err != nil {
		it.logger.Errorf("Error executing statement: %v", err)
		return false
	}

	defer db.Close()

	return true
}

func (it *Pg) Query(query string, args ...any) ([]map[string]any, error) {
	it.logger.Debug("+db.Query")
	defer it.logger.Debug("-db.Query")

	if !it.initialized {
		it.logger.Error("db.Query: connection not initialized")
		return nil, ErrNotInitialized
	}

	db, err := sql.Open("postgres", it.connStr)

	if err != nil {
		it.logger.Errorf("db.Query: issue opening connection: %v", err)
		return nil, ErrConnection
	}

	defer db.Close()

	rows, err := db.Query(query, args...)

	if err != nil {
		it.logger.Errorf("db.Query: error executing query: %v", err)
		return nil, ErrDb
	}

	out := []map[string]any{}

	cols, err := rows.Columns()

	if err != nil {
		return nil, ErrDb
	}

	for rows.Next() {
		vals := make([]any, len(cols))
		ptrs := make([]any, len(cols))

		for i := range vals {
			ptrs[i] = &vals[i]
		}

		if err := rows.Scan(ptrs...); err != nil {
			it.logger.Errorf("db.Query: unable to read row: %v", err)
			return nil, ErrDb
		}

		row := make(map[string]any)

		for i, col := range cols {
			v := vals[i]

			if byteV, ok := v.([]byte); ok {
				row[col] = string(byteV)
			} else {
				row[col] = v
			}
		}

		out = append(out, row)
	}

	if err := rows.Err(); err != nil {
		it.logger.Errorf("db.Query: Error scanning rows: %v", err)
		return nil, ErrDb
	}

	return out, nil
}

func (it *Pg) Ping() bool {
	it.logger.Debug("+db.Ping")
	defer it.logger.Debug("-db.Ping")

	if !it.initialized {
		it.logger.Error("db.Ping: connection not initialized")
		return false
	}

	db, err := sql.Open("postgres", it.connStr)

	if err != nil {
		it.logger.Errorf("db.Ping: issue opening connection: %v", err)
		return false
	}

	defer db.Close()

	err = db.Ping()

	if err != nil {
		it.logger.Errorf("db.Ping: unable to ping db: %v", err)
		return false
	}

	return true
}
