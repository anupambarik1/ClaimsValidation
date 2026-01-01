# Azure Services - Quick Reference Card

## 🎯 5 Features + 6 Azure Services

```
YOUR 5 FEATURES          AZURE SERVICES NEEDED
════════════════════════════════════════════════════════════

1. NLP Integration    ──→  Azure OpenAI (GPT-4)
                           └─ Summarize, extract entities, analyze fraud

2. Document Intel    ──→  Azure AI Document Intelligence
                           └─ Extract tables, forms, key-value pairs

3. Persistent DB     ──→  Azure SQL Database
                           └─ Store claims, documents, decisions

4. Blob Storage      ──→  Azure Blob Storage
                           └─ Store document files (raw & processed)

5. Fraud ML Enhance  ──→  (Uses: SQL Database + no new service)
                           └─ Stores training data & model info

SECURITY (Recommended):   Azure Key Vault
                          └─ Store all API keys & connection strings

OPTIONAL (Enhanced):      Azure Communication Services
                          └─ Send emails at enterprise scale
```

---

## 📊 Service Matrix

| Service | 1-NLP | 2-DocIntel | 3-Database | 4-Storage | 5-FraudML | Cost |
|---------|-------|-----------|-----------|-----------|-----------|------|
| Azure OpenAI | ✅ | | | | | $3-5 |
| Doc Intelligence | | ✅ | | | | $1-2 |
| SQL Database | | | ✅ | | ✅ | $5-15 |
| Blob Storage | | | | ✅ | | $0.50-1 |
| Key Vault | ✅ | ✅ | ✅ | ✅ | | FREE |
| Comm Services | | | | | | FREE* |
| **TOTAL** | | | | | | **$10-25/mo** |

*With free tier credits ($200 new account)

---

## ⚡ Quick Setup Order

```
SETUP: 90 MINUTES TOTAL

Week 1 - Foundation (20 min each):
[ ] Azure SQL Database          (20 min)
[ ] Azure Blob Storage          (10 min)
[ ] Azure Key Vault             (10 min)

Week 2 - Intelligence (15 min each):
[ ] Azure OpenAI + GPT-4        (15 min)
[ ] Azure Document Intelligence (15 min)

Optional:
[ ] Azure Communication Srv     (10 min)
```

---

## 🔑 What You'll Get (Credentials)

```
For Each Service, You Get:
─────────────────────────────

Azure OpenAI:
├─ Endpoint: https://<resource>.openai.azure.com/
├─ API Key: sk-...
└─ Deployment Name: gpt-4

Azure Document Intelligence:
├─ Endpoint: https://<region>.api.cognitive.microsoft.com/
└─ API Key: xxxxxxxx

Azure SQL Database:
├─ Server: claims-server.database.windows.net
├─ Database: ClaimsDB
├─ User: sqladmin
└─ Password: [your-password]

Azure Blob Storage:
├─ Account: claims-storage-xxx
├─ Key: xxxxxxxx
└─ Connection String: DefaultEndpointsProtocol=https;...

Azure Key Vault:
├─ Vault URL: https://<vault-name>.vault.azure.net/
└─ Access policies configured
```

---

## 💰 Costs Breakdown

```
SERVICE                              MONTHLY COST    FREE TIER
──────────────────────────────────────────────────────────────
Azure OpenAI (100K tokens)           $3-5            $200 credit
Azure Document Intelligence          $1-2            Free (2 pages/mo)
Azure SQL Database (Basic)           $5              None
Azure Blob Storage (10GB)            $0.50-1         5GB free
Azure Key Vault                      FREE            Standard tier
Azure Communication Services         FREE            40K emails/mo
──────────────────────────────────────────────────────────────
TOTAL DEVELOPMENT                    $10-25/month    ($200 credit covers all!)

TOTAL PRODUCTION (scaled)             $50-100/month   After free credits
```

---

## 🎓 NuGet Packages to Install

```csharp
// Run in Package Manager Console or dotnet CLI:

// Core AI/ML Services
Install-Package Azure.AI.OpenAI -Version 1.0.0-beta.12
Install-Package Azure.AI.FormRecognizer -Version 4.1.0

// Database
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 8.0.0

// Storage
Install-Package Azure.Storage.Blobs -Version 12.19.0

// Security (Key Vault)
Install-Package Azure.Identity -Version 1.10.0
Install-Package Azure.Security.KeyVault.Secrets -Version 4.7.0
Install-Package Azure.Extensions.AspNetCore.Configuration.Secrets -Version 1.2.0

// Email (Optional)
Install-Package Azure.Communication.Email -Version 1.0.1
```

---

## 🚀 Setup in Azure Portal (Visual Guide)

