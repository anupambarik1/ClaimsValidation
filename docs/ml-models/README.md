# Claims Validation System

A comprehensive ASP.NET Core application on Azure for automated insurance claims processing using OCR, rules validation, ML-based fraud detection, and bot-driven user interaction with automated decisioning.

## Project Structure

```
ClaimsValidation/
├── src/
│   ├── Claims.Api/              # ASP.NET Core Web API
│   ├── Claims.Domain/           # Domain models, entities, DTOs, enums
│   ├── Claims.Services/         # Application services layer
│   └── Claims.Infrastructure/   # Data access, DbContext, configurations
└── ClaimsValidation.sln        # Solution file
```

## Architecture Overview

### Layers
- **API Gateway**: Claims.Api - REST endpoints and Bot Framework integration
- **Application Services**: Business logic orchestration
- **Processing Components**: Tesseract OCR, ML.NET Fraud Detection, Rules Engine
- **Data Layer**: Entity Framework Core with SQL Server/In-Memory DB

### POC AI Stack (Free & Open Source)
- **OCR**: Tesseract 5.2.0 (Apache 2.0 License)
- **ML Fraud Detection**: ML.NET 3.0.1 with FastTree algorithm
- **Email Notifications**: MailKit 4.3.0 with SMTP
- **Cost**: $0 - No cloud dependencies required

### Key Components

#### 1. Claims.Api (ASP.NET Core 9.0 Web API)
- **Controllers**:
  - `ClaimsController` - Submit claims, get status, update claims
  - `StatusController` - Health checks
- **Endpoints**:
  - `POST /api/claims` - Submit new claim
  - `GET /api/claims/{id}/status` - Get claim status
  - `GET /api/claims/user/{userId}` - Get all user claims
  - `PUT /api/claims/{id}/status` - Update claim status (manual review)
  - `GET /api/status/health` - Health check

#### 2. Claims.Domain
- **Entities**: Claim, Document, Decision, Notification
- **Enums**: ClaimStatus, DecisionStatus, OcrStatus, NotificationType, DocumentType
- **DTOs**: ClaimSubmissionDto, ClaimStatusDto, ClaimResponseDto

#### 3. Claims.Services
- **IClaimsService** - Core claim processing orchestration
- **IOcrService** - Document OCR processing (✅ Tesseract OCR)
- **IDocumentAnalysisService** - Document classification and validation
- **IRulesEngineService** - Policy and business rules validation
- **IMlScoringService** - ML-based fraud detection (✅ ML.NET trained model)
- **INotificationService** - Email notifications (✅ MailKit SMTP)

#### 4. Claims.Infrastructure
- **ClaimsDbContext** - EF Core DbContext
- **Entity Configurations** - Fluent API configurations for entities
- **Data Access** - Repository pattern implementation

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- SQL Server (optional - uses in-memory DB by default)
- Azure subscription (for production deployment)

### Running Locally

1. **Clone and restore**:
   ```bash
   cd "c:\Hackathon Projects"
   dotnet restore
   ```

2. **Build the solution**:
   ```bash
   dotnet build
   ```

3. **Run the API**:
   ```bash
   cd src/Claims.Api
   dotnet run
   ```

4. **Access Swagger UI**:
   - Navigate to: `https://localhost:5001/swagger`
   - Or: `http://localhost:5000/swagger`

### Configuration

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "ClaimsDb": "Server=.;Database=ClaimsDb;Trusted_Connection=True;"
  },
  "AzureServices": {
    "ComputerVision": {
      "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
      "ApiKey": "your-api-key"
    },
    "CommunicationServices": {
      "ConnectionString": "endpoint=https://..."
    },
    "BlobStorage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;...",
      "RawDocsContainer": "raw-docs",
      "ProcessedDocsContainer": "processed-docs"
    }
  }
}
```

## API Usage Examples

### Submit a Claim
```http
POST /api/claims
Content-Type: application/json

