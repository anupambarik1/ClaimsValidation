# AWS NLP Integration - Architecture & Decision Framework

**Date**: January 2, 2026  
**Project**: Claims Validation System  
**Phase**: 2 of 5 (NLP Integration)  
**Status**: Ready to Implement

---

## Executive Summary

Your Claims Validation system has successfully implemented **Document Intelligence** (AWS Textract). The next phase is **NLP Integration** using AWS Bedrock (Claude 3) and AWS Comprehend.

### What We're Building
A natural language understanding pipeline that:
1. **Summarizes** claim descriptions intelligently
2. **Detects fraud patterns** from narrative text
3. **Extracts entities** (names, dates, amounts, locations)
4. **Classifies claim type** (medical, auto, property, etc.)
5. **Generates personalized** claim decision letters

### Key Numbers
- **Cost**: $0.45/month for 1000 claims
- **Speed**: < 500ms per API call
- **Accuracy**: Claude 3 (frontier-grade model)
- **Implementation Time**: 4-5 hours
- **Code Size**: ~600 lines (2 new files)

---

## Current Architecture (Today)

```
User Submit Claim (PDF)
    ↓
Claims.Api (Endpoint)
    ↓
ClaimsService (Orchestrator)
    ├─→ Step 1: OCR (AWS Textract) ✅ DONE
    ├─→ Step 2: Document Analysis (Textract + Classification) ✅ DONE
    ├─→ Step 3: Rules Engine (Business logic) ✅ DONE
    ├─→ Step 4: ML Scoring (Fraud detection) ✅ DONE
    └─→ Step 5: Decision Engine ✅ DONE
        ├─ Auto-Approve
        ├─ Auto-Reject
        └─ Route to Manual Review
    ↓
Notification Service
    └─→ Email (Template-based) ⚠️ BASIC
```

### Current Limitations
- ❌ No understanding of claim narrative
- ❌ No fraud pattern detection from text
- ❌ No entity extraction
- ❌ No claim type classification
- ❌ Auto-generated responses use templates only

---

## New Architecture (After Implementation)

```
User Submit Claim (PDF + Description)
    ↓
Claims.Api
    ↓
ClaimsService (Enhanced)
    ├─→ Step 1: OCR (AWS Textract)
    ├─→ Step 2: Document Analysis (Textract)
    ├─→ Step 2.5: NLP Analysis ✨ NEW
    │   ├─→ Summarize (Bedrock Claude 3)
    │   ├─→ Analyze Fraud Narrative (Bedrock + Comprehend)
    │   ├─→ Extract Entities (Comprehend + Bedrock)
    │   └─→ Classify Claim Type (Bedrock)
    ├─→ Step 3: Rules Engine
    ├─→ Step 4: Enhanced ML Scoring
    │   └─→ Combine: OCR signals + NLP signals + Historical data
    ├─→ Step 5: Decision Engine
    │
    └─→ Notification Service ✨ ENHANCED
        └─→ Email (AI-Generated personalized letter)
```

### New Capabilities
- ✅ **Text Understanding**: Summarize complex narratives
- ✅ **Fraud Detection**: Pattern recognition in descriptions
- ✅ **Entity Recognition**: Extract structured data from unstructured text
- ✅ **Intent Classification**: Medical vs. Auto vs. Property claims
- ✅ **Personalization**: Custom decision letters per claimant

---

## Service Breakdown

### 1. AWSBedrockService (NEW - 80 lines)
**Responsibility**: Invoke Claude 3 model  
**Location**: `src/Claims.Services/Aws/AWSBedrockService.cs`

```csharp
public class AWSBedrockService
{
    // Initialize Bedrock client with credentials
    // Invoke model with JSON payloads
    // Parse Claude responses
    // Handle streaming (future enhancement)
}
```

**Methods**:
- `InvokeClaudeAsync(prompt: string)` → `Task<string>`

**Capabilities**:
- Text analysis & classification
- Prompt engineering
- JSON response parsing
- Error handling

---

### 2. AWSNlpService (ENHANCED - ~400 lines)
**Responsibility**: NLP orchestration with Comprehend + Bedrock  
**Location**: `src/Claims.Services/Aws/AWSNlpService.cs`

**Implements**: `INlpService` interface

```csharp
public class AWSNlpService : INlpService
{
    private AWSBedrockService _bedrockService;      // Claude 3
    private AmazonComprehendClient _comprehendClient; // Entity extraction
}
```

**Methods**:

