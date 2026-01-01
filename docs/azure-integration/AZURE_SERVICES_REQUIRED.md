# Azure Services Required for Capstone Implementation

**Project**: Claims Validation System - Phase 1 Implementation  
**Date**: January 1, 2026  
**Scope**: 5 Core Features Implementation

---

## 📋 Summary of Required Azure Services

| Service | Feature | Purpose | Estimated Cost | Setup Time |
|---------|---------|---------|-----------------|------------|
| **Azure OpenAI** | NLP Integration | Claim summarization, entity extraction, fraud analysis | $0.03/1K tokens | 15 min |
| **Azure AI Document Intelligence** | Document Intelligence | Invoice/form extraction, table recognition | $1.50/1000 pages | 15 min |
| **Azure SQL Database** | Persistent Database | Store claims, documents, decisions, audit trail | $5-75/month | 20 min |
| **Azure Blob Storage** | Blob Storage | Store document files (raw & processed) | $0.018/GB | 10 min |
| **Azure Key Vault** | Security | Store API keys, connection strings (recommended) | Free-$0.60/key | 10 min |
| **Azure Communication Services** | Enhanced Email (optional) | Send emails at scale | $0.0025/email | 10 min |

**TOTAL SETUP TIME**: ~90 minutes  
**MINIMUM MONTHLY COST**: $6.50/month (with free tier credits)

---

## 🔧 Detailed Service Requirements

### 1. Azure OpenAI (NLP Integration)

**Purpose**: Natural Language Processing for claims
- Summarize claim descriptions
- Extract named entities (names, dates, amounts, locations)
- Analyze fraud narrative
- Generate claim response emails

**What You Get**:
```
Deployment: gpt-4 or gpt-3.5-turbo
├─ Text generation and understanding
├─ Entity extraction capabilities
├─ Summarization
└─ Custom prompt templates
```

**Setup Steps**:
1. Create "Cognitive Services" resource
2. Deploy model: **gpt-4** (recommended for quality) or **gpt-3.5-turbo** (cheaper)
3. Get: **Endpoint** and **API Key**

**Configuration Needed**:
```json
{
  "Azure": {
    "OpenAI": {
      "Enabled": true,
      "Endpoint": "https://<resource-name>.openai.azure.com/",
      "ApiKey": "<your-api-key>",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview"
    }
  }
}
```

**Pricing**:
- gpt-4: $0.03 per 1K input tokens, $0.06 per 1K output tokens
- gpt-3.5-turbo: $0.0005 per 1K input tokens, $0.0015 per 1K output tokens
- Free tier: $200 credit for new accounts
- **Estimated monthly**: $3-10 (at 100K tokens/month)

**Documentation**: https://learn.microsoft.com/en-us/azure/ai-services/openai/

---

### 2. Azure AI Document Intelligence (Document Intelligence)

**Purpose**: Extract structured data from insurance documents

**What You Get**:
```
Prebuilt Models:
├─ Invoice (extract line items, amounts, dates, vendor info)
├─ Receipt (recognition and parsing)
├─ Form (extract key-value pairs from custom forms)
├─ Document (general document analysis)
└─ ID Document (if handling ID documents)

Features:
├─ Table extraction
├─ Signature detection
├─ Confidence scores per field
├─ Multi-language support
└─ Custom model training
```

**Setup Steps**:
1. Create "Document Intelligence" resource
2. Select resource type: **Standard** (S0 tier recommended)
3. Get: **Endpoint** and **API Key**

**Configuration Needed**:
```json
{
  "Azure": {
    "DocumentIntelligence": {
      "Enabled": true,
      "Endpoint": "https://<region>.api.cognitive.microsoft.com/",
      "ApiKey": "<your-api-key>",
      "ModelId": "prebuilt-invoice"
    }
  }
}
```

**Pricing**:
- Prebuilt models: $1.50 per 1,000 pages analyzed
- Custom model training: Varies based on data
- Free tier: 2 pages per month
- **Estimated monthly**: $1-3 (at 2,000 pages/month)

**Documentation**: https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/

---

### 3. Azure SQL Database (Persistent Database)

**Purpose**: Persistent data storage for claims system

**What You Get**:
```
Features:
├─ Fully managed relational database
├─ Automatic backups & disaster recovery
├─ Built-in security (encryption, firewall)
├─ Performance monitoring
├─ Scalable compute & storage
└─ High availability options
```

**Setup Steps**:
1. Create "SQL Server" (logical server)
2. Create "SQL Database" within server
3. Choose pricing tier:
   - **Basic** ($5/month): Best for POC/dev
   - **Standard** ($15-75/month): Production small
   - **Premium** ($200+/month): Enterprise
