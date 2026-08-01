using Amazon;
using Amazon.BedrockAgentRuntime;
using Amazon.BedrockAgentRuntime.Model;
using Amazon.Runtime;

namespace HospitalityAI.Api.Services;

public class BedrockAgentService
{
    private readonly IAmazonBedrockAgentRuntime _bedrockAgentRuntime;
    private readonly string _agentId;
    private readonly string _agentAliasId;

    public BedrockAgentService(IConfiguration configuration)
    {
        var region = configuration["Bedrock:Region"] ?? "us-east-1";

        // AgentArn format: arn:aws:bedrock:<region>:<account>:agent/<agentId>
        var agentArn = configuration["Bedrock:AgentArn"]
            ?? throw new InvalidOperationException("Bedrock agent ARN is not configured.");

        _agentId = agentArn.Split('/').Last();
        _agentAliasId = configuration["Bedrock:AgentAliasId"] ?? "TSTALIASID";

        var awsRegion = RegionEndpoint.GetBySystemName(region);

        // Use explicit credentials when configured (local dev), otherwise fall
        // back to the default credential chain (IAM role, env vars, AWS profile).
        var accessKey = configuration["Bedrock:AccessKey"];
        var secretKey = configuration["Bedrock:SecretKey"];

        if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            && accessKey != "YOUR_AWS_ACCESS_KEY_ID")
        {
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            _bedrockAgentRuntime = new AmazonBedrockAgentRuntimeClient(credentials, awsRegion);
        }
        else
        {
            _bedrockAgentRuntime = new AmazonBedrockAgentRuntimeClient(awsRegion);
        }
    }

    public async Task<string> GetGuestHistoryAsync(string reservationCode, CancellationToken ct = default)
    {
        return await InvokeAgentAsync(
            $"Return the guest history for reservation code {reservationCode}.",
            ct);
    }

    /// <summary>
    /// Routes a guest chat message through the Bedrock Agent so the agent's
    /// configured instructions (set in the AWS console) are applied — identical
    /// to what the AWS Test panel exercises.
    /// A stable sessionId per guest keeps conversation context across turns.
    /// </summary>
    public async Task<string> ChatAsync(string guestId, string message, CancellationToken ct = default)
    {
        // Include a daily timestamp so sessions reset each day, preventing
        // stale agent context from old instruction versions affecting new chats.
        var dateStamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var sessionId = $"chat-{guestId}-{dateStamp}";
        return await InvokeAgentAsync(message, ct, sessionId);
    }

    /// <summary>
    /// Called immediately after check-in. The agent looks up the guest's history
    /// via its Lambda action group (keyed on reservationCode) and returns
    /// personalised recommendations as a plain-text response.
    /// </summary>
    public async Task<string> GetRecommendationsAsync(string reservationCode, CancellationToken ct = default)
    {
        return await InvokeAgentAsync(
            $"The guest with reservation code {reservationCode} has just checked in. " +
            $"Using their guest history, provide personalised activity and service recommendations " +
            $"for their stay. Be specific and concise.",
            ct);
    }

    // -----------------------------------------------------------------------
    // Shared helper — invokes the agent and collects all PayloadPart chunks
    // -----------------------------------------------------------------------
    private async Task<string> InvokeAgentAsync(string inputText, CancellationToken ct, string? sessionId = null)
    {
        var request = new InvokeAgentRequest
        {
            AgentId = _agentId,
            AgentAliasId = _agentAliasId,
            SessionId = sessionId ?? Guid.NewGuid().ToString(),
            InputText = inputText
        };

        var response = await _bedrockAgentRuntime.InvokeAgentAsync(request, ct);

        // ResponseStream extends EnumerableEventOutputStream<IEventStreamEvent, ...>
        // which implements IEnumerable — iterate with a plain foreach.
        var resultBuilder = new System.Text.StringBuilder();
        foreach (var ev in response.Completion)
        {
            if (ev is PayloadPart payload && payload.Bytes != null)
            {
                resultBuilder.Append(System.Text.Encoding.UTF8.GetString(
                    payload.Bytes.ToArray()));
            }
        }

        return resultBuilder.Length > 0
            ? resultBuilder.ToString()
            : "No response from Bedrock agent.";
    }
}
