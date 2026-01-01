# Claims Validation System - Architecture & Code Flow Explanation

## High-Level Architecture

This is a **layered, cloud-native insurance claims processing system** built on ASP.NET Core 9.0. Think of it as an automated pipeline that takes in insurance claims, validates them through multiple stages, and makes approval decisions with minimal human intervention.

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client Layer                             │
│  (Web/Mobile Apps, Bot Interface, Swagger UI)                   │
└───────────────────────────────┬─────────────────────────────────┘
                                │ HTTP/REST
┌───────────────────────────────▼─────────────────────────────────┐
│                    API Gateway (Claims.Api)                      │
│  • ClaimsController (CRUD operations)                           │
│  • StatusController (Health checks)                             │
│  • Bot Framework Integration (Conversational UI)                │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│              Application Services (Claims.Services)              │
│  • ClaimsService (Orchestration)                                │
│  • OcrService (Document extraction)                             │
│  • DocumentAnalysisService (Classification)                     │
│  • RulesEngineService (Policy validation)                       │
│  • MlScoringService (Fraud detection)                           │
│  • NotificationService (Communications)                         │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│              Domain Layer (Claims.Domain)                        │
│  • Entities (Claim, Document, Decision, Notification)           │
│  • Enums (Status types)                                         │
│  • DTOs (Data transfer objects)                                 │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│         Infrastructure (Claims.Infrastructure)                   │
│  • ClaimsDbContext (EF Core)                                    │
│  • Entity Configurations                                        │
│  • Repository Pattern                                           │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│              Data Store & External Services                      │
│  • SQL Server/In-Memory DB                                      │
│  • Azure Blob Storage (Documents)                               │
│  • Azure Computer Vision (OCR)                                  │
│  • Azure Communication Services (Email)                         │
│  • Azure ML (Fraud scoring)                                     │
└─────────────────────────────────────────────────────────────────┘
```

## Layer-by-Layer Breakdown

### 1. **Claims.Api** (Presentation Layer)
**Purpose**: Entry point for all client interactions

**Key Components**:
- **ClaimsController**: Handles claim lifecycle operations
  - `POST /api/claims` - Submit new claims with documents
  - `GET /api/claims/{id}/status` - Check processing status
  - `GET /api/claims/user/{userId}` - List user's claims
  - `PUT /api/claims/{id}/status` - Manual review updates

- **StatusController**: System health monitoring
  - `GET /api/status/health` - API health check

**Gotcha**: This layer is **stateless** and **thin** - it just routes requests to services, doesn't contain business logic.

---

### 2. **Claims.Services** (Business Logic Layer)
**Purpose**: Orchestrates the claims processing workflow

**Service Responsibilities**:

| Service | Responsibility |
|---------|---------------|
| `IClaimsService` | **Main orchestrator** - coordinates all other services |
| `IOcrService` | Extracts text from PDFs/images using Azure Computer Vision |
| `IDocumentAnalysisService` | Validates document types (invoice vs. receipt) |
| `IRulesEngineService` | Checks policy limits, coverage rules |
| `IMlScoringService` | Calculates fraud & approval scores (0.0-1.0) |
| `INotificationService` | Sends emails via Azure Communication Services |

**Key Pattern**: These are **interfaces** (contracts) - actual implementations can be swapped out easily (great for testing and Azure service integrations).

---

### 3. **Claims.Domain** (Core Business Models)
**Purpose**: Defines the "language" of the system

**Entities** (Database-backed objects):
- **Claim**: The main entity
  - Stores claim metadata (policy, claimant, amount, scores)
  - Tracks status progression (Submitted → Processing → AutoApproved/Rejected/ManualReview)
  
- **Document**: Attached files
  - Links to Blob Storage (`BlobUri`)
  - Stores OCR results (`ExtractedText`, `OcrConfidence`)
  
- **Decision**: Audit trail
  - Records who/what made the decision (System or SpecialistId)
  - Tracks fraud/approval scores at decision time
  
- **Notification**: Communication log
  - Tracks email delivery status

**Enums** define fixed states:
```csharp
ClaimStatus: Submitted, Processing, AutoApproved, Rejected, ManualReview
DecisionStatus: AutoApprove, Reject, ManualReview
OcrStatus: Pending, Processing, Completed, Failed
```

**DTOs** (Data Transfer Objects):
- `ClaimSubmissionDto` - What clients send when submitting
- `ClaimStatusDto` - What clients receive when checking status
- Used to **decouple** API contracts from database schema

---

### 4. **Claims.Infrastructure** (Data Access Layer)
**Purpose**: Handles all database operations

**Key Component**:
- **ClaimsDbContext**: Entity Framework Core database context
  - Defines `DbSet<Claim>`, `DbSet<Document>`, etc.
  - Uses **Fluent API** for entity configurations (primary keys, relationships, indexes)

**Design Pattern**: Repository Pattern (planned)
- Abstracts database queries behind interfaces
- Makes testing easier (can mock database calls)

---

## Processing Pipeline (The "Happy Path" Flow)

Here's what happens when a claim is submitted:

```
1. Client Submits Claim
   └─> POST /api/claims { policyId, documents[] }
          │
          ▼