4. Configure firewall rules (allow Azure services)
5. Get: **Connection String**

**Configuration Needed**:
```json
{
  "ConnectionStrings": {
    "ClaimsDb": "Server=tcp:claims-server.database.windows.net,1433;Initial Catalog=ClaimsDB;Persist Security Info=False;User ID=sqladmin;Password=<strong-password>;Encrypt=True;Connection Timeout=30;MultipleActiveResultSets=False;Integrated Security=false;"
  }
}
```

**Pricing Tiers**:
| Tier | Cost/Month | Compute | Storage | Best For |
|------|-----------|---------|---------|----------|
| Basic | $5 | 5 DTU | 2 GB | Development |
| Standard | $15-75 | 10-100 DTU | 250 GB | Small production |
| Premium | $200+ | 125+ DTU | 1 TB+ | Enterprise |

**Estimated monthly**: $5-15 (using Basic for now, upgrade as needed)

**Documentation**: https://learn.microsoft.com/en-us/azure/azure-sql/database/

---

### 4. Azure Blob Storage (Blob Storage)

**Purpose**: Store document files (invoices, receipts, photos, etc.)

**What You Get**:
```
Storage Features:
├─ Object storage (files/blobs)
├─ Multiple access tiers (Hot, Cool, Archive)
├─ Versioning & soft delete
├─ Encryption at rest
├─ Lifecycle policies
├─ CDN integration
└─ Redundancy options
```

**Setup Steps**:
1. Create "Storage Account"
2. Choose replication: **Locally Redundant Storage (LRS)** for cost
3. Create containers:
   - `raw-documents` (incoming documents)
   - `processed-documents` (after OCR/analysis)
4. Get: **Connection String**

**Configuration Needed**:
```json
{
  "Azure": {
    "BlobStorage": {
      "Enabled": true,
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<account-name>;AccountKey=<account-key>;EndpointSuffix=core.windows.net",
      "RawDocsContainer": "raw-documents",
      "ProcessedDocsContainer": "processed-documents",
      "ArchiveContainer": "archived-documents"
    }
  }
}
```

**Pricing**:
- Storage: $0.0184 per GB/month (Hot tier)
- Transactions: $0.004 per 10K read operations, $0.005 per 10K write operations
- Free tier: 5 GB for 12 months
- **Estimated monthly**: $0.50-2 (at 10GB stored)

**Documentation**: https://learn.microsoft.com/en-us/azure/storage/blobs/

---

### 5. Azure Key Vault (Security - Recommended)

**Purpose**: Securely store and manage API keys, connection strings, passwords

**What You Get**:
```
Features:
├─ Secure secret storage
├─ Encryption (at rest and in transit)
├─ Access control & audit logs
├─ Secret rotation
├─ Certificate management
└─ Hardware security module (HSM) option
```

**Setup Steps**:
1. Create "Key Vault" resource
2. Create secrets:
   - `OpenAI-Endpoint`
   - `OpenAI-ApiKey`
   - `DocumentIntelligence-ApiKey`
   - `SqlDatabase-ConnectionString`
   - `BlobStorage-ConnectionString`
3. Get: **Key Vault URL**

**Configuration Needed**:
```csharp
// In your Program.cs or appsettings
builder.Configuration.AddAzureKeyVault(
    vaultUri: new Uri("https://<vault-name>.vault.azure.net/"),
    credential: new DefaultAzureCredential());
```

**Pricing**:
- Standard tier: Free per month (+ $0.03 per 10K operations)
- Premium tier: $200-600/month (with HSM)
- **Estimated monthly**: Free (under 10K operations)

**Documentation**: https://learn.microsoft.com/en-us/azure/key-vault/

---

### 6. Azure Communication Services (Optional - Enhanced Email)

**Purpose**: Send emails and SMS at scale (alternative to MailKit/SMTP)

**What You Get**:
```
Features:
├─ Email delivery
├─ SMS messaging
├─ Phone calls
├─ Video calling
├─ Chat messaging
└─ Reliability (99.9% SLA)
```

**Setup Steps**:
1. Create "Communication Services" resource
2. Verify domain (for sending emails)
3. Get: **Connection String**

**Configuration Needed**:
```json
{
  "Azure": {
    "CommunicationServices": {
      "Enabled": true,
      "ConnectionString": "<connection-string>",
      "SenderEmail": "noreply@<your-domain>.com"
    }
  }
}
```

**Pricing**:
- Email: $0.0025 per message (first 40K free/month)
- SMS: $0.0075 per SMS
- **Estimated monthly**: Free (under 40K emails)

**Documentation**: https://learn.microsoft.com/en-us/azure/communication-services/

