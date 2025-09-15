# Azure DevOps to Fabric Integration - Architecture Overview

## Integration Flow Diagram

```
┌─────────────────┐    Push to     ┌─────────────────┐
│   Developer     │──────main─────▶│  Azure DevOps   │
│   Local Code    │     branch     │   Repository    │
└─────────────────┘                └─────────────────┘
                                            │
                                            │ Triggers
                                            ▼
                                   ┌─────────────────┐
                                   │ Azure Pipeline  │
                                   │ (azure-         │
                                   │  pipelines.yml) │
                                   └─────────────────┘
                                            │
                                   ┌────────┴────────┐
                                   │                 │
                                   ▼                 ▼
                           ┌──────────────┐  ┌──────────────┐
                           │  Build Stage │  │  Sync Stage  │
                           │   - Restore  │  │   - Fabric   │
                           │   - Build    │  │   - PowerShell│
                           │   - Test     │  │   - Script   │
                           └──────────────┘  └──────────────┘
                                                     │
                                                     │ Calls
                                                     ▼
                                            ┌─────────────────┐
                                            │ SyncToFabric.ps1│
                                            │   - Auth        │
                                            │   - Get Commits │
                                            │   - Upload Code │
                                            └─────────────────┘
                                                     │
                                                     │ API Calls
                                                     ▼
                                            ┌─────────────────┐
                                            │ Microsoft       │
                                            │ Fabric          │
                                            │ Workspace       │
                                            │   - Notebooks   │
                                            │   - Lakehouses  │
                                            └─────────────────┘
```

## File Architecture

```
mauiapp/
├── azure-pipelines.yml              # Azure DevOps pipeline configuration
├── Scripts/
│   └── SyncToFabric.ps1            # PowerShell sync script
├── Services/
│   └── FabricWorkspaceService.cs   # C# runtime integration service
├── fabric-config.json              # Fabric workspace configuration
├── appsettings.template.json       # App configuration template
├── README-FabricIntegration.md     # Setup and usage documentation
└── MauiProgram.cs                  # Updated with DI registration
```

## Key Components

### 1. Azure DevOps Pipeline (`azure-pipelines.yml`)
- **Trigger**: Monitors main branch for changes
- **Build Stage**: Compiles MAUI application
- **Sync Stage**: Executes Fabric synchronization
- **Security**: Uses encrypted pipeline variables

### 2. PowerShell Sync Script (`Scripts/SyncToFabric.ps1`)
- **Authentication**: Azure AD service principal
- **Data Collection**: Git commit metadata
- **API Integration**: Fabric REST API calls
- **Error Handling**: Comprehensive logging

### 3. C# Runtime Service (`Services/FabricWorkspaceService.cs`)
- **HTTP Client**: Fabric API communication
- **Token Management**: Automatic refresh
- **Data Sync**: Application runtime data
- **Dependency Injection**: MAUI service registration

### 4. Configuration Management
- **Pipeline Variables**: Secure credential storage
- **JSON Config**: Workspace and API settings
- **Template Files**: User customization guides

## Security Model

```
┌─────────────────┐
│ Azure DevOps    │
│ Secret Variables│
├─────────────────┤
│ FABRIC_TENANT_ID │
│ FABRIC_CLIENT_ID │
│ FABRIC_SECRET   │
│ WORKSPACE_ID    │
└─────────────────┘
        │
        │ Secured Transport
        ▼
┌─────────────────┐
│ Azure AD        │
│ Service         │
│ Principal       │
└─────────────────┘
        │
        │ OAuth Token
        ▼
┌─────────────────┐
│ Microsoft       │
│ Fabric API      │
│ Endpoints       │
└─────────────────┘
```

## Sync Process Details

1. **Code Change Detection**
   - Git commit hash comparison
   - File modification tracking
   - Metadata extraction

2. **Content Preparation**
   - Source code aggregation
   - Commit information formatting
   - Base64 encoding for API

3. **Fabric Integration**
   - Notebook creation/update
   - Lakehouse data storage
   - Metadata preservation

4. **Verification & Logging**
   - API response validation
   - Error reporting
   - Success confirmation

This architecture ensures secure, automated, and reliable synchronization of your MAUI application changes to Microsoft Fabric workspace.