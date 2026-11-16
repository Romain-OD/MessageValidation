<#
.SYNOPSIS
    Runs every MessageValidation example in podman (or docker) and reports PASS/FAIL per NuGet package.
.EXAMPLE
    ./examples/run-all.ps1                       # all examples
    ./examples/run-all.ps1 kafka natsnet         # a subset (service name without the "example-" prefix)
#>
param(
    [string[]] $Examples = @(
        'core', 'dataannotations', 'fluentvalidation', 'immediatevalidations', 'onnxruntime',
        'mqttnet', 'sparkplugb', 'rabbitmq', 'kafka', 'natsnet', 'azureservicebus', 'azureeventhubs'),
    [string] $Engine = 'podman',
    [switch] $NoBuild
)

$ErrorActionPreference = 'Continue'
$env:PODMAN_COMPOSE_WARNING_LOGS = 'false'
Push-Location $PSScriptRoot
try {
    if (-not $NoBuild) {
        & $Engine build -q -f Dockerfile -t localhost/messagevalidation-examples:latest ..
        if ($LASTEXITCODE -ne 0) { throw 'Image build failed.' }
        & $Engine build -q -t localhost/messagevalidation-kafka:3.9.1 kafka
        if ($LASTEXITCODE -ne 0) { throw 'Kafka image build failed.' }
    }

    $results = foreach ($example in $Examples) {
        Write-Host "`n=== $example ===" -ForegroundColor Cyan
        & $Engine compose run --rm -T "example-$example" 2>&1 | ForEach-Object { "$_" } | Where-Object { $_ -notmatch '^(>>>>|System\.Management)' } | Out-Host
        $passed = $LASTEXITCODE -eq 0
        & $Engine compose down 2>&1 | Out-Null # stop the brokers so the next example starts clean
        [pscustomobject]@{ Example = $example; Result = if ($passed) { 'PASS' } else { 'FAIL' } }
    }

    $results | Format-Table -AutoSize | Out-Host
    if ($results.Result -contains 'FAIL') { exit 1 }
}
finally {
    Pop-Location
}
