# Local disposable database ONLY. Never pass AWS credentials/RDS inputs here.
[CmdletBinding()]
param([string]$DockerExe = 'docker')
$ErrorActionPreference = 'Stop'
$suffix = [Guid]::NewGuid().ToString('N')
$project = 'smart-expense-smoke-' + $suffix
$database = $project + '-db'
$network = $project + '-network'
$directory = Join-Path $env:TEMP $project
[IO.Directory]::CreateDirectory($directory) | Out-Null
$runtimePath = Join-Path $directory 'runtime.env'
$postgresPath = Join-Path $directory 'postgres.env'
$overridePath = Join-Path $directory 'override.yaml'
$key = ('x' * 32) + '$"'
$password = 'fake;"$=' + $suffix
$quotedPassword = '"' + $password.Replace('"', '""') + '"'
$utf8 = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($runtimePath, "ConnectionStrings__DefaultConnection=Host=$database;Port=5432;Database=smoke;Username=smoke;Password=$quotedPassword;SSL Mode=Disable`nJwt__SigningKey=$key`n", $utf8)
[IO.File]::WriteAllText($postgresPath, "POSTGRES_DB=smoke`nPOSTGRES_USER=smoke`nPOSTGRES_PASSWORD=$password`n", $utf8)
$overrides = @"
services:
  migrations:
    image: smart-expense-api:aws-validation-migrations
  api:
    image: smart-expense-api:aws-validation
  frontend:
    image: smart-expense-frontend:aws-validation
    ports: !override
      - "127.0.0.1::8080"
networks:
  default:
    external: true
    name: $network
"@
[IO.File]::WriteAllText($overridePath, $overrides, $utf8)
$names = @('ECR_REGISTRY','IMAGE_TAG','RUNTIME_ENV_FILE','RDS_CA_FILE')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$env:ECR_REGISTRY = '123456789012.dkr.ecr.us-east-1.amazonaws.com'
$env:IMAGE_TAG = 'local-only'
$env:RUNTIME_ENV_FILE = $runtimePath
$env:RDS_CA_FILE = $runtimePath # Dummy public mount; local test deliberately disables TLS.
$compose = @('compose','--project-name',$project,'--env-file', (Join-Path $PSScriptRoot 'deployment.env.example'),'-f',(Join-Path $PSScriptRoot 'compose.yaml'),'-f',$overridePath)
try {
    & $DockerExe network create $network | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Local network creation failed.' }
    & $DockerExe run -d --name $database --network $network --env-file $postgresPath --health-cmd 'pg_isready -U smoke -d smoke' --health-interval 2s --health-retries 30 postgres:18.4-alpine | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Local PostgreSQL startup failed.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $status = & $DockerExe inspect --format '{{.State.Health.Status}}' $database
        if ($status -eq 'healthy') { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Local database health timeout.' }
    & $DockerExe @compose --profile tools config --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Local Compose config failed.' }
    $migrationOutput = & $DockerExe @compose run --rm --no-deps migrations 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Local migrations failed; output withheld.' }
    & $DockerExe @compose up -d --wait --wait-timeout 180 api frontend
    if ($LASTEXITCODE -ne 0) { throw 'Local application health failed.' }
    $apiId = & $DockerExe @compose ps -q api
    $containerEnvironment = (& $DockerExe inspect --format '{{json .Config.Env}}' $apiId) | ConvertFrom-Json
    if (('Jwt__SigningKey=' + $key) -cnotin $containerEnvironment) { throw 'Raw environment value was altered.' }
    $address = (& $DockerExe @compose port frontend 8080).Trim()
    $root = Invoke-WebRequest -Uri "http://$address/" -UseBasicParsing
    if ($root.StatusCode -ne 200) { throw 'Frontend smoke check failed.' }
    $proxyStatus = 0
    try { $proxyStatus = (Invoke-WebRequest -Uri "http://$address/api/transactions" -UseBasicParsing).StatusCode }
    catch { if ($null -ne $_.Exception.Response) { $proxyStatus = [int]$_.Exception.Response.StatusCode } else { throw 'Local proxy unreachable.' } }
    if ($proxyStatus -ne 401) { throw 'API proxy authorization check failed.' }
    # Rerun migrations to prove EF's already-applied path remains successful.
    $migrationOutput = & $DockerExe @compose run --rm --no-deps migrations 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Local migration rerun failed.' }
    & $DockerExe @compose up -d --force-recreate --remove-orphans --wait --wait-timeout 180 api frontend
    if ($LASTEXITCODE -ne 0) { throw 'Local container recreation failed.' }
    $address = (& $DockerExe @compose port frontend 8080).Trim()
    if ((Invoke-WebRequest -Uri "http://$address/" -UseBasicParsing).StatusCode -ne 200) { throw 'Recreated frontend check failed.' }
    $migrationCount = & $DockerExe exec $database psql -U smoke -d smoke -Atc 'SELECT count(*) FROM "__EFMigrationsHistory";'
    if ($LASTEXITCODE -ne 0 -or [int]$migrationCount -ne 3) { throw 'Existing migration history verification failed.' }
    Write-Output 'LOCAL_SMOKE=PASS; migrations=3; migration/container-rerun=PASS; api/frontend=healthy; proxy=401; raw-env=PASS'
}
finally {
    & $DockerExe @compose --profile tools down --remove-orphans | Out-Null
    & $DockerExe rm -f -v $database | Out-Null
    & $DockerExe network rm $network | Out-Null
    foreach ($file in @($runtimePath,$postgresPath,$overridePath)) { Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue }
    Remove-Item -LiteralPath $directory -ErrorAction SilentlyContinue # Empty directory only, never recursive.
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
