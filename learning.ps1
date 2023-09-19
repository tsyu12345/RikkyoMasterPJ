param(
    [string]$runid = $env:RUN_ID,
    [string]$configpath = $env:CONFIG_PATH,
    [switch]$overwrite = $false
)

if ($overwrite) {
    mlagents-learn $configpath --run-id=$runid --force
} else {
    mlagents-learn $configpath --run-id=$runid --resume
}
```