---

## 🎯 Implementation Order (Services to Set Up)

### **Phase 1: Foundation (Week 1)**

```
1. Azure SQL Database
   ├─ Create server
   ├─ Create database (ClaimsDB)
   ├─ Configure firewall
   └─ Get connection string
   └─ Time: 20 minutes

2. Azure Blob Storage
   ├─ Create storage account
   ├─ Create containers (raw, processed)
   ├─ Get connection string
   └─ Time: 10 minutes

3. Azure Key Vault
   ├─ Create vault
   ├─ Add secrets (connection strings, API keys)
   └─ Time: 10 minutes
```

### **Phase 2: Intelligence (Week 2)**

```
1. Azure OpenAI
   ├─ Create Cognitive Services resource
   ├─ Deploy GPT-4 (or gpt-3.5-turbo)
   ├─ Get endpoint & API key
   └─ Time: 15 minutes

2. Azure Document Intelligence
   ├─ Create Document Intelligence resource
   ├─ Get endpoint & API key
   └─ Time: 15 minutes
```

### **Phase 3: Optional (Week 3)**

```
1. Azure Communication Services (if upgrading email)
   ├─ Create resource
   ├─ Verify domain
   └─ Time: 10 minutes
```

---

## 📊 Service Dependency Diagram

```
┌─────────────────────────────────────────────────────┐
│             Your .NET Application                   │
│           (Claims.Api + Claims.Services)            │
└──────┬──────────────┬──────────────┬────────────────┘
       │              │              │
       ▼              ▼              ▼
   ┌─────────┐  ┌──────────────┐  ┌──────────────┐
   │ Azure   │  │ Azure SQL    │  │ Azure Blob   │
   │ OpenAI  │  │ Database     │  │ Storage      │
   │(NLP)    │  │(Persistence) │  │(Documents)   │
   └────┬────┘  └──────┬───────┘  └──────┬───────┘
        │               │                 │
        │               │                 │
   ┌────────────────────────────────────────────────┐
   │        Azure Key Vault                         │
   │   (Store secrets & credentials securely)       │
   └────────────────────────────────────────────────┘
        │
        ▼
   ┌─────────────────────────────────────────────────┐
   │   Azure AI Document Intelligence                │
   │   (Extract structured data from docs)           │
   └─────────────────────────────────────────────────┘
```

---

## 💰 Total Cost Breakdown

### Monthly Estimates

```
SERVICE                          COST/MONTH    TIER
────────────────────────────────────────────────────
Azure OpenAI (100K tokens)       $3-5          Standard
Azure Document Intelligence      $1-2          Standard
Azure SQL Database               $5-15         Basic
Azure Blob Storage (10GB)        $0.50-1       Standard
Azure Key Vault                  Free          Standard
Azure Communication Srv (opt)    Free          Standard (40K emails free)
────────────────────────────────────────────────────
TOTAL (Development)              $10-25/month

TOTAL (Production)               $50-100/month (with scaling)
```

### Free Credits Available

```
New Azure Account:
├─ $200 free credit (12 months)
├─ Free services for 12 months
└─ This covers ~20 months of POC development!
```

---

## 🔐 Security Best Practices

### 1. Store Secrets in Key Vault (Not in appsettings.json)

```csharp
// Good: Using Key Vault
var keyVaultUrl = "https://<vault-name>.vault.azure.net/";
builder.Configuration.AddAzureKeyVault(
    vaultUri: new Uri(keyVaultUrl),
    credential: new DefaultAzureCredential());

// Bad: Don't do this
"OpenAI:ApiKey": "sk-abc123..." // NEVER hardcode!
```

### 2. Configure Firewall Rules

```
Azure SQL Database:
├─ Allow Azure services: YES
├─ Allow your IP address: YES
├─ Allow connections from: Specific IPs only
└─ Default deny all: YES

Azure Blob Storage:
├─ Public access: Disabled
├─ Shared Access Signatures: Controlled
└─ Network rules: Restrict to Azure services
```

### 3. Enable Encryption

```
Azure SQL Database:
├─ Transparent Data Encryption (TDE): Enabled
├─ Connection encryption: Enforced
└─ Audit logging: Enabled

Azure Blob Storage:
├─ Encryption at rest: Enabled (default)
├─ HTTPS only: Enforced
└─ Storage account firewall: Configured
```

---

## 📋 Setup Checklist

### Before You Start

