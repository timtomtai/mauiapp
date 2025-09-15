# PowerShell script to sync code changes to Microsoft Fabric Workspace
# This script handles authentication and API calls to Fabric workspace

param(
    [Parameter(Mandatory=$true)]
    [string]$WorkspaceId,
    
    [Parameter(Mandatory=$true)]
    [string]$TenantId,
    
    [Parameter(Mandatory=$true)]
    [string]$ClientId,
    
    [Parameter(Mandatory=$true)]
    [string]$ClientSecret
)

# Function to get access token for Fabric API
function Get-FabricAccessToken {
    param(
        [string]$TenantId,
        [string]$ClientId,
        [string]$ClientSecret
    )
    
    try {
        $tokenUrl = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token"
        $scope = "https://api.fabric.microsoft.com/.default"
        
        $body = @{
            client_id = $ClientId
            client_secret = $ClientSecret
            scope = $scope
            grant_type = "client_credentials"
        }
        
        Write-Host "Requesting access token from Azure AD..."
        $response = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
        
        if ($response.access_token) {
            Write-Host "Successfully obtained access token"
            return $response.access_token
        } else {
            throw "Failed to obtain access token"
        }
    } catch {
        Write-Error "Error obtaining access token: $($_.Exception.Message)"
        throw
    }
}

# Function to create or update a notebook in Fabric workspace
function Sync-CodeToFabricNotebook {
    param(
        [string]$AccessToken,
        [string]$WorkspaceId,
        [string]$NotebookName,
        [string]$CodeContent
    )
    
    try {
        $headers = @{
            'Authorization' = "Bearer $AccessToken"
            'Content-Type' = 'application/json'
        }
        
        # Check if notebook exists
        $notebooksUrl = "https://api.fabric.microsoft.com/v1/workspaces/$WorkspaceId/notebooks"
        Write-Host "Checking existing notebooks in workspace..."
        
        $notebooks = Invoke-RestMethod -Uri $notebooksUrl -Headers $headers -Method Get
        $existingNotebook = $notebooks.value | Where-Object { $_.displayName -eq $NotebookName }
        
        if ($existingNotebook) {
            # Update existing notebook
            Write-Host "Updating existing notebook: $NotebookName"
            $notebookId = $existingNotebook.id
            $updateUrl = "https://api.fabric.microsoft.com/v1/workspaces/$WorkspaceId/notebooks/$notebookId/definition"
            
            $notebookDefinition = @{
                definition = @{
                    parts = @(
                        @{
                            path = "notebook-content.py"
                            payload = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($CodeContent))
                            payloadType = "InlineBase64"
                        }
                    )
                }
            }
            
            $body = $notebookDefinition | ConvertTo-Json -Depth 10
            Invoke-RestMethod -Uri $updateUrl -Headers $headers -Method Post -Body $body
            Write-Host "Successfully updated notebook: $NotebookName"
        } else {
            # Create new notebook
            Write-Host "Creating new notebook: $NotebookName"
            $createBody = @{
                displayName = $NotebookName
                type = "Notebook"
            } | ConvertTo-Json
            
            $createResponse = Invoke-RestMethod -Uri $notebooksUrl -Headers $headers -Method Post -Body $createBody
            Write-Host "Successfully created notebook: $NotebookName"
            
            # Update the newly created notebook with content
            Start-Sleep -Seconds 2
            $notebookId = $createResponse.id
            $updateUrl = "https://api.fabric.microsoft.com/v1/workspaces/$WorkspaceId/notebooks/$notebookId/definition"
            
            $notebookDefinition = @{
                definition = @{
                    parts = @(
                        @{
                            path = "notebook-content.py"
                            payload = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($CodeContent))
                            payloadType = "InlineBase64"
                        }
                    )
                }
            }
            
            $body = $notebookDefinition | ConvertTo-Json -Depth 10
            Invoke-RestMethod -Uri $updateUrl -Headers $headers -Method Post -Body $body
            Write-Host "Successfully updated notebook content"
        }
    } catch {
        Write-Error "Error syncing code to Fabric notebook: $($_.Exception.Message)"
        throw
    }
}

# Function to generate summary of changes
function Get-ChangesSummary {
    try {
        # Get git commit info
        $commitHash = & git rev-parse HEAD
        $commitMessage = & git log -1 --pretty=format:"%s"
        $author = & git log -1 --pretty=format:"%an"
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        
        $summary = @"
# MAUI App Code Sync - $timestamp

## Latest Commit Information
- **Commit Hash:** $commitHash
- **Author:** $author
- **Message:** $commitMessage
- **Sync Timestamp:** $timestamp

## Synchronized Files
This notebook contains the latest version of the MAUI application code from the main branch.

## C# MAUI Application Code

```csharp
// Main application files synchronized from Azure DevOps main branch
// Last updated: $timestamp
// Commit: $commitHash

"@
        
        # Read main application files and add them to the summary
        $mainFiles = @("App.xaml.cs", "MainPage.xaml.cs", "MauiProgram.cs")
        
        foreach ($file in $mainFiles) {
            $filePath = Join-Path $pwd $file
            if (Test-Path $filePath) {
                $fileContent = Get-Content $filePath -Raw
                $summary += @"

// === $file ===
$fileContent

"@
            }
        }
        
        $summary += @"
```

## Integration Status
- ✅ Successfully synchronized from Azure DevOps main branch
- ✅ Latest commit: $commitHash
- ✅ Sync completed at: $timestamp
"@
        
        return $summary
    } catch {
        Write-Error "Error generating changes summary: $($_.Exception.Message)"
        return "Error generating summary: $($_.Exception.Message)"
    }
}

# Main execution
try {
    Write-Host "Starting Fabric Workspace synchronization..."
    Write-Host "Workspace ID: $WorkspaceId"
    Write-Host "Tenant ID: $TenantId"
    
    # Get access token
    $accessToken = Get-FabricAccessToken -TenantId $TenantId -ClientId $ClientId -ClientSecret $ClientSecret
    
    # Generate changes summary
    $changesSummary = Get-ChangesSummary
    
    # Sync to Fabric notebook
    $notebookName = "MAUI-App-Sync-$(Get-Date -Format 'yyyyMMdd')"
    Sync-CodeToFabricNotebook -AccessToken $accessToken -WorkspaceId $WorkspaceId -NotebookName $notebookName -CodeContent $changesSummary
    
    Write-Host "Successfully synchronized changes to Fabric workspace!"
    Write-Host "Notebook name: $notebookName"
    
} catch {
    Write-Error "Synchronization failed: $($_.Exception.Message)"
    exit 1
}