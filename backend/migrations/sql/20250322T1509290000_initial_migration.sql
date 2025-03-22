-- DB migration initial migration generated on 2025-03-22T15:09:29+00:00
CREATE TABLE IF NOT EXISTS migration_history (
    migration varchar(255) NOT NULL,
    PRIMARY KEY (migration)
);

INSERT INTO migration_history (migration)
VALUES ('20250322T1509290000_initial_migration')
ON CONFLICT (migration) DO NOTHING;
