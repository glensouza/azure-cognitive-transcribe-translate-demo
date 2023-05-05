param(
    [Parameter()]
    [string]$tenantId,

    [Parameter()]
    [string]$subscriptionId,

    [Parameter()]
    [string]$applicationName,

    [Parameter()]
    [string]$repoName
)

$context = Get-AzContext  
if (!$context)   
{  
    Connect-AzAccount -TenantId $tenantId -SubscriptionId $subscriptionId
}
else
{
    Set-AzContext -TenantId $tenantId -SubscriptionId $subscriptionId
}

New-AzADApplication -DisplayName $applicationName
$clientId = (Get-AzADApplication -DisplayName $applicationName).AppId
$appObjectId = (Get-AzADApplication -DisplayName $applicationName).Id

New-AzADServicePrincipal -ApplicationId $clientId
$objectId = (Get-AzADServicePrincipal -DisplayName $applicationName).Id

New-AzRoleAssignment -ObjectId $objectId -RoleDefinitionName Contributor
$subscriptionId = (Get-AzContext).Subscription.Id
$tenantId = (Get-AzContext).Subscription.TenantId

$subject = 'repo:' + $repoName + ':ref:refs/heads/main'
New-AzADAppFederatedCredential -ApplicationObjectId $appObjectId -Audience api://AzureADTokenExchange -Issuer 'https://token.actions.githubusercontent.com/' -Name 'GitHub-Actions' -Subject $subject

Write-Host "AZURE_CLIENT_ID: $clientId"
Write-Host "AZURE_TENANT_ID: $tenantId"
Write-Host "AZURE_SUBSCRIPTION_ID: $subscriptionId"
