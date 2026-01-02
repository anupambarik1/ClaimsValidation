using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Claims.Services.Aws;

/// <summary>
/// AWS Bedrock service for invoking Claude 3 models.
/// Handles text generation, summarization, and analysis tasks.
/// </summary>
public class AWSBedrockService
{
    private readonly ILogger<AWSBedrockService> _logger;
    private readonly IAmazonBedrockRuntime _bedrockClient;
    private readonly string _modelId;
    private readonly int _maxTokens;
    private readonly double _temperature;

    public AWSBedrockService(IConfiguration configuration, ILogger<AWSBedrockService> logger)
    {
        _logger = logger;
        
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        var regionName = configuration["AWS:Region"] ?? "us-east-1";
        
        AWSCredentials credentials = new BasicAWSCredentials(accessKey, secretKey);
        var region = RegionEndpoint.GetBySystemName(regionName);
        
        _bedrockClient = new AmazonBedrockRuntimeClient(credentials, region);
        _modelId = configuration["AWS:Bedrock:Model"] ?? "anthropic.claude-3-5-haiku-20241022-v1:0";
        _maxTokens = configuration.GetValue<int>("AWS:Bedrock:MaxTokens", 1024);
        _temperature = configuration.GetValue<double>("AWS:Bedrock:Temperature", 0.7);
        
        _logger.LogInformation("Bedrock service initialized with model: {ModelId}", _modelId);
    }

    /// <summary>
    /// Invokes Claude model with the given prompt and returns the response.
    /// </summary>
    public async Task<string> InvokeClaudeAsync(string prompt)
    {
        try
        {
            var requestBody = JsonSerializer.Serialize(new
            {
                anthropic_version = "bedrock-2023-06-01",
                max_tokens = _maxTokens,
                temperature = _temperature,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            });

            var request = new InvokeModelRequest
            {
                ModelId = _modelId,
                Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody)),
                ContentType = "application/json"
            };

            var response = await _bedrockClient.InvokeModelAsync(request);
            
            using (var reader = new System.IO.StreamReader(response.Body))
            {
                var responseText = await reader.ReadToEndAsync();
                var jsonResponse = JsonDocument.Parse(responseText);
                
                var content = jsonResponse.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();
                
                return content ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking Bedrock Claude model");
            throw;
        }
    }
}
