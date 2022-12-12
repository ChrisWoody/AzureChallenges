Param (
    [ValidateNotNullOrEmpty()]
    [string] $subscriptionId = "",

    [ValidateNotNullOrEmpty()]
    [string]
    $name = ""
)

$srcPath = Join-Path -Path $PSScriptRoot -ChildPath '..\src'
$artifactsPath = Join-Path -Path $PSScriptRoot -ChildPath '\artifacts'
$artifactsTempPath = Join-Path -Path $PSScriptRoot -ChildPath '\artifactsTemp'

$resourceGroupName = $name + "rg";
$webAppName = $name + "web"

if (Test-path $artifactsPath) { Remove-Item -Recurse -Force $artifactsPath }
if (Test-path $artifactsTempPath) { Remove-Item -Recurse -Force $artifactsTempPath }
New-Item -Path $artifactsPath -ItemType Directory | Out-Null
New-Item -Path $artifactsTempPath -ItemType Directory | Out-Null

dotnet publish "$srcPath\AzureChallenges\AzureChallenges\AzureChallenges.csproj" -c Release --output "$artifactsTempPath\AzureChallenges\"
Remove-Item "$artifactsTempPath\AzureChallenges\appsettings.Development.json"

Compress-Archive -Path "$artifactsTempPath\AzureChallenges\*" -DestinationPath "$artifactsPath\AzureChallenges.zip" -CompressionLevel Optimal -Force

az account set --subscription $subscriptionId
az webapp deployment source config-zip -g $resourceGroupName -n $webAppName --src "$artifactsPath\AzureChallenges.zip"