2. ClaimsController receives request
   └─> Validates input (model binding)
   └─> Calls ClaimsService.SubmitClaimAsync()
          │
          ▼
3. ClaimsService orchestrates processing:
   ├─> Saves claim to DB (Status: Submitted)
   ├─> Uploads documents to Blob Storage
   │     └─> Returns BlobUri
   ├─> Calls OcrService.ProcessDocumentAsync()
   │     └─> Azure Computer Vision extracts text
   │     └─> Saves ExtractedText + Confidence score
   ├─> Calls DocumentAnalysisService
   │     └─> Validates document types match requirements
   ├─> Calls RulesEngineService
   │     └─> Checks policy limits, coverage rules
   │     └─> Example: totalAmount <= policyLimit
   ├─> Calls MlScoringService
   │     └─> Fraud score (0.0-1.0, higher = more suspicious)
   │     └─> Approval score (0.0-1.0, higher = more likely to approve)
   │
   ├─> Decision Logic:
   │     IF fraudScore < 0.3 AND approvalScore > 0.8
   │        └─> Status = AutoApproved
   │     ELSE IF fraudScore > 0.7
   │        └─> Status = Rejected
   │     ELSE
   │        └─> Status = ManualReview (assign to specialist)
   │
   └─> Calls NotificationService
         └─> Sends email to claimant
         └─> Sends email to specialist (if manual review)
          │
          ▼
4. Return ClaimResponseDto to client
   └─> Contains claimId, status, scores
```

## Critical Design Decisions

### 1. **Asynchronous Processing**
All service methods use `async/await` because:
- OCR can take 5-30 seconds for complex documents
- ML scoring may call external APIs
- Prevents thread blocking in ASP.NET Core

### 2. **Scoring Thresholds**
The system uses **dual scoring** to balance fraud prevention vs. customer experience:

| Condition | Decision | Reasoning |
|-----------|----------|-----------|
| Fraud < 0.3 AND Approval > 0.8 | Auto-Approve | Low risk, high confidence |
| Fraud > 0.7 | Reject | Too risky, deny immediately |
| Everything else | Manual Review | Human judgment needed |

**Gotcha**: These thresholds are **hardcoded** in the business logic - in production, they should be configurable.

### 3. **Database Relationships**
```
Claim (1) ──< Documents (Many)
Claim (1) ──< Decisions (Many) - Audit trail of all decision changes
Claim (1) ──< Notifications (Many) - All sent messages
```

Foreign keys ensure referential integrity (can't have orphaned documents).

### 4. **Azure Service Integration Points**
The system is designed for Azure but uses **interfaces** so you can:
- Use in-memory DB locally → Azure SQL in production
- Mock OCR service in tests → Real Azure Computer Vision in production
- Stub email service → Azure Communication Services in production

## Configuration Structure

The `appsettings.json` uses hierarchical configuration:

```json
{
  "ConnectionStrings": { ... },
  "AzureServices": {
    "ComputerVision": { "Endpoint", "ApiKey" },
    "CommunicationServices": { "ConnectionString" },
    "BlobStorage": { "ConnectionString", "Containers" }
  }
}
```

**Best Practice**: In production, sensitive values (API keys, connection strings) should be stored in **Azure Key Vault**, not config files.

## Current Implementation Status

**What Works Now**:
- ✅ Full API structure with Swagger
- ✅ Database schema with EF Core migrations
- ✅ Service interfaces defined
- ✅ In-memory database for local testing

**What's Stubbed Out** (returns placeholder data):
- 🚧 OCR service (not calling Azure yet)
- 🚧 ML scoring (returns random scores)
- 🚧 Email notifications (just logs to console)

**Why This Design?**: Allows testing the full pipeline locally without Azure dependencies. The interfaces make it easy to "plug in" real implementations later.

## Suggested Improvements

1. **Add Background Processing**: Use Azure Functions or Hangfire for long-running OCR tasks instead of blocking API requests

2. **Implement CQRS**: Separate read (status queries) from write (submit claim) models for better scalability

3. **Add Caching**: Cache policy rules and ML models to reduce external calls

4. **Improve Error Handling**: Add retry policies for transient Azure service failures (use Polly library)

5. **Add Metrics**: Track processing times, approval rates, fraud detection accuracy

---

**Document Version**: 1.0  
**Last Updated**: December 11, 2025  
**Author**: Architecture Team