#### 2.1 SummarizeClaimAsync()
**Input**: Claim description + OCR text  
**Process**:
1. Combine description and OCR text
2. Send to Claude with summarization prompt
3. Return concise summary

**Output**: String (50-100 words)

**Example**:
```
Input: "I was in a car accident on Main Street. Another car hit me on the passenger side. 
The other driver's insurance said they will cover damages. Police report number 12345. 
Damage estimate from body shop: $5,234.50"

Output: "Vehicle accident on Main Street, passenger side damage. Police report filed. 
Other driver's insurance will cover. Damage estimate: $5,234.50"
```

#### 2.2 AnalyzeFraudNarrativeAsync()
**Input**: Claim description  
**Process**:
1. Use Comprehend for sentiment analysis
2. Use Claude for fraud pattern detection
3. Combine signals into risk score
4. Return JSON with risk assessment

**Output**: 
```json
{
  "riskScore": 0.72,
  "riskLevel": "High",
  "indicators": ["Vague injury description", "Inconsistent timeline"],
  "recommendation": "Investigate",
  "sentimentAnalysis": "NEGATIVE"
}
```

#### 2.3 ExtractEntitiesAsync()
**Input**: Document text  
**Process**:
1. Use Comprehend for entity detection (persons, dates, locations)
2. Use Claude for claim type classification
3. Extract amounts using regex + Claude
4. Return structured JSON

**Output**:
```json
{
  "names": ["John Smith", "Mary Johnson"],
  "dates": ["2025-01-15", "2025-01-20"],
  "amounts": [5234.50, 150.00],
  "locations": ["Main Street", "City Hospital"],
  "claimType": "auto"
}
```

#### 2.4 GenerateClaimResponseAsync()
**Input**: Claimant name, decision, reason, amount  
**Process**:
1. Create context prompt with decision details
2. Use Claude to generate professional letter
3. Return personalized response

**Output**: Professional claim decision letter

---

## Integration Points

### In ClaimsService.ProcessClaimAsync()

#### Current Flow
```csharp
Step 1: OCR
    ↓
Step 2: Document Analysis
    ↓
Step 3: Rules Engine Validation
    ↓
Step 4: ML Scoring
    ↓
Step 5: Decision Engine
```

#### New Flow
```csharp
Step 1: OCR (Unchanged)
    ↓
Step 2: Document Analysis (Unchanged)
    ↓
Step 2.5: NLP Analysis ← NEW
    ├─ Summarize claim narrative
    ├─ Analyze fraud from text
    ├─ Extract entities
    └─ Detect claim type
    ↓
Step 3: Rules Engine Validation
    ├─ Use NLP entities for validation
    ├─ Consider fraud signals
    └─ Classify by claim type
    ↓
Step 4: Enhanced ML Scoring
    ├─ OCR signals: 60%
    ├─ NLP signals: 40%
    └─ Combined fraud score
    ↓
Step 5: Decision Engine
    ↓
Step 6: Generate Response (Enhanced)
    ├─ Use Claude for personalization
    └─ Include decision details
```

---

## Data Flow

### Claim Processing with NLP

```
┌─────────────────────────────────────────┐
│ API Request: SubmitClaim                │
│ ├─ PDF file                             │
│ ├─ Description: "I was in a wreck..."   │
│ └─ Claimant: John Smith                 │
└──────────────┬──────────────────────────┘
               ↓
        ┌──────────────┐
        │ AWS Textract │ ← OCR Extract
        │              │
        │ Output: text │
        │ confidence   │
        └──────┬───────┘
               ↓
┌─────────────────────────────────────────┐
│ NLP Analysis Input                      │
│ ├─ Description: "I was in a wreck..."   │
│ ├─ OCR text: "Policy #123..."           │
│ └─ Combined text                        │
└──────────────┬──────────────────────────┘
               ↓
        ┌──────────────────────┐
        │ AWS Bedrock (Claude) │ ← Summarization
        │                      │
        │ Output: Summary      │
        └──────┬───────────────┘
               ↓
        ┌──────────────────────┐
        │ AWS Comprehend       │ ← Entity Detection
        │                      │
        │ Output: Entities     │
        └──────┬───────────────┘
               ↓
        ┌──────────────────────┐
        │ AWS Bedrock (Claude) │ ← Fraud Analysis
        │                      │
        │ Output: Risk Score   │
        └──────┬───────────────┘
               ↓
┌─────────────────────────────────────────┐
│ NLP Results                             │
│ ├─ Summary: "Vehicle accident..."       │
│ ├─ Fraud Risk: 0.35 (Low)              │
│ ├─ Entities: John, 2025-01-15, $5234   │
│ └─ Claim Type: Auto                    │
└──────────────┬──────────────────────────┘
               ↓
        ┌──────────────────┐
        │ ML Model         │ ← Fraud Prediction
        │                  │
        │ Uses both:       │
        │ - OCR confidence │
        │ - NLP risk score │
        └──────┬───────────┘
               ↓
        ┌──────────────────┐
        │ Decision Engine  │ ← Decision
        │ Routes to:       │
        │ - Auto Approve   │
        │ - Auto Reject    │
        │ - Manual Review  │
        └──────┬───────────┘
               ↓
        ┌──────────────────────────┐
        │ Bedrock Response Gen      │ ← Letter
        │ Creates custom letter     │
        └──────┬───────────────────┘
               ↓
        ┌──────────────────┐
        │ Email Notif      │ ← Send
        │ Personalized     │
        └──────────────────┘
```