```
Azure Subscription:
[ ] Create Azure account (https://azure.microsoft.com/free/)
[ ] Get $200 free credit
[ ] Install Azure CLI or use Azure Portal
[ ] Install Visual Studio or VS Code

Services to Create:
[ ] Azure SQL Server + Database
[ ] Azure Blob Storage Account
[ ] Azure Key Vault
[ ] Azure OpenAI (Cognitive Services)
[ ] Azure Document Intelligence

Configuration Files:
[ ] Create appsettings.Development.json
[ ] Create appsettings.Production.json
[ ] Add secrets to Key Vault
[ ] Test connections from Visual Studio

Code Updates:
[ ] Update Program.cs to use Key Vault
[ ] Update Configuration in appsettings
[ ] Install NuGet packages
[ ] Run EF Core migrations
```

---

## 🚀 Quick Start: Azure Portal Setup (20 minutes)

### Step 1: Create Resource Group (2 min)
```
Azure Portal → Resource Groups → Create
Name: claims-validation-rg
Region: East US (or your region)
```

### Step 2: Create SQL Database (5 min)
```
Marketplace → SQL Database → Create
├─ Server name: claims-server-001
├─ Database name: ClaimsDB
├─ Admin user: sqladmin
├─ Password: [strong password]
├─ Tier: Basic (5 DTU)
└─ Storage: 2 GB
```

### Step 3: Create Blob Storage (3 min)
```
Marketplace → Storage Account → Create
├─ Name: claimsstoragexxxxx (unique)
├─ Tier: Standard
├─ Replication: LRS
└─ Access: Private
```

### Step 4: Create Azure OpenAI (5 min)
```
Marketplace → Cognitive Services → Create
├─ Name: claims-openai
├─ Type: OpenAI
├─ Pricing tier: Standard (S0)
└─ Region: East US (or available region)
```

### Step 5: Deploy GPT-4 Model (5 min)
```
Azure OpenAI Studio (https://oai.azure.com)
├─ Select your resource
├─ Go to Deployments
├─ Create new deployment
├─ Model: gpt-4
├─ Name: gpt-4
└─ Capacity: 10K TPM (tokens per minute)
```

---

## 🔗 NuGet Packages Needed

Add these to `Claims.Api.csproj` and `Claims.Services.csproj`:

```xml
<!-- Azure OpenAI -->
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.12" />

<!-- Azure Document Intelligence -->
<PackageReference Include="Azure.AI.FormRecognizer" Version="4.1.0" />

<!-- Azure SQL Database (EF Core) -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />

<!-- Azure Blob Storage -->
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.0" />

<!-- Azure Key Vault -->
<PackageReference Include="Azure.Identity" Version="1.10.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.2.0" />

<!-- Azure Communication Services (optional) -->
<PackageReference Include="Azure.Communication.Email" Version="1.0.1" />
```

---

## ✅ Verification Checklist

After setup, verify everything works:

```
Azure OpenAI:
[ ] Connection string in Key Vault
[ ] Model deployment shows "Succeeded"
[ ] Test call returns response

Azure Document Intelligence:
[ ] Endpoint accessible
[ ] API key valid
[ ] Test document analysis

Azure SQL Database:
[ ] Connection string accessible
[ ] SQL Server firewall allows connections
[ ] EF Core migrations run successfully
[ ] Data persists after restart

Azure Blob Storage:
[ ] Containers created (raw, processed)
[ ] Connection string accessible
[ ] Test upload/download works
[ ] Documents persist

Azure Key Vault:
[ ] All secrets stored
[ ] Access policies configured
[ ] Application can read secrets
```

---

## 📞 Support & Documentation Links

| Service | Quick Start | Full Docs |
|---------|------------|-----------|
| Azure OpenAI | https://aka.ms/azure-openai-quickstart | https://learn.microsoft.com/en-us/azure/ai-services/openai/ |
| Document Intelligence | https://aka.ms/doc-intelligence-quickstart | https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/ |
| Azure SQL | https://aka.ms/sql-quickstart | https://learn.microsoft.com/en-us/azure/azure-sql/ |
| Blob Storage | https://aka.ms/blob-quickstart | https://learn.microsoft.com/en-us/azure/storage/blobs/ |
| Key Vault | https://aka.ms/keyvault-quickstart | https://learn.microsoft.com/en-us/azure/key-vault/ |
| Communication Srv | https://aka.ms/acs-quickstart | https://learn.microsoft.com/en-us/azure/communication-services/ |

---

## 🎯 Next Steps

1. **Create Azure Account** → Get $200 free credit
2. **Setup Services** → Follow checklist above (90 minutes)
3. **Get Credentials** → Save to Key Vault
4. **Update Code** → Integrate with your .NET application
5. **Test Connections** → Verify each service works
6. **Start Implementation** → Begin with database migrations

---

**You're ready to build!** All Azure services are documented and ready for integration. 🚀
