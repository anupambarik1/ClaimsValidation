using Claims.Infrastructure.Data;
using Claims.Services.Azure;
using Claims.Services.Aws;
using Claims.Services.Implementations;
using Claims.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Amazon.Textract;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Claims Validation API", 
        Version = "v1",
        Description = "AI-powered claims validation system with OCR, fraud detection, and automated decision-making"
    });
});

// Configure Database
builder.Services.AddDbContext<ClaimsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ClaimsDb");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        // Use in-memory database for development/demo
        options.UseInMemoryDatabase("ClaimsDb");
    }
});

// Register Core Application Services
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<IDocumentAnalysisService, AWSTextractDocumentIntelligenceService>();
builder.Services.AddScoped<IRulesEngineService, RulesEngineService>();
builder.Services.AddSingleton<IMlScoringService, MlScoringService>(); // Singleton to keep ML model loaded
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IS3UploadService, AWSS3UploadService>(); // Add S3 upload service

// Register AWS Document Intelligence Service (Textract with advanced features)
var useAwsTextractIntelligence = builder.Configuration.GetValue<bool>("AWS:Textract:Enabled");
if (useAwsTextractIntelligence)
{
    builder.Services.AddScoped<AWSTextractDocumentIntelligenceService>();
    builder.Services.AddScoped<IOcrService, AWSTextractService>();
}

// Determine provider preference using feature flags or fallback to CloudProvider
var useAwsGlobal = builder.Configuration.GetValue<bool>("FeatureFlags:UseAWS");
var useAzureGlobal = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzure");
var cloudProvider = builder.Configuration.GetValue<string>("CloudProvider")?.ToLowerInvariant() ?? "azure";

string chosenProvider;
if (useAwsGlobal && !useAzureGlobal)
{
    chosenProvider = "aws";
}
else if (useAzureGlobal && !useAwsGlobal)
{
    chosenProvider = "azure";
}
else
{
    chosenProvider = cloudProvider;
}

// Register NLP service
var useAzureOpenAI = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureOpenAI");
var useAwsBedrock = builder.Configuration.GetValue<bool>("AWS:Bedrock:Enabled");
var awsEnabled = builder.Configuration.GetValue<bool>("AWS:Enabled");

if (awsEnabled && useAwsBedrock)
{
    builder.Services.AddSingleton<INlpService, Claims.Services.Aws.AWSNlpService>();
    builder.Services.AddSingleton<AWSBedrockService>();
}
else if (useAzureOpenAI)
{
    builder.Services.AddSingleton<INlpService, AzureOpenAIService>();
}
else if (awsEnabled)
{
    builder.Services.AddSingleton<INlpService, Claims.Services.Aws.AWSNlpService>();
}
else
{
    builder.Services.AddSingleton<INlpService, AzureOpenAIService>();
}

// Register Blob storage service
var useAzureBlob = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureBlobStorage");
var useAwsS3 = builder.Configuration.GetValue<bool>("AWS:Enabled");
if (useAzureBlob && !useAwsS3)
{
    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
}
else if (useAwsS3 && !useAzureBlob)
{
    builder.Services.AddSingleton<IBlobStorageService, Claims.Services.Aws.AWSBlobStorageService>();
}
else if (chosenProvider == "azure")
{
    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
}
else
{
    builder.Services.AddSingleton<IBlobStorageService, Claims.Services.Aws.AWSBlobStorageService>();
}

// Register Email service
var enableEmailNotifications = builder.Configuration.GetValue<bool>("FeatureFlags:SendEmailNotifications");
var useAzureEmail = builder.Configuration.GetValue<bool>("Azure:CommunicationServices:Enabled");
var useAwsSes = builder.Configuration.GetValue<bool>("AWS:SES:Enabled");

if (useAzureEmail && !useAwsSes && enableEmailNotifications)
{
    builder.Services.AddSingleton<IEmailService, AzureEmailService>();
}
else if (useAwsSes && !useAzureEmail && enableEmailNotifications)
{
    builder.Services.AddSingleton<IEmailService, Claims.Services.Aws.AWSEmailService>();
}
else if (chosenProvider == "azure")
{
    builder.Services.AddSingleton<IEmailService, AzureEmailService>();
}
else
{
    builder.Services.AddSingleton<IEmailService, Claims.Services.Aws.AWSEmailService>();
}

// Register OCR (Textract) if explicitly enabled for AWS
var useAwsTextract = builder.Configuration.GetValue<bool>("AWS:Textract:Enabled");
if (useAwsTextract)
{
    builder.Services.AddScoped<IOcrService, Claims.Services.Aws.AWSTextractService>();
}
else
{
    // Existing OCR registration handled earlier (Azure Document Intelligence or Tesseract fallback)
}

// Add Application Insights (if configured)
var appInsightsConnectionString = builder.Configuration["Azure:ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Claims API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", (IConfiguration config) => 
{
    var featureFlags = new
    {
        AzureDocumentIntelligence = config.GetValue<bool>("FeatureFlags:UseAzureDocumentIntelligence"),
        AzureOpenAI = config.GetValue<bool>("FeatureFlags:UseAzureOpenAI"),
        AzureBlobStorage = config.GetValue<bool>("FeatureFlags:UseAzureBlobStorage"),
        TesseractOCR = config.GetValue<bool>("FeatureFlags:UseTesseractOCR"),
        LocalMLModel = config.GetValue<bool>("FeatureFlags:UseLocalMLModel"),
        EmailNotifications = config.GetValue<bool>("FeatureFlags:SendEmailNotifications")
    };

    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        service = "Claims.Api",
        version = "1.0.0",
        environment = app.Environment.EnvironmentName,
        features = new[]
        {
            "OCR Document Processing (Tesseract / Azure Document Intelligence)",
            "ML Fraud Detection (ML.NET)",
            "NLP Analysis (Azure OpenAI - when enabled)",
            "Business Rules Engine",
            "Automated Decision Making",
            "Azure Blob Storage (when enabled)",
            "Email Notifications (Azure Communication Services)"
        },
        activeFeatures = featureFlags
    });
});

// API info endpoint
app.MapGet("/api", () => Results.Ok(new
{
    name = "Claims Validation API",
    version = "1.0.0",
    description = "AI-powered claims validation with real-time fraud detection",
    platform = "Azure",
    endpoints = new {
        submitClaim = "POST /api/claims",
        processClaim = "POST /api/claims/{claimId}/process",
        submitAndProcess = "POST /api/claims/submit-and-process",
        getStatus = "GET /api/claims/{claimId}/status",
        addDocument = "POST /api/claims/{claimId}/documents",
        health = "GET /health"
    }
}));

app.Run();