---

## Technology Stack

### Services Used
| Service | Purpose | Cost | Status |
|---------|---------|------|--------|
| Textract | Document OCR | ~$0.01 per page | ✅ Implemented |
| Bedrock Claude 3 | Text analysis | $0.0000035 per token | 🔄 Implementing |
| Comprehend | Entity extraction | $0.0001 per unit | 🔄 Implementing |
| S3 | Document storage | ~$0.023 per GB | ✅ Implemented |
| RDS | Database | TBD | 📋 Phase 3 |

### Programming Patterns

**1. Service Orchestration Pattern**
```csharp
// Multiple services composed in AWSNlpService
class AWSNlpService
{
    AWSBedrockService _bedrock;        // Claude 3
    ComprehendClient _comprehend;      // Entity extraction
    
    // Combine their outputs
}
```

**2. Dependency Injection**
```csharp
// In Program.cs
builder.Services.AddSingleton<AWSBedrockService>();
builder.Services.AddSingleton<INlpService, AWSNlpService>();
```

**3. Configuration-Driven**
```csharp
// Enable/disable via config
if (config["AWS:Bedrock:Enabled"] == "true")
{
    // Use Claude
}
```

**4. Error Handling & Fallback**
```csharp
try
{
    // Call Bedrock
}
catch
{
    // Fallback to template/default
}
```

---

## Cost Breakdown

### Per-Claim Costs

**Bedrock Claude 3 Haiku**:
- Summarization: ~200 input tokens → $0.00005
- Fraud Analysis: ~150 input, ~100 output → $0.00015
- Entity Classification: ~100 input, ~80 output → $0.00008
- **Bedrock Subtotal**: ~$0.00028

**AWS Comprehend**:
- Sentiment Detection: ~$0.0001
- Entity Detection: ~$0.0001
- **Comprehend Subtotal**: ~$0.0002

**Per-Claim NLP Cost**: **~$0.00048**

### Monthly Estimate

| Volume | Bedrock | Comprehend | Total | AVG/month |
|--------|---------|-----------|-------|-----------|
| 100 claims | $0.03 | $0.01 | $0.04 | $0.04 |
| 500 claims | $0.14 | $0.05 | $0.19 | $0.19 |
| 1000 claims | $0.28 | $0.10 | $0.38 | $0.38 |
| 5000 claims | $1.40 | $0.50 | $1.90 | $1.90 |

**Typical MVP Load**: 1000 claims/month = **$0.38/month**

---

## Comparison: AWS vs Azure for NLP

### Option 1: AWS Bedrock + Comprehend (RECOMMENDED)
✅ **Pros**:
- Claude 3 is frontier-grade model
- Pay per token (most cost-effective)
- Simple API integration
- Already using AWS stack

❌ **Cons**:
- Bedrock still in limited availability
- Need to request model access

**Cost**: $0.38/month for 1000 claims

### Option 2: Azure OpenAI
✅ **Pros**:
- GPT-4 available
- Enterprise support

❌ **Cons**:
- Higher per-token cost
- Requires separate subscription
- Different API

**Cost**: $1.50+/month for 1000 claims

---

## Implementation Complexity

