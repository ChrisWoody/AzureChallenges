Param (
    [ValidateNotNullOrEmpty()]
    [string] $tenantId = "",

    [ValidateNotNullOrEmpty()]
    [string] $subscriptionId = "",

    [ValidateNotNullOrEmpty()]
    [string]
    $name = "",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("CentralUS", "EastUS", "EastUS2", "NorthCentralUS", "SouthCentralUS", "WestUS", "WestUS2",
      "NorthEurope", "WestEurope", "EastAsia", "SoutheastAsia", "JapanEast", "JapanWest", 
      "BrazilSouth", "AustraliaEast", "AustraliaSoutheast", "CentralIndia", "SouthIndia", "WestIndia")]
    [string] $location = "AustraliaEast"
)

$resourceGroupName = $name + "rg";
$storageName = $name + "str"
$appInsightsName = $name + "ai"
$webAppPlanName = $name + "plan"
$webAppName = $name + "web"

az account set --subscription $subscriptionId

if ((az group exists --name $resourceGroupName) -eq 'false')
{
    Write-Host "Creating the resource group $resourceGroupName"
    az group create --name $resourceGroupName --location $location
}
else
{
    Write-Host "Resource group $resourceGroupName already exists"
}

az storage account create -g $resourceGroupName -n $storageName -l $location --sku Standard_LRS --min-tls-version TLS1_2 --https-only true
$storageAccountConnectionString = (az storage account show-connection-string -g $resourceGroupName -n $storageName | ConvertFrom-Json).connectionString

az appservice plan create -g $resourceGroupName -n $webAppPlanName --sku B1 --location $location

az webapp create -g $resourceGroupName -n $webAppName -p $webAppPlanName --assign-identity -r '"DOTNET|6.0"'
az webapp config set -g $resourceGroupName -n $webAppName --always-on true
az webapp config appsettings set -g $resourceGroupName -n $webAppName --settings TenantId=$tenantId StorageAccountConnctionString=$storageAccountConnectionString

az monitor app-insights component create -g $resourceGroupName -a $appInsightsName -l $location --retention-time 30
az monitor app-insights component connect-webapp -g $resourceGroupName -a $appInsightsName --web-app $webAppName

Write-Host "NOTE need to manually assign the webapp's identity to the appropriate subscriptions and setup the 'easy auth' to the app service"