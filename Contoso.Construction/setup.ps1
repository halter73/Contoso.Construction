param (
    $resourceBaseName,
    $apiManagementOwnerEmail='apiadmin@contoso.com',
    $apiManagementOwnerName='API Admin',
    $sqlServerDbUsername='contoso',
    $sqlServerDbPwd='pass4Sql!PlzChange42'
)

Write-Host 'Getting active Azure user identity'
$env:identityGuid=$(az ad signed-in-user show --query "objectId")

Write-Host 'Using identity:' $env:identityGuid

dotnet publish --self-contained -r win-x86 -o publish
Compress-Archive -Path .\publish\*.* -DestinationPath deployment.zip -Force

Write-Host 'Creating resources using ' $apiManagementOwnerEmail $apiManagementOwnerName $sqlServerDbUsername $sqlServerDbPwd