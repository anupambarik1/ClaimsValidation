# AWS NLP Implementation - Quick Checklist

**Project**: Claims Validation System  
**Feature**: NLP with AWS Bedrock (Claude 3) + Comprehend  
**Status**: Ready to Implement  
**Estimated Time**: 4-5 hours

---

## Prerequisites Checklist

### AWS Setup
- [ ] AWS Account with Bedrock & Comprehend access
- [ ] Region: **us-east-1** (or your chosen region)
- [ ] IAM credentials ready (Access Key + Secret Key)
- [ ] Request access to Claude 3 models in Bedrock console

### Development Setup
- [ ] Visual Studio / VS Code open with ClaimsValidation solution
- [ ] .NET 9.0 SDK installed (`dotnet --version`)
- [ ] Terminal access in project root
- [ ] Solution builds cleanly without errors

---

## Phase 1: Install Dependencies (15 minutes)

### Step 1.1: Install NuGet Packages
```powershell
cd C:\Demo_Projects\Claim_Validation
dotnet add src/Claims.Services/Claims.Services.csproj package AWSSDK.Bedrock
dotnet add src/Claims.Services/Claims.Services.csproj package AWSSDK.Comprehend
dotnet restore
```
- [ ] Command runs without errors
- [ ] Both packages appear in Claims.Services.csproj

### Step 1.2: Verify Installation
```powershell
cd src/Claims.Services
dotnet list package
```
- [ ] AWSSDK.Bedrock visible in package list
- [ ] AWSSDK.Comprehend visible in package list

---

## Phase 2: Create Bedrock Service (30 minutes)

### Step 2.1: Create AWSBedrockService.cs
**File**: `src/Claims.Services/Aws/AWSBedrockService.cs`  
**Source**: AWS_NLP_BEDROCK_INTEGRATION.md (Step 2)

```powershell
# Verify file created
Test-Path C:\Demo_Projects\Claim_Validation\src\Claims.Services\Aws\AWSBedrockService.cs
```
- [ ] File created at correct path
- [ ] Code compiles without errors
- [ ] Constructor properly initializes Bedrock client

### Step 2.2: Test Compilation
```powershell
dotnet build ClaimsValidation.sln
```
- [ ] No compilation errors
- [ ] Claims.Services project builds successfully

---

## Phase 3: Enhanced NLP Service (60 minutes)

### Step 3.1: Replace AWSNlpService.cs
**File**: `src/Claims.Services/Aws/AWSNlpService.cs`  
**Action**: Replace entire file with enhanced version  
**Source**: AWS_NLP_BEDROCK_INTEGRATION.md (Step 3)

- [ ] File replaced with new implementation
- [ ] All 4 methods implemented:
  - [ ] SummarizeClaimAsync()
  - [ ] AnalyzeFraudNarrativeAsync()
  - [ ] ExtractEntitiesAsync()
  - [ ] GenerateClaimResponseAsync()

### Step 3.2: Verify NLP Service
```powershell
# Check imports are correct
Select-String -Path "src/Claims.Services/Aws/AWSNlpService.cs" -Pattern "using Amazon"
```
- [ ] Imports for Bedrock included
- [ ] Imports for Comprehend included
- [ ] No duplicate using statements

### Step 3.3: Build Check
```powershell
dotnet build src/Claims.Services/Claims.Services.csproj
```
- [ ] No errors
- [ ] No warnings about unused references

---

## Phase 4: Configuration (10 minutes)

### Step 4.1: Update appsettings.json
**File**: `src/Claims.Api/appsettings.json`  
**Action**: Add/update AWS Bedrock section

```json
{
  "AWS": {
    "Enabled": true,
    "Region": "us-east-1",
    "AccessKey": "YOUR_AWS_ACCESS_KEY",
    "SecretKey": "YOUR_AWS_SECRET_KEY",
    "Bedrock": {
      "Enabled": true,
      "Model": "anthropic.claude-3-5-haiku-20241022-v1:0",
      "MaxTokens": 1024,
      "Temperature": 0.7
    },
    "Comprehend": {
      "Enabled": true
    }
  },
  "FeatureFlags": {
    "UseAWSBedrock": true
  }
}
```

- [ ] Bedrock section added
- [ ] AccessKey filled in
- [ ] SecretKey filled in
- [ ] Model ID correct
- [ ] Temperature set to 0.7

### Step 4.2: Verify Configuration
```powershell
# Check JSON is valid
Get-Content src/Claims.Api/appsettings.json | ConvertFrom-Json | Out-Null
```
- [ ] No JSON syntax errors
- [ ] All required keys present

---

## Phase 5: Update DI Registration (15 minutes)

### Step 5.1: Update Program.cs
**File**: `src/Claims.Api/Program.cs`  
**Action**: Update NLP service registration

