param(
    [Parameter()]
    [switch]$e2e
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Update-Cinemadle {
    param(
        [Parameter()]
        [switch]$e2e
    )

    docker compose down


    if ($e2e) {
        ./devops/build.ps1 -Tag "latest" -Component "all" -e2e
    } else {
        docker pull hodgeswt/cinemadle:latest
        docker pull hodgeswt/cinemadle-frontend:latest
    }

    $composeFile = if ($e2e) { "docker-compose.e2e.yaml" } else { "docker-compose.yaml" }

    # ./devops/migrate.ps1 -e2e:$e2e
    docker compose -f $composeFile up -d
}

Update-Cinemadle -e2e:$e2e