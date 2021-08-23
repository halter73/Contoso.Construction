
param (
    $resourceBaseName,
    $apiManagementOwnerEmail='admin@contoso.com',
    $apiManagementOwnerName='API Admin',
    $sqlServerDbUsername='contoso',
    $sqlServerDbPwd='pass4Sql!PlzChange42',
    $location='westus'
)

Write-Host 'Building .NET 6 minimal API project'
dotnet publish --self-contained -r win-x86 -o publish
Compress-Archive -Path .\publish\*.* -DestinationPath deployment.zip -Force

Write-Host 'Creating resource group'
az group create -l westus -n $resourceBaseName

Write-Host 'Getting active Azure user identity'
$env:identityGuid=$(az ad signed-in-user show --query "objectId")

Write-Host 'Using identity:' $env:identityGuid
az deployment group create --resource-group $resourceBaseName --template-file deploy.bicep --parameters sqlUsername= --parameters sqlPassword= --parameters resourceBaseName=$resourceBaseName --parameters currentUserObjectId=$env:identityGuid --parameters apimPublisherEmail=$apiManagementOwnerEmail --parameters apimPublisherName=$apiManagementOwnerName

Write-Host 'Deploying .NET to Azure Web Apps'
az webapp deploy -n '$($resourceBaseName)web' -g $resourceBaseName --src-path .\deployment.zip
