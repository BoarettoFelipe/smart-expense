# Offline tests: aws and sleep are mocked; no credentials/network required.
$ErrorActionPreference = 'Stop'
$directory = Join-Path ([IO.Path]::GetTempPath()) ('smart-expense-sender-test-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($directory) | Out-Null
$configPath = Join-Path $directory 'deployment.env'
$payloadPath = Join-Path $directory 'payload.json'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'deployment.env.example') -Destination $configPath
$global:senderTestCalls = @()
$global:senderTestScenario = 'success'
function aws {
    $global:senderTestCalls += ,@($args)
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'sts') {
        if ($global:senderTestScenario -eq 'wrong-account') { return '{"Account":"999999999999"}' }
        return '{"Account":"739275443630"}'
    }
    if ($args[1] -eq 'send-command') { return '00000000-0000-0000-0000-000000000001' }
    if ($global:senderTestScenario -eq 'eventual') {
        $global:senderTestScenario = 'success'
        $global:LASTEXITCODE = 1
        return
    }
    if ($global:senderTestScenario -eq 'failure') { return '{"Status":"Failed","ResponseCode":1}' }
    if ($global:senderTestScenario -eq 'timeout') { return '{"Status":"TimedOut","ResponseCode":-1}' }
    if ($global:senderTestScenario -eq 'nonzero') { return '{"Status":"Success","ResponseCode":1}' }
    return '{"Status":"Success","ResponseCode":0}'
}
function Start-Sleep { param($Seconds) }
function Assert($condition, $message) { if (-not $condition) { throw $message } }
$sender = Join-Path $PSScriptRoot 'deploy-via-ssm.ps1'
try {
    & $sender -ConfigPath $configPath -ParametersPath $payloadPath | Out-Null
    Assert ($global:senderTestCalls.Count -eq 0) 'Prepare-only contacted AWS.'
    $payload = Get-Content -LiteralPath $payloadPath -Raw | ConvertFrom-Json
    Assert ($payload.executionTimeout[0] -eq '900') 'Unexpected execution timeout.'
    Assert ($payload.commands[-1] -match "^bash '/opt/smart-expense/releases/[a-f0-9]{32}/deploy.sh'") 'Invalid remote invocation.'
    $writes = @($payload.commands | Where-Object { $_ -match "^printf '%s' '([^']+)' \| base64 --decode > '([^']+)'$" })
    Assert ($writes.Count -eq 4) 'Wrong embedded files.'
    foreach ($write in $writes) {
        $null = $write -match "^printf '%s' '([^']+)' \| base64 --decode > '([^']+)'$"
        $content = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Matches[1]))
        Assert (-not $content.Contains("`r")) 'Payload must use LF.'
        if ($Matches[2].EndsWith('/deployment.env')) {
            Assert ($content.Contains('IMAGE_TAG=demo-001')) 'Config was not preserved.'
        }
    }
    & $sender -ConfigPath $configPath -ParametersPath $payloadPath -Execute -Wait | Out-Null
    Assert (-not (@($global:senderTestCalls | ForEach-Object { $_ }) -contains '--profile')) 'Ambient credentials unexpectedly use a profile.'
    $global:senderTestCalls = @()
    & $sender -ConfigPath $configPath -ParametersPath $payloadPath -Profile smart-expense-demo -Execute -Wait | Out-Null
    Assert (@($global:senderTestCalls | ForEach-Object { $_ }) -contains 'smart-expense-demo') 'Explicit profile was not preserved.'
    $global:senderTestScenario = 'eventual'
    & $sender -ConfigPath $configPath -ParametersPath $payloadPath -Execute -Wait | Out-Null
    foreach ($scenario in @('failure', 'timeout', 'nonzero', 'wrong-account')) {
        $global:senderTestScenario = $scenario
        $global:senderTestCalls = @()
        $failed = $false
        try { & $sender -ConfigPath $configPath -ParametersPath $payloadPath -Execute -Wait | Out-Null }
        catch { $failed = $true }
        Assert $failed "Scenario $scenario did not fail."
        if ($scenario -eq 'wrong-account') {
            Assert ($global:senderTestCalls.Count -eq 1) 'Wrong account sent a remote command.'
        }
    }
    Write-Output 'Sender: 8 scenarios passed; AWS calls mocked; payload verified.'
}
finally {
    Remove-Item -LiteralPath $configPath, $payloadPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $directory
}
