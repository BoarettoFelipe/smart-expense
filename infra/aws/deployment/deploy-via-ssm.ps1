[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ConfigPath,
    [string]$InstanceId = 'i-0604d67bb952209ae',
    [string]$Profile = 'smart-expense-demo',
    [string]$ParametersPath = (Join-Path $env:TEMP 'smart-expense-ssm-parameters.json'),
    [switch]$Execute
)
$ErrorActionPreference = 'Stop'
if ($InstanceId -notmatch '^i-[0-9a-f]+$') { throw 'Invalid instance ID.' }
$configText = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $ConfigPath))
$allowedKeys = @('AWS_REGION','ECR_REGISTRY','IMAGE_TAG','RDS_HOST','RDS_PORT','RDS_DATABASE','RDS_USERNAME','DB_PASSWORD_PARAMETER','JWT_SIGNING_KEY_PARAMETER')
$config = @{}
foreach ($line in ($configText -split '\r?\n')) {
    if ($line -eq '' -or $line.StartsWith('#')) { continue }
    if (-not ($line -match '^([A-Z_]+)=(.*)$')) {
        throw 'Config must contain only approved non-secret KEY=value entries.'
    }
    if ($Matches[1] -notin $allowedKeys) { throw 'Unsupported config key.' }
    if ($config.ContainsKey($Matches[1])) { throw 'Duplicate config key.' }
    $config[$Matches[1]] = $Matches[2]
}
if ($config.Count -ne $allowedKeys.Count -or $config['AWS_REGION'] -ne 'us-east-1') {
    throw 'Missing inputs or unexpected region.'
}
if ($config['ECR_REGISTRY'] -notmatch '^([0-9]{12})\.dkr\.ecr\.us-east-1\.amazonaws\.com$') {
    throw 'Invalid registry.'
}
$expectedAccount = $Matches[1]
$releasePath = '/opt/smart-expense/releases/' + [Guid]::NewGuid().ToString('N')
$commands = @('set +x', 'set -eu', "install -d -m 0700 '$releasePath'")
$files = @{}
foreach ($name in @('compose.yaml','deploy.sh','prepare-runtime.py')) {
    $files[$name] = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $name)).Replace("`r`n", "`n")
}
$files['deployment.env'] = $configText.Replace("`r`n", "`n")
foreach ($name in $files.Keys) {
    $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($files[$name]))
    $commands += "printf '%s' '$encoded' | base64 --decode > '$releasePath/$name'"
    $commands += "chmod 0600 '$releasePath/$name'"
}
$commands += "bash '$releasePath/deploy.sh' '$releasePath/deployment.env'"
$parameters = @{ commands = $commands; executionTimeout = @('900') }
[IO.File]::WriteAllText([IO.Path]::GetFullPath($ParametersPath), ($parameters | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
Write-Output "Non-secret SSM command payload prepared: $ParametersPath"
if (-not $Execute) {
    Write-Output 'No AWS/remote command executed. Review the payload, then explicitly use -Execute.'
    return
}
$env:AWS_CLI_AUTO_PROMPT = 'off'
$identityJson = & aws sts get-caller-identity --profile $Profile --region us-east-1 --no-cli-pager --output json
if ($LASTEXITCODE -ne 0) { throw 'Identity check failed; no deployment command sent. Renew SSO manually if required.' }
if (($identityJson | ConvertFrom-Json).Account -ne $expectedAccount) { throw 'AWS account does not match the registry.' }
$commandId = & aws ssm send-command --profile $Profile --region us-east-1 --instance-ids $InstanceId --document-name AWS-RunShellScript --parameters "file://$([IO.Path]::GetFullPath($ParametersPath))" --comment 'Manual SmartExpense deployment' --query 'Command.CommandId' --output text --no-cli-pager
if ($LASTEXITCODE -ne 0) { throw 'SSM command submission failed.' }
Write-Output "SSM CommandId: $commandId"
Write-Output 'Monitor GetCommandInvocation status; successful submission is not successful deployment.'
