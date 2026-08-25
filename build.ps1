param(
    [ValidateSet('Build', 'Restore', 'Test')]
    [string]$Target = 'Build'
)

$ErrorActionPreference = 'Stop'

dotnet restore SimpleShop.slnx
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Target -eq 'Restore') {
    exit 0
}

dotnet build SimpleShop.slnx --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Target -eq 'Test') {
    dotnet test SimpleShop.slnx --no-build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
