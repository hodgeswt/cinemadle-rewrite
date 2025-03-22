-- DB migration guesses generated on 2025-03-22T15:22:46+00:00

CREATE TABLE IF NOT EXISTS guesses (
    id SERIAL PRIMARY KEY,
    guid varchar(36) NOT NULL,
    guess_id int NOT NULL,
    target_id int NOT NULL,
    datetime TIMESTAMPTZ NOT NULL
);

INSERT INTO migration_history (migration)
VALUES ('20250322T1522460000_guesses')
ON CONFLICT (migration) DO NOTHING;