```
STEP 1: Resource Group (2 min)
┌─────────────────────────────────┐
│ Azure Portal                    │
│ ├─ Resource Groups              │
│ ├─ Create New                   │
│ ├─ Name: claims-validation-rg   │
│ └─ Region: East US              │
└─────────────────────────────────┘
                ▼

STEP 2: SQL Database (5 min)
┌─────────────────────────────────┐
│ Marketplace → SQL Database      │
│ ├─ Server: claims-server-001    │
│ ├─ Database: ClaimsDB           │
│ ├─ Tier: Basic (5 DTU)          │
│ └─ Admin: sqladmin              │
└─────────────────────────────────┘
                ▼

STEP 3: Blob Storage (5 min)
┌─────────────────────────────────┐
│ Marketplace → Storage Account   │
│ ├─ Name: claimsstorageXXXXX     │
│ ├─ Tier: Standard               │
│ ├─ Replication: LRS             │
│ └─ Create containers:           │
│    ├─ raw-documents             │
│    └─ processed-documents       │
└─────────────────────────────────┘
                ▼

STEP 4: Key Vault (5 min)
┌─────────────────────────────────┐
│ Marketplace → Key Vault         │
│ ├─ Name: claims-keyvault        │
│ ├─ Tier: Standard               │
│ └─ Add Secrets:                 │
│    ├─ OpenAI-Endpoint           │
│    ├─ OpenAI-ApiKey             │
│    ├─ DocIntel-ApiKey           │
│    └─ SqlConnection-String      │
└─────────────────────────────────┘
                ▼

STEP 5: Azure OpenAI (10 min)
┌─────────────────────────────────┐
│ Marketplace → Cognitive Srvc    │
│ ├─ Create OpenAI resource       │
│ ├─ Go to Azure OpenAI Studio    │
│ ├─ Create Deployment            │
│ │  ├─ Name: gpt-4               │
│ │  ├─ Model: gpt-4              │
│ │  └─ Capacity: 10K TPM         │
│ └─ Wait for "Succeeded" status  │
└─────────────────────────────────┘
                ▼

STEP 6: Document Intelligence (5 min)
┌─────────────────────────────────┐
│ Marketplace → Document Intel    │
│ ├─ Create new resource          │
│ ├─ Tier: Standard (S0)          │
│ └─ Note endpoint & API key      │
└─────────────────────────────────┘
```

---

## 📋 Configuration Files

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "ClaimsDb": "Server=tcp:claims-server.database.windows.net,1433;Initial Catalog=ClaimsDB;Persist Security Info=False;User ID=sqladmin;Password=YOUR_PASSWORD;Encrypt=True;"
  },
  "Azure": {
    "OpenAI": {
      "Enabled": true,
      "Endpoint": "https://claims-openai.openai.azure.com/",
      "ApiKey": "YOUR_OPENAI_KEY",
      "DeploymentName": "gpt-4"
    },
    "DocumentIntelligence": {
      "Enabled": true,
      "Endpoint": "https://eastus.api.cognitive.microsoft.com/",
      "ApiKey": "YOUR_DOCTEL_KEY",
      "ModelId": "prebuilt-invoice"
    },
    "BlobStorage": {
      "Enabled": true,
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=claimsstorage;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
      "RawDocsContainer": "raw-documents",
      "ProcessedDocsContainer": "processed-documents"
    }
  }
}
```

---

## ✅ Verification Checklist

After setting up all services:

```
Azure SQL Database:
[ ] Server created and online
[ ] Database created (ClaimsDB)
[ ] Firewall rule added (allow Azure services)
[ ] Can connect from VS Code/SSMS
[ ] Connection string works

Azure Blob Storage:
[ ] Storage account created
[ ] Containers created (raw, processed)
[ ] Connection string works
[ ] Can upload/download test file

Azure OpenAI:
[ ] Resource created
[ ] GPT-4 model deployed
[ ] Status shows "Succeeded"
[ ] Can call API from Postman

Azure Document Intelligence:
[ ] Resource created
[ ] API key works
[ ] Can analyze test document

Azure Key Vault:
[ ] All secrets added
[ ] Access policies configured
[ ] Application can read secrets
[ ] No secrets in appsettings (moved to vault)
```

---

## 🔐 Security Checklist

```
Before Production:
[ ] All secrets in Key Vault (not appsettings)
[ ] SQL Database firewall configured
[ ] Blob Storage requires authentication
[ ] Connection strings use HTTPS only
[ ] No API keys in source control
[ ] Audit logging enabled
[ ] Backup/disaster recovery configured
```

---

## 📞 Support

**Don't know something?** Check these docs:
- Azure OpenAI: https://aka.ms/azure-openai
- Document Intelligence: https://aka.ms/doc-intelligence
- SQL Database: https://aka.ms/sql-database
- Blob Storage: https://aka.ms/blob-storage
- Key Vault: https://aka.ms/key-vault

---

## 🎯 You're Ready to Build!

**Services Setup Time**: 90 minutes total  
**Total Monthly Cost**: $10-25 (covered by free credits)  
**Free Credits**: $200 (lasts ~20 months for POC)

Next: Start implementing! 🚀
