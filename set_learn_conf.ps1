param(
    [string]$runid,
    [string]$configpath
)

#runidとconfigpathをプリント
Write-Host "Setting --run-id: $runid"
Write-Host "Setting config yaml path: $configpath"


# Set as environment variables
[Environment]::SetEnvironmentVariable('RUN_ID', $runid, [EnvironmentVariableTarget]::User)
[Environment]::SetEnvironmentVariable('CONFIG_PATH', $configpath, [EnvironmentVariableTarget]::User)

# Verify that the environment variables have been set
Write-Host "Environment variable for RUN_ID is set to: $env:RUN_ID"
Write-Host "Environment variable for CONFIG_PATH is set to: $env:CONFIG_PATH"