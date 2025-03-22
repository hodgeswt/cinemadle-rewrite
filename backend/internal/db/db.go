package db

import (
	"database/sql"
	"fmt"
	"github.com/hodgeswt/utilw/pkg/logw"
	_ "github.com/lib/pq"
	"os"
)

type Db interface {
	Init(logger *logw.Logger)
	Ping() bool
	Exec(statement string, args ...any) bool
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

	return true
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
