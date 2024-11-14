[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$MyUplinkPassword,
    [Parameter(Mandatory = $true)]
    [string]$TibberApiAccessToken,
    [Parameter(Mandatory = $false)]
    [string]$PublishPath = "\\192.168.10.244\pimylifeupshare",
    [Parameter(Mandatory = $false)]
    [string]$RunTime = "linux-arm64"
)

function Update-Appsettings {
    param(
        [string]$MyUplinkPassword,
        [string]$TibberApiAccessToken
    )
    
    $configPath = ".\appsettings.json"

    if (-not (Test-Path $configPath)) {
        Write-Error "Configuration file not found at: $configPath"
        exit 1
    }

    $config = Get-Content -Path $configPath -Raw | ConvertFrom-Json

    $config.UpLinkCredentials.Password = $MyUplinkPassword
    $config.TibberApiClient.AccessToken = $TibberApiAccessToken

    $updatedJson = $config | ConvertTo-Json -Depth 10

    $updatedJson | Set-Content -Path $configPath -Encoding UTF8

    Write-Host "Configuration updated successfully!"
}

if ((Test-Path $PublishPath) -eq $false) {
    throw "Network share '$PublishPath' not found"
}

Push-Location "C:\GIT\other\MyTibberApp\MyTibber.WebUi\MyTibber.WebUi"

Update-Appsettings $MyUplinkPassword $TibberApiAccessToken

dotnet publish --configuration Release --output $PublishPath --runtime $RunTime

Update-Appsettings "" ""

Pop-Location

write-Host "How to run on RP:"
write-Host "screen"
write-Host "dotnet MyTibber.WebUi --urls=http://0.0.0.0:5001/"
write-Host "ctrl A + ctrl D"
