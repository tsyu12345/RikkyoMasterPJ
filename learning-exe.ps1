param(
    [switch]$overwrite = $false,
    [int]$num_envs = 1
)


# Check if the module is installed
$module = Get-Module -ListAvailable -Name powershell-yaml

# If the module is not installed, elevate permissions and install it
if ($null -eq $module) {
    # Elevate to admin and install the module
    Start-Process -Verb RunAs PowerShell -ArgumentList {
        Install-Module -Name powershell-yaml -Force -SkipPublisherCheck
        Import-Module powershell-yaml
    }
} else {
    Import-Module powershell-yaml
}

# Read the YAML file into a PowerShell object
$config = (Get-Content -Path './learning_config.yaml' | Out-String) | ConvertFrom-Yaml

# Extract the variables from the config object
$run_id = $config.RUN_ID
$config_path = $config.CONFIG_PATH
$exe_path = $config.EXE_PATH

# Print the variables
Write-Host "--run-id: $run_id"
Write-Host "config yaml path: $config_path"
Write-Host "--force : $overwrite"
Write-Host "exe path: $exe_path"
Write-Host "num_envs: $num_envs"

# Execute the mlagents-learn command
if($overwrite){
    mlagents-learn $config_path --run-id=$run_id --force --env=$exe_path --num-envs=$num_envs --width=1280 --height=720
} else {
    mlagents-learn $config_path --run-id=$run_id --resume --env=$exe_path --num-envs=$num_envs --width=1280 --height=720
}
