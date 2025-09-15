# Azure DevOps to Microsoft Fabric Workspace Integration

This solution enables automatic synchronization of code changes from Azure DevOps main branch to a Microsoft Fabric workspace.

## Overview

When changes are pushed to the main branch in Azure DevOps, a pipeline automatically triggers and synchronizes the latest code and metadata to a Microsoft Fabric workspace. This enables seamless integration between development workflows and data analytics workspaces.

## Setup Instructions

### 1. Azure DevOps Configuration

1. **Import the Pipeline**
   - In your Azure DevOps project, go to Pipelines > Create Pipeline
   - Choose "Existing Azure Pipelines YAML file"
   - Select the `azure-pipelines.yml` file from the repository

2. **Configure Pipeline Variables**
   Set the following variables in your Azure DevOps pipeline:
   
   | Variable Name | Description | Example |
   |---------------|-------------|---------|
   | `FABRIC_WORKSPACE_ID` | Your Fabric workspace GUID | `12345678-1234-1234-1234-123456789012` |
   | `FABRIC_TENANT_ID` | Your Azure AD tenant ID | `87654321-4321-4321-4321-210987654321` |
   | `FABRIC_CLIENT_ID` | Service principal client ID | `abcdef12-3456-7890-abcd-ef1234567890` |
   | `FABRIC_CLIENT_SECRET` | Service principal secret | `your-secret-value` |

   **Important**: Mark `FABRIC_CLIENT_SECRET` as a secret variable.

### 2. Azure AD Service Principal Setup

1. **Create a Service Principal**
   ```bash
   az ad sp create-for-rbac --name "FabricWorkspaceSync" --role Contributor
   ```

2. **Grant Fabric Permissions**
   - Go to the Azure portal
   - Navigate to Azure AD > App registrations
   - Find your service principal
   - Go to API permissions > Add permission
   - Add Microsoft Fabric permissions:
     - `Workspace.ReadWrite.All`
     - `Item.ReadWrite.All`

3. **Configure Fabric Workspace Access**
   - In Microsoft Fabric, go to your workspace
   - Click Settings > Manage access
   - Add the service principal with Admin or Contributor permissions

### 3. Microsoft Fabric Workspace Setup

1. **Get Workspace ID**
   - In Microsoft Fabric, navigate to your workspace
   - The workspace ID is visible in the URL: `https://fabric.microsoft.com/groups/{workspace-id}/`

2. **Enable API Access**
   - Ensure your workspace has API access enabled
   - Configure any necessary firewall rules to allow Azure DevOps agents

### 4. Application Configuration (Optional)

For runtime integration within the MAUI app:

1. **Add Configuration**
   Add to your `appsettings.json` or environment variables:
   ```json
   {
     "FabricWorkspace": {
       "WorkspaceId": "your-workspace-id",
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id",
       "ClientSecret": "your-client-secret"
     }
   }
   ```

2. **Use the Service**
   ```csharp
   // Inject the service in your page/view model
   private readonly FabricWorkspaceService _fabricService;
   
   // Test connection
   var isConnected = await _fabricService.TestConnectionAsync();
   
   // Sync data
   await _fabricService.SyncApplicationDataAsync(myData);
   ```

## How It Works

### Pipeline Workflow

1. **Trigger**: Push to main branch
2. **Build**: Compile and test the MAUI application
3. **Sync**: Execute PowerShell script to sync with Fabric
4. **Notification**: Log results and any errors

### Synchronization Process

The sync process:
1. Authenticates with Azure AD using service principal
2. Gets latest commit information from Git
3. Reads main application files (C# code)
4. Creates or updates a Fabric notebook with the code content
5. Includes metadata like commit hash, author, and timestamp

### Fabric Workspace Artifacts

The sync creates/updates:
- **Notebook**: Contains synchronized code with metadata
- **Lakehouse** (if using runtime service): Stores application data
- **Metadata**: Commit information and sync timestamps

## Troubleshooting

### Common Issues

1. **Authentication Errors**
   - Verify service principal credentials
   - Check Fabric workspace permissions
   - Ensure correct tenant ID

2. **Pipeline Failures**
   - Check Azure DevOps agent has required PowerShell modules
   - Verify network connectivity to Fabric API endpoints
   - Review pipeline logs for specific error messages

3. **Permission Errors**
   - Ensure service principal has Fabric API permissions
   - Check workspace-level access for the service principal
   - Verify tenant admin has enabled Fabric API access

### Logs and Monitoring

- Azure DevOps pipeline logs show detailed sync progress
- Fabric workspace activity logs show API operations
- Application logs (if using runtime service) provide additional details

## Customization

### Modify Sync Content

Edit `Scripts/SyncToFabric.ps1` to:
- Include additional files
- Change notebook format
- Add custom metadata
- Integrate with other Fabric artifacts

### Pipeline Customization

Modify `azure-pipelines.yml` to:
- Change trigger conditions
- Add additional build steps
- Integrate with other systems
- Add notifications (Teams, Slack, etc.)

### Runtime Integration

Use `FabricWorkspaceService` to:
- Sync application data in real-time
- Read data from Fabric workspace
- Implement bi-directional synchronization

## Security Considerations

- Store secrets securely in Azure DevOps variable groups
- Use managed identities where possible
- Regularly rotate service principal secrets
- Monitor access logs and API usage
- Follow principle of least privilege for permissions

## Support

For issues or questions:
1. Check Azure DevOps pipeline logs
2. Review Fabric workspace activity
3. Verify configuration settings
4. Test service principal permissions independently