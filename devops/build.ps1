param(
	[Parameter(Mandatory = $true)]
	[ValidateNotNullOrEmpty()]
	[string]$Tag,

	[Parameter()]
	[ValidateSet('backend', 'frontend', 'migrator', 'all')]
	[string]$Component = 'all',

    [Parameter()]
    [switch]$e2e,

    [Parameter()]
    [hashtable]$BuildArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Build-CinemadleContainers {
	param(
		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[string]$Tag,

		[Parameter()]
		[ValidateSet('backend', 'frontend', 'migrator', 'all')]
		[string]$Component = 'all',

        [Parameter()]
        [switch]$e2e,

        [Parameter()]
        [hashtable]$BuildArgs
	)

	if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
		throw 'Docker CLI is not available in the current shell context.'
	}

	# Build backend, frontend, and migrator images with a shared tag so deployments stay aligned.
	$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
	$targetDefinitions = @(
		@{ Key = 'backend'; Image = 'cinemadle'; Dockerfile = 'devops/backend/Dockerfile' },
		@{ Key = 'frontend'; Image = 'cinemadle-frontend'; Dockerfile = 'devops/frontend/Dockerfile' },
		@{ Key = 'migrator'; Image = 'cinemadle-migrator'; Dockerfile = 'devops/migrator/Dockerfile' }
	)

	$targetsToBuild = if ($Component -eq 'all') {
		$targetDefinitions
	} else {
		$targetDefinitions | Where-Object { $_.Key -eq $Component }
	}

	$imageSuffix = if ($e2e) { '-e2e' } else { '' }

	foreach ($target in $targetsToBuild) {
		$imageRef = '{0}{1}:{2}' -f $target.Image, $imageSuffix, $Tag
		$dockerfilePath = Join-Path $repoRoot $target.Dockerfile

		Write-Host "Building $imageRef using $($target.Dockerfile)..."

		$dockerArgs = @('build', '--file', $dockerfilePath, '--tag', $imageRef)

		if ($BuildArgs) {
			foreach ($entry in $BuildArgs.GetEnumerator()) {
				$value = if ($null -ne $entry.Value) { $entry.Value } else { '' }
				$dockerArgs += '--build-arg'
				$dockerArgs += ('{0}={1}' -f $entry.Key, $value)
			}
		}

		$dockerArgs += $repoRoot

		docker @dockerArgs | Out-Host

		if ($LASTEXITCODE -ne 0) {
			throw "Docker build failed for $imageRef"
		}
	}

	if ($Component -eq 'all') {
		Write-Host 'All Cinemadle containers built successfully.'
	} else {
		Write-Host "Cinemadle $Component container built successfully."
	}
}

Build-CinemadleContainers -Tag $Tag -Component $Component -e2e:$e2e -BuildArgs $BuildArgs
