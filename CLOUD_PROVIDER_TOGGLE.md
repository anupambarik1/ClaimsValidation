# Cloud Provider Toggle (Azure <-> AWS)

## Overview
This project supports toggling provider-specific services (OCR, NLP, Blob storage, Email) between Azure and AWS using configuration-driven feature flags. The codebase provides Azure implementations and lightweight AWS stubs; switch providers via `CloudProvider` in configuration.

## How it works
- Consumers depend on abstractions: `INlpService`, `IBlobStorageService`, `IEmailService`, and `IOcrService`.
- `Program.cs` reads `CloudProvider` and registers provider-specific implementations for these interfaces.
- Set `CloudProvider` to `"Azure"` (default) or `"AWS"` to route flows.

## Configuration
Update `src/Claims.Api/appsettings.json` (or environment variables):

- `CloudProvider`: `Azure` or `AWS` (default: `Azure`)
- `FeatureFlags`: granular toggles for OCR provider, OpenAI, Blob storage, etc.
- `Azure` and `AWS` sections hold provider-specific connection values.

Example (partial):

```
"CloudProvider": "Azure",
"FeatureFlags": {
  "UseAzureDocumentIntelligence": true,
  "UseAzureOpenAI": true,
  "UseAzureBlobStorage": true
},
"Azure": { ... },
"AWS": { "Enabled": false, "Textract": { "Enabled": false } }
```

## Interfaces
- `INlpService` - Summarize, extract entities, analyze fraud, generate responses.
- `IBlobStorageService` - Upload, download, list, move, delete documents.
- `IEmailService` - Send emails and claim/decision notifications.
- `IOcrService` - OCR-specific interface (existing).

Implementations:
- Azure: `AzureOpenAIService` (INlpService), `AzureBlobStorageService` (IBlobStorageService), `AzureEmailService` (IEmailService), `AzureDocumentIntelligenceService` (IOcrService).
- AWS: `AWSNlpService` (INlpService stub), `AWSBlobStorageService` (IBlobStorageService stub), `AWSEmailService` (IEmailService stub), `AWSTextractService` (IOcrService stub).

## To switch providers
1. Open `src/Claims.Api/appsettings.json` or set environment variable `CloudProvider`.
2. Set `CloudProvider` to `AWS` to use AWS stubs, or `Azure` to use Azure implementations.
3. Toggle additional feature flags (e.g., `AWS:Textract:Enabled`) for OCR.
4. Restart the API.

## Implementing full AWS support
AWS stubs are placeholders:
- Replace stubs with AWS SDK integrations: S3 (Blob storage), Textract (OCR), SES (Email), Bedrock (or other model inference) for NLP.
- Ensure credentials and permissions are provided via environment variables, shared credentials file, or secret manager.
- Update DI registration in `Program.cs` only if different concrete types are used.

## Files changed in this work
- Updated DI: `src/Claims.Api/Program.cs`
- Interfaces: `src/Claims.Services/Interfaces/*` (existing)
- Azure: `src/Claims.Services/Azure/AzureEmailService.cs` (now implements `IEmailService`)
- AWS stubs updated to implement interfaces:
  - `src/Claims.Services/Aws/AWSNlpService.cs`
  - `src/Claims.Services/Aws/AWSBlobStorageService.cs`
  - `src/Claims.Services/Aws/AWSEmailService.cs`

## Verification
- Solution builds successfully after these changes:

```bash
dotnet restore "ClaimsValidation.sln"
dotnet build "ClaimsValidation.sln"
```

## Next steps (recommended)
- Replace AWS stubs with full AWS SDK implementations and add unit/integration tests for provider switching.
- Move secrets to Azure Key Vault or AWS Secrets Manager.
- Add runtime telemetry and feature-flag management (e.g., LaunchDarkly) for safer toggles.

---
If you want, I can implement AWS SDK integrations next or add integration tests that validate runtime provider switching.
