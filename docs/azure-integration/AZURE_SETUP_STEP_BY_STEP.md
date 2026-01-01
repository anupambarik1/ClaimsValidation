# Step-by-Step Azure Setup Guide

**Goal**: Set up all 6 Azure services in 90 minutes  
**Prerequisite**: Azure subscription (free $200 credit)

---

## 📋 Pre-Setup Checklist

Before you start, have these ready:

```
BEFORE SETUP:
[ ] Azure account created (https://azure.microsoft.com/free/)
[ ] $200 free credit confirmed
[ ] Browser with Azure Portal (portal.azure.com)
[ ] Password manager to save credentials
[ ] Text editor to copy configuration values
[ ] VS Code or Visual Studio ready (for later)
[ ] 90 minutes of focused time
```

---

## 🎯 Phase 1: Foundation Services (30 minutes)

### Service 1: Create Resource Group (2 minutes)

**Why**: Organizes all your resources in one place

**Steps**:
1. Go to https://portal.azure.com/
2. Click **"Resource groups"** (search top bar)
3. Click **"+ Create"**
4. Fill in:
   - **Name**: `claims-validation-rg`
   - **Region**: Select closest to you (e.g., East US)
5. Click **"Review + create"** → **"Create"**
6. Wait for completion (1-2 minutes)

**Result**: ✅ Resource group ready

---

### Service 2: Azure SQL Database (8 minutes)

**Why**: Stores all claim data persistently

**Steps**:

#### Part A: Create SQL Server
1. In Azure Portal, search **"SQL Database"**
2. Click **"+ Create"**
3. Fill in:
   - **Resource Group**: `claims-validation-rg`
   - **Database name**: `ClaimsDB`
   - **Server**: Click **"Create new server"**
4. In server popup, fill:
   - **Server name**: `claims-server-001` (must be globally unique)
   - **Location**: Same region as resource group
   - **Admin username**: `sqladmin`
   - **Admin password**: Create strong password (save it!)
   - Example: `P@ssw0rd!Claims2024#SecureDB`
5. Click **"OK"** on server popup

#### Part B: Configure Database
6. Back on database creation, fill:
   - **Workload environment**: Development
   - **Compute + storage**: Click to change
     - Select **"Basic (5 DTU, 2 GB)"** (cheapest for POC)
   - Click **"Apply"**
7. Click **"Next: Networking"**
8. Set **"Allow Azure services..."** to **"Yes"** (important!)
9. Click **"Review + create"** → **"Create"**
10. Wait for completion (5-6 minutes)

**After creation**:
1. Go to your database
2. Click **"Connection strings"** (left menu)
3. Copy **"ADO.NET (SQL authentication)"** - save this!

```
Your connection string looks like:
Server=tcp:claims-server-001.database.windows.net,1433;
Initial Catalog=ClaimsDB;
Persist Security Info=False;
User ID=sqladmin;
Password=YOUR_PASSWORD;
Encrypt=True;
Connection Timeout=30;
```

**Result**: ✅ SQL Database ready with connection string

**Time spent**: 8 minutes

---

### Service 3: Azure Blob Storage (8 minutes)

**Why**: Stores document files (invoices, photos, etc.)

**Steps**:

#### Part A: Create Storage Account
1. Search **"Storage account"** in Azure Portal
2. Click **"+ Create"**
3. Fill in:
   - **Resource Group**: `claims-validation-rg`
   - **Storage account name**: `claimsstorageXXXXX` (unique, lowercase, 3-24 chars)
     - Example: `claimsstorage20260101xyz`
   - **Region**: Same as resource group
   - **Replication**: `Locally-redundant storage (LRS)` (cheapest)
4. Click **"Review + create"** → **"Create"**
5. Wait for completion (2-3 minutes)

#### Part B: Create Containers
6. Go to your storage account
7. Click **"Containers"** (left menu)
8. Click **"+ Container"**
9. Create first container:
   - **Name**: `raw-documents`
   - **Public access level**: `Private`
   - Click **"Create"**
10. Repeat step 8-9 for second container:
    - **Name**: `processed-documents`
    - **Public access level**: `Private`

#### Part C: Get Connection String
11. Click **"Access keys"** (left menu)
12. Copy **"Connection string"** (under key1) - save this!

```
Your connection string looks like:
DefaultEndpointsProtocol=https;
AccountName=claimsstorage20260101xyz;
AccountKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx;
EndpointSuffix=core.windows.net
```

