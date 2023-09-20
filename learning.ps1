param(
    [switch]$overwrite = $false
)
#runidとconfigpathをプリント
Write-Host "--run-id: $env:RUN_ID"
Write-Host "config yaml path: $env:CONFIG_PATH"
Write-Host "--force : $overwrite"

if($overwrite){
    mlagents-learn $env:CONFIG_PATH --run-id=$env:RUN_ID --force
} else {
    mlagents-learn $env:CONFIG_PATH --run-id=$env:RUN_ID
}