{
  "policyId": "POL-12345",
  "claimantId": "user@example.com",
  "totalAmount": 1500.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "invoice.pdf",
      "base64Content": "..."
    }
  ]
}
```

### Get Claim Status
```http
GET /api/claims/{claimId}/status
```

Response:
```json
{
  "claimId": "guid",
  "status": "Processing",
  "submittedDate": "2025-12-10T10:00:00Z",
  "lastUpdatedDate": "2025-12-10T10:05:00Z",
  "fraudScore": 0.15,
  "approvalScore": 0.85
}
```

## Database Schema

### Claims Table
- ClaimId (PK, GUID)
- PolicyId (string)
- ClaimantId (string)
- Status (enum: Submitted, Processing, AutoApproved, Rejected, ManualReview)
- SubmittedDate (DateTime)
- LastUpdatedDate (DateTime)
- TotalAmount (decimal)
- FraudScore (decimal?)
- ApprovalScore (decimal?)
- AssignedSpecialistId (string?)

### Documents Table
- DocumentId (PK, GUID)
- ClaimId (FK)
- DocumentType (enum)
- BlobUri (string)
- UploadedDate (DateTime)
- OcrStatus (enum)
- OcrConfidence (decimal?)
- ProcessedBlobUri (string?)
- ExtractedText (string?)

### Decisions Table
- DecisionId (PK, GUID)
- ClaimId (FK)
- DecisionStatus (enum: AutoApprove, Reject, ManualReview)
- DecisionDate (DateTime)
- Reason (string?)
- DecidedBy (string?) - "System" or SpecialistId
- FraudScore (decimal?)
- ApprovalScore (decimal?)

### Notifications Table
- NotificationId (PK, GUID)
- ClaimId (FK)
- RecipientEmail (string)
- NotificationType (enum)
- SentDate (DateTime)
- Status (enum)
- MessageBody (string?)

## Processing Pipeline

```
Claim Submission
    ↓
OCR Processing (Azure Computer Vision)
    ↓
Policy Rules Validation
    ↓
ML Scoring (Fraud + Approval)
    ↓
Decision Logic
    ├── AutoApprove (fraud < 0.3, approval > 0.8)
    ├── Reject (fraud > 0.7)
    └── ManualReview (everything else)
    ↓
Notification (Email)
```

## Technology Stack

- **Backend**: ASP.NET Core 9.0
- **ORM**: Entity Framework Core 9.0
- **Database**: In-Memory (POC) / SQL Server (production)
- **OCR**: Tesseract 5.2.0 (Apache License)
- **ML Framework**: ML.NET 3.0.1 with FastTree
- **Email**: MailKit 4.3.0 + SMTP
- **API Documentation**: Swagger/OpenAPI 6.5.0
- **Logging**: Built-in ASP.NET Core logging
- **DI**: Built-in dependency injection

## Planned Azure Services

1. **Azure App Service** - API hosting
2. **Azure SQL Database** - Production database
3. **Azure Blob Storage** - Document storage
4. **Azure Computer Vision** - OCR processing
5. **Azure Communication Services** - Email notifications
6. **Azure Bot Service** - Conversational interface
7. **Azure Machine Learning** - ML models (fraud detection)
8. **Azure Key Vault** - Secrets management
9. **Application Insights** - Monitoring and telemetry

## Development Status

### ✅ Completed (Working POC)
- [x] Project structure and solution setup
- [x] Domain models (entities, enums, DTOs)
- [x] EF Core DbContext and configurations
- [x] Service interfaces and implementations
- [x] API controllers (Claims, Status)
- [x] Dependency injection setup
- [x] Swagger/OpenAPI documentation
- [x] In-memory database for development
- [x] **Tesseract OCR integration** (real text extraction)
- [x] **ML.NET fraud detection model** (trained and working)
- [x] **MailKit email notifications** (SMTP ready)
- [x] End-to-end claim processing pipeline

### 🚧 Optional Enhancements
- [ ] Azure Computer Vision (higher accuracy OCR)
- [ ] Azure Communication Services (cloud email)
- [ ] Bot Framework integration

### 📋 Planned
- [ ] Authentication (Azure AD B2C)
- [ ] Blob Storage integration
- [ ] Database migrations for SQL Server
- [ ] Unit and integration tests
- [ ] Application Insights integration
- [ ] CI/CD pipeline
- [ ] Docker containerization
- [ ] Kubernetes deployment configurations

## Testing

### Run Tests
```bash
dotnet test
```

### Manual Testing with Swagger
1. Start the API: `dotnet run` in Claims.Api
2. Navigate to `https://localhost:5001/swagger`
3. Test endpoints using Swagger UI

## Deployment

### Azure Deployment (Planned)
1. Provision Azure resources (App Service, SQL, Blob Storage, etc.)
2. Update appsettings.json with Azure connection strings
3. Deploy using Azure DevOps or GitHub Actions
4. Configure managed identities for secure access

## Contributing

1. Create feature branch
2. Implement changes
3. Add tests
4. Submit pull request

## License

[Add License Information]

## Contact

[Add Contact Information]

---

**Last Updated**: December 10, 2025
**Version**: 1.0.0
