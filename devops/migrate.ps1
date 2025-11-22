param(
    [Parameter()]
    [switch]$e2e
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Start-CinemadleMigration {
    param(
        [Parameter()]
        [switch]$e2e
    )

    if ($e2e) {
        docker run --rm -v ./volumes:/root/.local/share/AppData cinemadle-migrator-e2e:latest
    } else {
        docker pull hodgeswt/cinemadle-migrator:latest
        docker run --rm -v ./AppData:/root/.local/share/AppData hodgeswt/cinemadle-migrator:latest
    }
}

Start-CinemadleMigration -e2e:$e2e