### Effort Breakdown
```
Installation & Setup          15 minutes
├─ NuGet packages            5 min
├─ Configuration             5 min
└─ Verification              5 min

Service Implementation        90 minutes
├─ AWSBedrockService        30 min
├─ Enhanced AWSNlpService   60 min
└─ Testing                  15 min

Integration & Testing        120 minutes
├─ DI registration          15 min
├─ ClaimsService updates    45 min
├─ Unit tests               30 min
├─ Integration tests        30 min
└─ Manual testing           30 min

Total                        225 minutes (3.75 hours)
```

---

## Risk Assessment

### Low Risk
- ✅ Using established AWS services
- ✅ API-based (no infrastructure changes)
- ✅ Easy to disable if issues arise
- ✅ Fallbacks in place (template responses)

### Medium Risk
- ⚠️ Bedrock model availability (early service)
- ⚠️ Claude 3 model changes (new model)
- ⚠️ API rate limits with high volume

### Mitigation Strategies
1. **Rate Limiting**: Queue claims, batch processing
2. **Fallback**: Template responses if API fails
3. **Monitoring**: CloudWatch alerts on errors
4. **Caching**: Cache summaries for identical claims

---

## Success Metrics

### Functional
- [ ] Summarization accuracy: 90%+ human review satisfaction
- [ ] Fraud detection: Recall > 80% of known fraud
- [ ] Entity extraction: 85%+ accuracy
- [ ] Response generation: Professional quality

### Performance
- [ ] API response < 500ms per method
- [ ] Claims processed < 3 seconds end-to-end
- [ ] 99%+ uptime for NLP services

### Cost
- [ ] Monthly bill < $1 for MVP
- [ ] < $50/month for production 1000+ claims

### Operational
- [ ] Comprehensive logging
- [ ] Error alerts configured
- [ ] Automated rollback plan
- [ ] Documentation complete

---

## Implementation Sequence

### Week 1 (You are here)
1. **Setup** (Today): Review this doc, gather AWS credentials
2. **Dev 1** (Tomorrow): Install packages, create AWSBedrockService
3. **Dev 2** (Day 3): Enhance AWSNlpService, update DI
4. **Dev 3** (Day 4): Integrate with ClaimsService, unit tests
5. **Testing** (Day 5): Integration tests, manual validation

### Week 2
1. **Monitoring**: Set up CloudWatch dashboards
2. **Optimization**: Tune prompts, test performance
3. **Documentation**: Create runbooks, troubleshooting guide
4. **Handoff**: Prepare for production deployment

### Week 3+
1. **Production**: Deploy to production environment
2. **Monitoring**: Daily checks for first week
3. **Optimization**: Based on real usage patterns
4. **Planning**: Phase 3 (Database & RDS)

---

## Next Steps

### Immediate (Next 30 minutes)
1. Review this document and checklists
2. Gather AWS credentials
3. Request Bedrock model access (if not already approved)

### Phase 1: Setup (15 minutes)
1. Install NuGet packages
2. Update appsettings.json
3. Verify configuration

### Phase 2: Development (60 minutes)
1. Create AWSBedrockService
2. Enhance AWSNlpService
3. Update Program.cs

### Phase 3: Integration (45 minutes)
1. Add NLP to ClaimsService
2. Write unit tests
3. Write integration tests

### Phase 4: Testing (90 minutes)
1. Run all tests
2. Manual end-to-end testing
3. Verify accuracy

### Phase 5: Monitoring (30 minutes)
1. Set up CloudWatch
2. Configure billing alerts
3. Create monitoring dashboard

---

## References

### AWS Documentation
- [Bedrock Documentation](https://docs.aws.amazon.com/bedrock/)
- [Claude 3 Models](https://docs.anthropic.com/claude/)
- [Comprehend API](https://docs.aws.amazon.com/comprehend/)
- [Cost Estimation](https://calculator.aws/)

### Implementation Guides
- [AWS_NLP_BEDROCK_INTEGRATION.md](./AWS_NLP_BEDROCK_INTEGRATION.md) - Full technical guide
- [AWS_NLP_IMPLEMENTATION_CHECKLIST.md](./AWS_NLP_IMPLEMENTATION_CHECKLIST.md) - Step-by-step checklist
- [AWS_TEXTRACT_QUICK_START.md](./AWS_TEXTRACT_QUICK_START.md) - Similar pattern for reference

### Code Examples
- See `AWSTextractDocumentIntelligenceService.cs` for credential handling pattern
- See `AWSTextractService.cs` for error handling pattern
- See `ClaimsService.cs` for integration pattern

---

**Ready to begin?** Start with the implementation checklist → `AWS_NLP_IMPLEMENTATION_CHECKLIST.md`

