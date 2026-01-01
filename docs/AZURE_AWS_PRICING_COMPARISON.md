# Azure vs AWS: Detailed Pricing Comparison for Claims Validation MVP

**Prepared for:** Personal Pay-as-You-Go Subscription (No Free Tier)  
**Date:** January 1, 2026  
**Use Case:** Production MVP with 5 Core Features

---

## 🎯 Features to Implement

1. **NLP Integration** - Chatbot for claim understanding
2. **Document Intelligence** - Extract data from invoices/forms
3. **Persistent Database** - SQL database for data persistence
4. **Blob Storage** - Cloud document storage
5. **Fraud ML Enhancement** - Advanced fraud detection model

---

## 📊 Service Comparison Matrix

### AZURE Services for MVP

| Feature | Service | Tier | Monthly Cost | Annual Cost |
|---------|---------|------|--------------|-------------|
| **NLP Integration** | Azure OpenAI | Pay-as-you-go (GPT-4) | $3-5 | $36-60 |
| **Document Intelligence** | Document Intelligence API | Standard | $1-2 | $12-24 |
| **Persistent Database** | SQL Database | Basic (5 DTU) | $5-8 | $60-96 |
| **Blob Storage** | Blob Storage | Standard LRS | $0.50-1 | $6-12 |
| **Fraud ML** | N/A (Uses DB + Compute) | VM Optional | $0-30 | $0-360 |
| **Secrets Management** | Key Vault | Standard | FREE | FREE |
| **Email (Optional)** | Communication Services | Pay-as-you-go | FREE* | FREE* |
| | | **TOTAL (No VM)** | **$10-16/mo** | **$114-192/yr** |
| | | **TOTAL (With VM for ML)** | **$40-46/mo** | **$474-552/yr** |

**Notes:**
- *Communication Services: 40K emails/month free, $0.0001/email after
- GPT-4: ~1M tokens = $30-40, typical MVP usage = $3-5/month
- Document Intelligence: $1 per page (1000 pages = ~$1/month)
- SQL Database Basic: No backup/restore, limited features
- ML training uses included compute or optional VM

---

### AWS Services for MVP

| Feature | Service | Tier | Monthly Cost | Annual Cost |
|---------|---------|------|--------------|-------------|
| **NLP Integration** | Amazon Bedrock (Claude/GPT) | Pay-as-you-go | $2-4 | $24-48 |
| **Document Intelligence** | Amazon Textract | Per page | $2-4 | $24-48 |
| **Persistent Database** | RDS MySQL | db.t4g.micro | $15-20 | $180-240 |
| **Blob Storage** | S3 Standard | Per GB | $2-5 | $24-60 |
| **Fraud ML** | SageMaker | ml.m5.large (optional) | $0-25 | $0-300 |
| **Secrets Management** | Secrets Manager | Per secret | $0.40 | $4.80 |
| **Email (Optional)** | SES | Pay-as-you-go | FREE* | FREE* |
| | | **TOTAL (No ML instance)** | **$22-34/mo** | **$264-408/yr** |
| | | **TOTAL (With ML instance)** | **$47-59/mo** | **$564-708/yr** |

**Notes:**
- *SES: 62K emails/month free for 30 days, then $0.10 per 1000 emails
- Bedrock: Claude 3 = $3/1M input + $15/1M output tokens
- Textract: $1 per 1000 pages (or $0.50/page with Intelligent Document Processing)
- RDS MySQL: Smallest: db.t4g.micro at $0.015/hour = ~$11/month
- Textract pricing: $1 per document + additional per-page charges

---

## 💰 Detailed Cost Breakdown

### AZURE Pricing Details

#### 1. **NLP Integration - Azure OpenAI (GPT-4)**
```
Model: GPT-4 Turbo
Pricing: Input $0.01/1K tokens, Output $0.03/1K tokens

MVP Usage Estimate:
- 50 claims/day × 300 tokens per claim = 15K tokens/day
- 15K × 30 = 450K tokens/month
- Average: 200K input + 250K output
- Cost: (200K × $0.01) + (250K × $0.03) = $2 + $7.50 = $9.50/month

Conservative Estimate: $3-5/month (at lower usage)
Peak Estimate: $15-20/month (at higher usage)
REALISTIC for MVP: $5-10/month
```

