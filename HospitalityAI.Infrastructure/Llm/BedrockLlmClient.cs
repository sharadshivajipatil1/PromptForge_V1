namespace HospitalityAI.Infrastructure.Llm;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using HospitalityAI.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

/// <summary>
/// ILlmClient implementation that calls Amazon Bedrock InvokeModel API.
/// Supports both Amazon Nova models and Anthropic Claude models — the payload
/// format is selected automatically based on the configured ModelId.
/// Used by ConciergeAgent.HandleChatAsync when UseBedrock = true.
/// </summary>
public class BedrockLlmClient : ILlmClient
{
    private readonly IAmazonBedrockRuntime _runtime;
    private readonly string _modelId;

    public BedrockLlmClient(IConfiguration configuration, RuntimeModeService runtimeMode)
    {
        var region = configuration["Bedrock:Region"] ?? "us-east-1";
        _modelId = configuration["Bedrock:ModelId"]
                   ?? runtimeMode.BedrockModelId
                   ?? "amazon.nova-micro-v1:0";

        var awsRegion = RegionEndpoint.GetBySystemName(region);

        var accessKey = configuration["Bedrock:AccessKey"];
        var secretKey = configuration["Bedrock:SecretKey"];

        if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            && accessKey != "YOUR_AWS_ACCESS_KEY_ID")
        {
            var creds = new BasicAWSCredentials(accessKey, secretKey);
            _runtime = new AmazonBedrockRuntimeClient(creds, awsRegion);
        }
        else
        {
            // Falls back to environment variables, IAM instance role, or AWS profile.
            _runtime = new AmazonBedrockRuntimeClient(awsRegion);
        }
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        return _modelId.StartsWith("amazon.nova", StringComparison.OrdinalIgnoreCase)
            ? await InvokeNovaAsync(systemPrompt, userPrompt, ct)
            : await InvokeClaudeAsync(systemPrompt, userPrompt, ct);
    }

    // -----------------------------------------------------------------------
    // Amazon Nova (Micro / Lite / Pro)
    // Payload: { "system": [{"text":"..."}], "messages": [...], "inferenceConfig": {...} }
    // Response: { "output": { "message": { "content": [ { "text": "..." } ] } } }
    // -----------------------------------------------------------------------
    private async Task<string> InvokeNovaAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var payload = new
        {
            system = new[] { new { text = systemPrompt } },
            messages = new[]
            {
                new { role = "user", content = new[] { new { text = userPrompt } } }
            },
            inferenceConfig = new
            {
                max_new_tokens = 512,
                temperature = 0.7,
                top_p = 0.9
            }
        };

        var responseJson = await InvokeModelRawAsync(payload, ct);
        var doc = JsonNode.Parse(responseJson);

        // Nova response shape: output.message.content[0].text
        var text = doc?["output"]?["message"]?["content"]?
            .AsArray()
            .FirstOrDefault()
            ?["text"]?.GetValue<string>();

        return text ?? "I'm sorry, I couldn't generate a response right now.";
    }

    // -----------------------------------------------------------------------
    // Anthropic Claude 3 (Haiku / Sonnet / Opus)
    // Payload: { "anthropic_version": "...", "system": "...", "messages": [...] }
    // Response: { "content": [ { "type": "text", "text": "..." } ] }
    // -----------------------------------------------------------------------
    private async Task<string> InvokeClaudeAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var payload = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 512,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        var responseJson = await InvokeModelRawAsync(payload, ct);
        var doc = JsonNode.Parse(responseJson);

        // Claude 3 response shape: content[].text where type == "text"
        var text = doc?["content"]?
            .AsArray()
            .FirstOrDefault(node => node?["type"]?.GetValue<string>() == "text")
            ?["text"]?.GetValue<string>();

        return text ?? "I'm sorry, I couldn't generate a response right now.";
    }

    // -----------------------------------------------------------------------
    // Shared: serialize payload → InvokeModelAsync → return response body string
    // -----------------------------------------------------------------------
    private async Task<string> InvokeModelRawAsync(object payload, CancellationToken ct)
    {
        var bodyJson = JsonSerializer.Serialize(payload);
        var bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

        var request = new InvokeModelRequest
        {
            ModelId = _modelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(bodyBytes)
        };

        var response = await _runtime.InvokeModelAsync(request, ct);

        using var reader = new StreamReader(response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct);
    }
}