Find this section:
```csharp
// Register NLP service - prefer AWS Bedrock if both enabled
var useAzureOpenAI = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureOpenAI");
var useAwsBedrock = builder.Configuration.GetValue<bool>("AWS:Bedrock:Enabled");
```

Replace with code from AWS_NLP_BEDROCK_INTEGRATION.md (Step 4)

- [ ] NLP registration updated
- [ ] AWSBedrockService registration added
- [ ] Bedrock preferred over Azure when both enabled

### Step 5.2: Build Check
```powershell
dotnet build ClaimsValidation.sln
```
- [ ] All 4 projects compile
- [ ] No unresolved references

---

## Phase 6: ClaimsService Integration (45 minutes)

### Step 6.1: Add NLP Dependency
**File**: `src/Claims.Services/Implementations/ClaimsService.cs`

**Add field**:
```csharp
private readonly INlpService _nlpService;
```

**Add to constructor parameter list**:
```csharp
public ClaimsService(
    // ... existing parameters ...
    INlpService nlpService)
{
    _nlpService = nlpService;
    // ... rest of constructor
}
```

- [ ] Field declared
- [ ] Constructor parameter added
- [ ] Parameter assigned to field

### Step 6.2: Add NLP Processing in ProcessClaimAsync()
**Action**: Add after Step 2 (Document Analysis)

Insert this code block (from AWS_NLP_BEDROCK_INTEGRATION.md Step 5):

```csharp
// Step 2.5: NLP Analysis
_logger.LogInformation("Step 2.5: NLP Analysis");

// Summarize claim
var summary = await _nlpService.SummarizeClaimAsync(
    claim.Description ?? string.Empty,
    ocrResults.FirstOrDefault()?.ExtractedText ?? string.Empty);
// ... rest of NLP code
```

- [ ] Code added at correct location
- [ ] Summary variable used
- [ ] Fraud analysis captured
- [ ] Entities extracted

### Step 6.3: Update ML Scoring
**Action**: Combine NLP fraud score with ML score

```csharp
// Step 4: Enhanced ML Scoring (MODIFIED)
var nlpFraudScore = ExtractScoreFromJson(fraudAnalysis);
var combinedFraudScore = (fraudScore * 0.6) + (nlpFraudScore * 0.4);
```

- [ ] Fraud scores combined with 60/40 weight
- [ ] Combined score used for decision

### Step 6.4: Verify Compilation
```powershell
dotnet build src/Claims.Services/Claims.Services.csproj
```
- [ ] No errors
- [ ] All methods resolved

---

## Phase 7: Testing & Validation (90 minutes)

### Step 7.1: Unit Tests
Create `src/Claims.Services.Tests/Aws/AWSNlpServiceTests.cs`

Add test methods:
```csharp
[TestClass]
public class AWSNlpServiceTests
{
    [TestMethod]
    public async Task SummarizeClaimAsync_WithValidText_ReturnsSummary()
    {
        // Arrange & Act
        // Assert
    }

    [TestMethod]
    public async Task AnalyzeFraudNarrativeAsync_WithHighRiskText_ReturnsHighScore()
    {
        // Test
    }

    [TestMethod]
    public async Task ExtractEntitiesAsync_WithClaimText_ReturnsEntities()
    {
        // Test
    }
}
```

- [ ] Test file created
- [ ] All 3 test methods written
- [ ] Tests compile without errors

### Step 7.2: Run Unit Tests
```powershell
dotnet test src/Claims.Services.Tests/Claims.Services.Tests.csproj
```
- [ ] All tests pass
- [ ] No test execution errors

### Step 7.3: Integration Test
Create test in `Claims.Services.Tests`:

```csharp
[TestMethod]
public async Task ProcessClaim_WithNlp_CompletesSuccessfully()
{
    // Create mock claim
    // Process through pipeline
    // Assert NLP results present
}
```

- [ ] Integration test passes
- [ ] Full pipeline works end-to-end

### Step 7.4: Manual Testing
Launch application:
```powershell
cd src/Claims.Api
dotnet run
```

Test via Swagger or API client:
- [ ] POST /api/claims with test data
- [ ] Response includes NLP analysis results
- [ ] Summarization works correctly
- [ ] Fraud score calculated
- [ ] Entities extracted
- [ ] Response generation works
- [ ] No timeout errors (< 3 sec response time)

### Step 7.5: Verify Logging
```powershell
# Check console output for NLP logs
```
- [ ] See log: "Step 2.5: NLP Analysis"
- [ ] See log: "Claim summarized successfully"
- [ ] See log: "Fraud analysis completed"
- [ ] See log: "Entities extracted"

---

## Phase 8: Cost & Performance Monitoring (15 minutes)

### Step 8.1: AWS CloudWatch Setup
- [ ] Log into AWS Console
- [ ] Go to CloudWatch → Dashboards
- [ ] Create dashboard for:
  - [ ] Bedrock API calls
  - [ ] Comprehend API calls
  - [ ] Cost tracking
  - [ ] Error rates