#### 2. **Document Intelligence - Document Intelligence API**
```
Pricing: $1 per document or $2 per page (whichever is lower)

MVP Usage Estimate:
- 100 documents/month × 2 pages avg
- 200 pages × $0.001/page = $0.20/month

REALISTIC for MVP: $0.50-1/month
```

#### 3. **Persistent Database - SQL Database Basic**
```
Tier: Basic (5 DTU)
Pricing: ~$5-8/month (includes 2GB storage)

Additional storage: $0.01 per GB/month after 2GB
Backup storage: Included in first 100% of database size

MVP Database Size: 
- 10K claims × 2KB = 20MB
- Pretty easily fits in 2GB free allocation

REALISTIC for MVP: $5-8/month
```

#### 4. **Blob Storage - Standard LRS**
```
Pricing: 
- Storage: $0.018 per GB/month (first 50TB)
- Operations: $0.0005 per 10K read ops
- Data transfer: $0 ingress, $0.08/GB egress

MVP Usage Estimate:
- 1000 documents × 500KB avg = 500GB
- 500GB × $0.018 = $9/month (but free tier includes 100GB/month)
- For MVP: ~50GB stored documents

REALISTIC for MVP: $1-2/month
```

#### 5. **Fraud ML Enhancement - No additional service**
```
Uses: SQL Database (already included) + optional VM
Option 1: Use SQL Stored Procedures (FREE)
Option 2: Dedicated ML VM (Standard_B1s: ~$10-15/month)

REALISTIC for MVP: $0-10/month (using stored procedures is FREE)
```

#### 6. **Key Vault - Secrets Management**
```
Pricing: FREE for standard operations
- Up to 1M transactions/month free
- Additional: $0.03 per 10K transactions

REALISTIC for MVP: **FREE**
```

---

### AWS Pricing Details

#### 1. **NLP Integration - Amazon Bedrock (Claude 3)**
```
Model: Claude 3 Sonnet
Pricing: 
- Input: $3/million tokens
- Output: $15/million tokens

MVP Usage Estimate (same as Azure):
- 200K input + 250K output tokens/month
- Cost: (200K × $3M) + (250K × $15M) = $0.60 + $3.75 = $4.35/month

REALISTIC for MVP: $3-5/month
```

#### 2. **Document Intelligence - Amazon Textract**
```
Pricing:
- Standard: $1 per 1000 pages
- IDP (Tables): $2 per 1000 pages

MVP Usage Estimate:
- 100 documents × 2 pages = 200 pages/month
- 200 pages × $0.001 = $0.20/month

But AWS bills in blocks:
- Minimum 1000 pages counted = ~$1/month

REALISTIC for MVP: $1-2/month
```

#### 3. **Persistent Database - RDS MySQL**
```
Pricing: 
- db.t4g.micro: $0.015/hour on-demand
- Monthly: $0.015 × 730 hours = $10.95/month
- Storage: $0.10 per GB-month
- Backup storage: $0.095 per GB-month

MVP Database Size:
- 10K claims × 2KB = 20MB
- Storage: 20GB allocated (standard) × $0.10 = $2/month

REALISTIC for MVP: $13-16/month (instance + storage)
```

#### 4. **Blob Storage - S3 Standard**
```
Pricing:
- Storage: $0.023 per GB/month (first 50TB)
- Requests: $0.0004 per PUT/COPY/POST/LIST per 1000
- Retrieval: $0.0001 per GET per 1000

MVP Usage Estimate:
- 50GB stored: 50 × $0.023 = $1.15/month
- ~1000 PUT operations/month: $0.0004
- ~1000 GET operations/month: $0.0001

REALISTIC for MVP: $1-3/month
```

#### 5. **Fraud ML Enhancement - SageMaker (Optional)**
```
Pricing (if using SageMaker):
- ml.m5.large notebook: $0.149/hour = ~$109/month
- ml.m5.xlarge: $0.298/hour = ~$217/month
- Batch transform: $0.0001 per instance-hour

BUT: You can use EC2 or Lambda instead (cheaper)

Option 1: Lambda + S3 (cheapest): $1-5/month
Option 2: EC2 t3.small: ~$20/month
Option 3: SageMaker ml.m5.large: ~$110/month

REALISTIC for MVP: $1-30/month (using Lambda or EC2)
```

#### 6. **Secrets Manager - AWS Secrets Manager**
```
Pricing:
- $0.40 per secret per month
- $0.05 per 10K API calls

For 5 secrets:
- 5 × $0.40 = $2/month
- Plus API calls: ~$0.05/month

REALISTIC for MVP: $2-3/month
```