**Result**: ✅ Blob Storage with 2 containers and connection string

**Time spent**: 8 minutes

---

### Service 4: Azure Key Vault (10 minutes)

**Why**: Securely store all secrets and API keys

**Steps**:

#### Part A: Create Key Vault
1. Search **"Key Vault"** in Azure Portal
2. Click **"+ Create"**
3. Fill in:
   - **Resource Group**: `claims-validation-rg`
   - **Name**: `claims-keyvault-001` (must be globally unique)
   - **Region**: Same as resource group
   - **Pricing tier**: `Standard`
4. Click **"Review + create"** → **"Create"**
5. Wait for completion (3-4 minutes)

#### Part B: Add Secrets
6. Go to your Key Vault
7. Click **"Secrets"** (left menu)
8. Click **"+ Generate/Import"**
9. Add first secret (**SQL Connection String**):
   - **Upload options**: `Manual`
   - **Name**: `SqlConnection-String`
   - **Value**: Paste your SQL connection string (from step 2)
   - Click **"Create"**
10. Add second secret (**Storage Connection String**):
    - Click **"+ Generate/Import"** again
    - **Name**: `BlobStorage-ConnectionString`
    - **Value**: Paste your Blob connection string (from step 3)
    - Click **"Create"**
11. (You'll add API keys later after creating those services)

**Result**: ✅ Key Vault with secrets stored

**Time spent**: 10 minutes

**Total Phase 1**: 30 minutes

---

## 🧠 Phase 2: Intelligence Services (30 minutes)

### Service 5: Azure OpenAI with GPT-4 (15 minutes)

**Why**: Natural language processing for claim understanding

**Steps**:

#### Part A: Create OpenAI Resource
1. Search **"Cognitive Services"** in Azure Portal
2. Click **"+ Create"**
3. Fill in:
   - **Resource Group**: `claims-validation-rg`
   - **Region**: **IMPORTANT**: Not all regions have OpenAI
     - Available regions: East US, France Central, UK South, etc.
     - Use **East US** if available
   - **Name**: `claims-openai-001`
   - **Pricing tier**: `Standard S0`
4. Click **"Review + create"** → **"Create"**
5. Wait for completion (3-4 minutes)

#### Part B: Deploy GPT-4 Model
6. After creation, go to your OpenAI resource
7. Click **"Go to Azure OpenAI Studio"** (or go to https://oai.azure.com/)
8. Sign in if needed
9. Click **"Deployments"** (left menu)
10. Click **"Create new deployment"**
11. Fill in:
    - **Deployment name**: `gpt-4`
    - **Model name**: `gpt-4` (select from dropdown)
    - **Model version**: Keep default (latest)
    - **Tokens per Minute Throttle (TPM)**: `10000` (or higher if needed)
12. Click **"Create"**
13. Wait for status to show **"Succeeded"** (this takes 2-3 minutes)

#### Part C: Get Credentials
14. Back in Azure Portal, go to your OpenAI resource
15. Click **"Keys and Endpoint"** (left menu)
16. Copy:
    - **Key 1**: Save as `OpenAI-ApiKey`
    - **Endpoint**: Save as `OpenAI-Endpoint`

**Your values look like**:
```
OpenAI-ApiKey: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
OpenAI-Endpoint: https://claims-openai-001.openai.azure.com/
Deployment: gpt-4
```

**Result**: ✅ Azure OpenAI with GPT-4 deployed

**Time spent**: 15 minutes

---

### Service 6: Azure AI Document Intelligence (12 minutes)

**Why**: Extract structured data from invoices and forms

**Steps**:

#### Part A: Create Resource
1. Search **"Document Intelligence"** in Azure Portal
2. Click **"+ Create"**
3. Fill in:
   - **Resource Group**: `claims-validation-rg`
   - **Region**: East US (or your region)
   - **Name**: `claims-doctel-001`
   - **Pricing tier**: `Standard (S0)`
4. Click **"Review + create"** → **"Create"**
5. Wait for completion (3-4 minutes)

#### Part B: Get Credentials
6. Go to your Document Intelligence resource
7. Click **"Keys and Endpoint"** (left menu)
8. Copy:
    - **Key 1**: Save as `DocumentIntelligence-ApiKey`
    - **Endpoint**: Save as `DocumentIntelligence-Endpoint`

**Your values look like**:
```
DocumentIntelligence-ApiKey: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
DocumentIntelligence-Endpoint: https://eastus.api.cognitive.microsoft.com/
```

**Result**: ✅ Azure Document Intelligence ready

**Time spent**: 7 minutes

---

### Optional: Azure Communication Services (3 minutes)

**Why**: Send emails at scale (alternative to SMTP)

**Steps**:
1. Search **"Communication Services"** in Azure Portal
2. Click **"+ Create"**
3. Fill in:
   - **Resource Group**: `claims-validation-rg`
   - **Name**: `claims-acs-001`
4. Click **"Review + create"** → **"Create"**
5. Wait for completion (1-2 minutes)

**Note**: Using MailKit (SMTP) is sufficient for now. Skip this if you don't need scale.

**Total Phase 2**: 25-30 minutes

---

## 📝 Phase 3: Configure Secrets (10 minutes)

### Add API Keys to Key Vault

Now that you have all services created, add their API keys to Key Vault:

**Steps**:

1. Go back to your Key Vault
2. Click **"Secrets"** (left menu)
3. Add OpenAI secret:
   - Click **"+ Generate/Import"**
   - **Name**: `OpenAI-ApiKey`
   - **Value**: Paste your OpenAI API key
   - Click **"Create"**

4. Add Document Intelligence secret:
   - Click **"+ Generate/Import"**
   - **Name**: `DocumentIntelligence-ApiKey`
   - **Value**: Paste your Document Intelligence API key
   - Click **"Create"**

5. Add Endpoints (as secrets for consistency):
   - Click **"+ Generate/Import"**
   - **Name**: `OpenAI-Endpoint`
   - **Value**: `https://claims-openai-001.openai.azure.com/`
   - Click **"Create"**

   - Repeat for Document Intelligence endpoint

**Result**: ✅ All secrets in Key Vault

**Time spent**: 10 minutes

---

## 📊 Verification (10 minutes)

### Test Each Service

#### Test 1: SQL Database Connection

1. Download **Azure Data Studio** or use **SQL Server Management Studio (SSMS)**
2. Create new connection:
   - **Server**: `claims-server-001.database.windows.net`
   - **Username**: `sqladmin`
   - **Password**: Your password from earlier
   - **Database**: `ClaimsDB`
3. Click **"Connect"**
4. If successful, you see database in explorer

**Result**: ✅ Database connection works

---

#### Test 2: Blob Storage Upload

1. In Azure Portal, go to Blob Storage account
2. Click **"Containers"** → **"raw-documents"**
3. Click **"Upload"**
4. Select any small file (document.txt, image.jpg)
5. Click **"Upload"**
6. Verify file appears in container

**Result**: ✅ Blob Storage works

---

#### Test 3: Azure OpenAI

1. Go to https://oai.azure.com/
2. Go to **"Chat"** (left menu)
3. Select deployment: **"gpt-4"**
4. Type test prompt: `"Summarize this insurance claim: The policyholder was in a car accident."`
5. Verify response appears

**Result**: ✅ OpenAI works

---

#### Test 4: Document Intelligence

1. Go to https://documentintelligence.ai.azure.com/
2. Select **"Analyze"** tab
3. Select model: **"Invoice"**
4. Upload test invoice (PDF or image)
5. Verify it extracts data (amounts, dates, vendor)

**Result**: ✅ Document Intelligence works

---

#### Test 5: Key Vault Access

1. In Azure Portal, go to Key Vault
2. Click **"Secrets"**
3. Click on any secret (e.g., `SqlConnection-String`)
4. Verify you can see it (blurred by default)

**Result**: ✅ Key Vault works

---

## 📋 All Credentials Summary

**Create a file called `AZURE_CREDENTIALS.txt`** (save securely, don't commit to git!):

```
═══════════════════════════════════════════════════════════
AZURE CREDENTIALS FOR CLAIMS VALIDATION PROJECT
═══════════════════════════════════════════════════════════

RESOURCE GROUP:
Name: claims-validation-rg
Region: East US

═══════════════════════════════════════════════════════════
1. AZURE SQL DATABASE
═══════════════════════════════════════════════════════════
Server: claims-server-001.database.windows.net
Database: ClaimsDB
Username: sqladmin
Password: [YOUR PASSWORD]
Connection String: Server=tcp:claims-server-001.database.windows.net,1433;Initial Catalog=ClaimsDB;Persist Security Info=False;User ID=sqladmin;Password=[YOUR PASSWORD];Encrypt=True;

═══════════════════════════════════════════════════════════
2. AZURE BLOB STORAGE
═══════════════════════════════════════════════════════════
Account: claimsstorage20260101xyz
Containers: 
  - raw-documents
  - processed-documents
Connection String: DefaultEndpointsProtocol=https;AccountName=claimsstorage20260101xyz;AccountKey=[YOUR KEY];EndpointSuffix=core.windows.net

═══════════════════════════════════════════════════════════
3. AZURE OPENAI
═══════════════════════════════════════════════════════════
Resource: claims-openai-001
Endpoint: https://claims-openai-001.openai.azure.com/
API Key: [YOUR KEY]
Deployment Name: gpt-4

═══════════════════════════════════════════════════════════
4. AZURE DOCUMENT INTELLIGENCE
═══════════════════════════════════════════════════════════
Resource: claims-doctel-001
Endpoint: https://eastus.api.cognitive.microsoft.com/
API Key: [YOUR KEY]

═══════════════════════════════════════════════════════════
5. AZURE KEY VAULT
═══════════════════════════════════════════════════════════
Name: claims-keyvault-001
URL: https://claims-keyvault-001.vault.azure.net/
Secrets stored:
  - SqlConnection-String
  - BlobStorage-ConnectionString
  - OpenAI-ApiKey
  - OpenAI-Endpoint
  - DocumentIntelligence-ApiKey
  - DocumentIntelligence-Endpoint

═══════════════════════════════════════════════════════════
SAVE THIS FILE SECURELY!
DO NOT COMMIT TO GIT!
═══════════════════════════════════════════════════════════
```

---

## ✅ Final Checklist

```
SERVICES CREATED:
[ ] Resource Group (claims-validation-rg)
[ ] SQL Database (ClaimsDB on claims-server-001)
[ ] Blob Storage (claimsstorage + 2 containers)
[ ] Key Vault (claims-keyvault-001)
[ ] Azure OpenAI (claims-openai-001 with gpt-4)
[ ] Document Intelligence (claims-doctel-001)

CREDENTIALS SAVED:
[ ] SQL connection string
[ ] Blob connection string
[ ] OpenAI endpoint & API key
[ ] Document Intelligence endpoint & API key
[ ] All secrets added to Key Vault

TESTED:
[ ] SQL Database connection works
[ ] Blob Storage upload works
[ ] OpenAI can generate text
[ ] Document Intelligence can analyze documents
[ ] Key Vault can store secrets

TOTAL TIME SPENT: ~90 minutes ✅
```

---

## 🚀 Next Step: Update Your .NET Code

Now that services are set up, you'll need to:

1. Create `appsettings.json` with configuration
2. Install NuGet packages
3. Update `Program.cs` to use Key Vault
4. Run EF Core migrations
5. Implement the service classes

**See**: `IMPLEMENTATION_TEMPLATES.md` for code examples

---

## 🆘 Troubleshooting

**Problem**: "Region not available for Azure OpenAI"
**Solution**: Try East US, France Central, or UK South

**Problem**: "Storage account name already exists"
**Solution**: Storage names must be globally unique. Add timestamp: `claimsstorage20260101xyz`

**Problem**: "SQL Server connection fails"
**Solution**: Check firewall rule allows Azure services (should be "Yes")

**Problem**: "Can't find Document Intelligence in Marketplace"
**Solution**: Search "Document Intelligence" (not "Form Recognizer" - old name)

**Problem**: "Key Vault access denied"
**Solution**: Add yourself as "Key Vault Administrator" in Access Policies

---

## 💰 Cost Verification

**After setup, verify you're on free tier**:

1. Go to Azure Portal
2. Search **"Cost Management + Billing"**
3. Check **"Cost analysis"**
4. Verify usage is under $200 free credit

**You should see**: $0.00 charges (all on free credits)

---

## 📞 Support

If you get stuck:
1. Check Azure documentation (links in previous guide)
2. Use Azure Support (included with subscription)
3. Check specific service documentation

---

**CONGRATULATIONS!** 🎉 

You now have all 6 Azure services set up and ready for implementation!

**Next**: Install NuGet packages and start coding with the implementation templates.