### Step 8.2: Set Billing Alerts
- [ ] Go to AWS Billing Console
- [ ] Set alert at $20/month
- [ ] Receive email notifications

### Step 8.3: Performance Monitoring
```powershell
# Check average claim processing time
# Should be < 3 seconds total
```
- [ ] Response time acceptable
- [ ] No API timeout issues

---

## Phase 9: Documentation & Cleanup (30 minutes)

### Step 9.1: Update README
**File**: `docs/README.md`

Add NLP section:
```markdown
## NLP Integration
- ✅ AWS Bedrock (Claude 3 Haiku) for text analysis
- ✅ AWS Comprehend for entity extraction
- ✅ Integrated with claims processing pipeline
- Cost: ~$0.45/month for 1000 claims
```

- [ ] README updated with NLP info

### Step 9.2: Update Architecture Diagram
**File**: `docs/architecture/COMPLETE_TECHNICAL_GUIDE.md`

Add NLP box to data flow diagram
- [ ] Diagram updated to show NLP step

### Step 9.3: Code Cleanup
- [ ] Remove any debug code
- [ ] Clean up temporary logging
- [ ] Format code (dotnet format)

```powershell
dotnet format ClaimsValidation.sln
```

- [ ] Code formatted
- [ ] No trailing whitespace

### Step 9.4: Final Build Check
```powershell
dotnet build ClaimsValidation.sln
dotnet test ClaimsValidation.sln
```

- [ ] Full solution builds cleanly
- [ ] All tests pass
- [ ] No warnings

---

## Success Criteria Verification

### Code Quality
- [ ] No compiler errors
- [ ] No compiler warnings
- [ ] All code formatted consistently
- [ ] Comments explain complex logic

### Functionality
- [ ] Summarization produces valid output
- [ ] Fraud detection scores in range [0, 1]
- [ ] Entity extraction returns correct types
- [ ] Response generation creates proper letters
- [ ] All error cases handled gracefully

### Performance
- [ ] Single claim processes in < 3 seconds
- [ ] 10 concurrent claims process without timeout
- [ ] Memory usage stable over time
- [ ] No resource leaks

### Cost
- [ ] Monthly estimate < $1
- [ ] Bedrock calls optimized (no large inputs)
- [ ] Comprehend usage within free tier
- [ ] Cost tracking visible in CloudWatch

### Documentation
- [ ] Implementation guide complete
- [ ] Code comments explain key logic
- [ ] README updated
- [ ] Architecture documentation current

---

## Rollback Plan (If Needed)

If you encounter critical issues:

```powershell
# 1. Revert AWSNlpService to original
git checkout HEAD -- src/Claims.Services/Aws/AWSNlpService.cs

# 2. Remove new AWSBedrockService
rm src/Claims.Services/Aws/AWSBedrockService.cs

# 3. Revert Program.cs
git checkout HEAD -- src/Claims.Api/Program.cs

# 4. Rebuild
dotnet build ClaimsValidation.sln
```

- [ ] Rollback command saved
- [ ] Git repository available
- [ ] Backup of current state

---

## Post-Implementation Tasks

### Immediate (Day 1)
- [ ] Monitor CloudWatch for API errors
- [ ] Check billing alerts
- [ ] Validate accuracy of fraud scores
- [ ] Verify response generation quality

### Short-term (Week 1)
- [ ] Collect fraud detection accuracy metrics
- [ ] A/B test different prompts
- [ ] Optimize token limits
- [ ] Document learnings

### Medium-term (Month 1)
- [ ] Analyze cost trends
- [ ] Retrain ML model with NLP features
- [ ] Implement prompt caching
- [ ] Scale to production load

---

## Support Resources

### Documentation
- [AWS_NLP_BEDROCK_INTEGRATION.md](./AWS_NLP_BEDROCK_INTEGRATION.md) - Full implementation guide
- [AWS Bedrock Docs](https://docs.aws.amazon.com/bedrock/)
- [Claude 3 Models](https://docs.anthropic.com/claude/)
- [AWS Comprehend API](https://docs.aws.amazon.com/comprehend/)

### Quick Commands
```powershell
# Install packages
dotnet add Claims.Services package AWSSDK.Bedrock

# Build solution
dotnet build ClaimsValidation.sln

# Run tests
dotnet test

# Format code
dotnet format

# Build specific project
dotnet build src/Claims.Services/Claims.Services.csproj
```

### Common Issues
- **Bedrock model access**: Request in AWS console under Model Access
- **Credentials error**: Verify AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY in appsettings.json
- **Comprehend permission denied**: Check IAM policy includes comprehend:*
- **Timeout errors**: Reduce MaxTokens from 1024 to 512

---

**Next Step**: Start with Phase 1 - Install Dependencies