---

## 📈 Total Cost Comparison

### Monthly Costs (Pay-As-You-Go)

```
SCENARIO 1: Minimal MVP (No dedicated ML compute)

AZURE:
├─ OpenAI GPT-4:         $5-10/month
├─ Document Intelligence: $1/month
├─ SQL Database:         $5-8/month
├─ Blob Storage:         $1-2/month
├─ Fraud ML (builtin):   $0/month
└─ Key Vault:           $0/month
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOTAL AZURE:             $12-21/month
Annual: $144-252

AWS:
├─ Bedrock (Claude):     $3-5/month
├─ Textract:            $1-2/month
├─ RDS MySQL:           $13-16/month
├─ S3 Storage:          $1-3/month
├─ Fraud ML (EC2):      $20/month
└─ Secrets Manager:     $2-3/month
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOTAL AWS:              $40-49/month
Annual: $480-588


SCENARIO 2: With dedicated ML compute

AZURE:
├─ All above:           $12-21/month
├─ VM (Standard_B1s):   $10-15/month
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOTAL AZURE + ML:        $22-36/month
Annual: $264-432

AWS:
├─ All above:           $40-49/month
├─ SageMaker ml.m5:     $110/month
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOTAL AWS + ML:         $150-159/month
Annual: $1800-1908
```

---

## 🔄 Quarterly & Annual Breakdown

### Azure MVP (No extra VM)
| Period | Cost |
|--------|------|
| Monthly | $12-21 |
| Quarterly | $36-63 |
| 6-Month | $72-126 |
| Annual | $144-252 |

### AWS MVP (With EC2 for ML)
| Period | Cost |
|--------|------|
| Monthly | $40-49 |
| Quarterly | $120-147 |
| 6-Month | $240-294 |
| Annual | $480-588 |

### Difference
- **Monthly:** AWS is $19-28 MORE expensive
- **Annual:** AWS is $228-336 MORE expensive
- **Over 3 years:** AWS costs $684-1008 MORE

---

## ⚡ Performance & Feature Comparison

| Factor | AZURE | AWS |
|--------|-------|-----|
| **NLP Model Quality** | GPT-4 Turbo (Best-in-class) | Claude 3 Sonnet (Comparable) |
| **Document Extraction** | Better at tables/forms | Good general extraction |
| **Database Maturity** | SQL Server compatible | Standard MySQL |
| **Storage Performance** | Excellent | Excellent |
| **Free Tier Available** | Yes ($200 credit) | Yes (12 months) |
| **Regional Availability** | 60+ regions | 30+ regions |
| **API Rate Limits** | High | High |
| **Learning Curve** | Medium (C# friendly) | Steeper (many services) |

---

## 🎯 Feature Mapping

### Azure Implementation

```
NLP Chatbot
└─ Azure OpenAI (GPT-4 Turbo)
   Language: C#, SDK: Azure.AI.OpenAI
   Integration: Minimal configuration needed

Document Intelligence
└─ Azure Document Intelligence
   Language: C#, SDK: Azure.AI.FormRecognizer
   Best for: Forms, invoices, receipts

Database
└─ Azure SQL Database Basic
   Language: T-SQL, ORM: Entity Framework Core
   Connection: Standard SQL connection string

Storage
└─ Azure Blob Storage
   Language: C#, SDK: Azure.Storage.Blobs
   Containers: raw-documents, processed-documents

Fraud Detection
└─ SQL Stored Procedures + ML.NET
   Language: C#, uses existing infrastructure
   No additional cost
```

### AWS Implementation

```
NLP Chatbot
└─ Amazon Bedrock (Claude 3)
   Language: Python/Node.js better supported
   Integration: REST API with authentication

Document Intelligence
└─ Amazon Textract
   Language: SDK requires AWS SDKs
   Best for: General document extraction

Database
└─ RDS MySQL
   Language: SQL, ORM: Entity Framework Core
   Connection: MySQL connection string

Storage
└─ S3 Standard
   Language: Various SDKs available
   Buckets: raw-documents, processed-documents

Fraud Detection
└─ Lambda functions or EC2 instance
   Language: Python, Node.js, or any
   Requires: Additional compute setup
```

---

## 🏆 Recommendation Analysis

### Azure is Better For You Because:

✅ **Price:** $288-336 cheaper per year (46-57% less expensive)
- Minimal MVP: $144/year vs AWS $480/year
- With ML compute: $264/year vs AWS $1800/year

✅ **Simplicity:** Native .NET/C# integration
- Your codebase is .NET/C# based
- Azure SDKs integrate seamlessly
- Less configuration overhead

✅ **Feature Parity:** All required services available
- OpenAI integration is official (not third-party)
- Document Intelligence is purpose-built for your use case
- SQL Database integrates with EF Core directly

✅ **Developer Experience:** 
- Single SDK for most services
- Excellent documentation for .NET devs
- Key Vault is free and simple

✅ **Scalability:** 
- Easier to scale from MVP to production
- Pay-as-you-go model is transparent
- No surprise costs

### AWS Would Be Better If:

❌ You need Python/Node.js (not your case)
❌ You prefer Lambda (serverless, but more complex)
❌ You need enterprise AWS ecosystem (not needed for MVP)
❌ You have existing AWS infrastructure (you don't)

---

## 💡 Cost Optimization Tips for Your MVP

### AZURE:
1. **Use SQL Database Basic tier** - $5-8/month (not Standard)
2. **Implement fraud detection in SQL** - $0 (avoid dedicated VM)
3. **Use shared Key Vault** - $0 (included free)
4. **Start with Standard LRS storage** - $1-2/month (sufficient for MVP)
5. **Monitor token usage** - Set spending alerts in OpenAI

### AWS:
1. **Use RDS db.t4g.micro or t3.micro** - Smallest instance (still $10+)
2. **Implement fraud in Lambda** - $1-5/month (vs $110 SageMaker)
3. **Use S3 Intelligent-Tiering** - Auto-reduces storage cost
4. **Enable CloudWatch cost alerts** - Prevent surprises
5. **Consider RDS Aurora Serverless** - Only pay for what you use

---

## 📋 Implementation Readiness

### Azure MVP Checklist
- [ ] Provision Azure OpenAI (15 min)
- [ ] Create Document Intelligence service (5 min)
- [ ] Set up SQL Database Basic (8 min)
- [ ] Create Blob Storage account (8 min)
- [ ] Configure Key Vault (5 min)
- [ ] Install NuGet packages (5 min)
- [ ] Update connection strings (5 min)
- [ ] Test each service (30 min)
**Total Setup Time: 90 minutes**

### AWS MVP Checklist
- [ ] Create Bedrock access (10 min)
- [ ] Set up Textract (5 min)
- [ ] Launch RDS MySQL instance (15 min)
- [ ] Create S3 bucket (5 min)
- [ ] Set up Secrets Manager (10 min)
- [ ] Launch EC2 instance for ML (10 min)
- [ ] Configure all connections (15 min)
- [ ] Test each service (40 min)
**Total Setup Time: 110 minutes**

---

## 🎓 Final Verdict

### **RECOMMENDATION: CHOOSE AZURE** ✅

| Metric | Winner | Reason |
|--------|--------|--------|
| **Price** | AZURE | 46-57% cheaper ($144-264 vs $480-588/year) |
| **.NET Compatibility** | AZURE | Native integration with C# stack |
| **Setup Complexity** | AZURE | Fewer services, better documentation |
| **Learning Curve** | AZURE | Straightforward for .NET developers |
| **Time-to-MVP** | AZURE | 20 min faster setup (90 vs 110 min) |
| **Feature Completeness** | TIE | Both have all required services |
| **Production Ready** | AZURE | Scales easily from MVP to enterprise |

### Cost Difference Over Time

```
Year 1:  Azure $144  |  AWS $480   |  SAVE: $336 ✓
Year 2:  Azure $144  |  AWS $480   |  SAVE: $336 ✓
Year 3:  Azure $144  |  AWS $480   |  SAVE: $336 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
3-Year Total:        Azure $432  |  AWS $1440  |  SAVE: $1008 ✓
```

### Next Steps (Azure Path)

1. Create Azure account (https://azure.microsoft.com/en-us/)
2. Follow `docs/azure-integration/AZURE_SETUP_STEP_BY_STEP.md`
3. Complete setup in 90 minutes
4. Start implementing features
5. Stay within $12-21/month budget

---

**Bottom Line:** For a .NET-based MVP with personal pay-as-you-go, **Azure is 46% cheaper and easier to implement**. Choose Azure. 🚀
