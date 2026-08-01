namespace HospitalityAI.Infrastructure.Llm;

using HospitalityAI.Domain.Interfaces;

public class SwitchingLlmClient : ILlmClient
{
    private readonly RuntimeModeService _runtimeModeService;
    private readonly ILlmClient _mockLlmClient;
    private readonly ILlmClient _bedrockLlmClient;

    public SwitchingLlmClient(RuntimeModeService runtimeModeService, MockLlmClient mockLlmClient, BedrockLlmClient bedrockLlmClient)
    {
        _runtimeModeService = runtimeModeService;
        _mockLlmClient = mockLlmClient;
        _bedrockLlmClient = bedrockLlmClient;
    }

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        return _runtimeModeService.UseBedrock
            ? _bedrockLlmClient.CompleteAsync(systemPrompt, userPrompt, ct)
            : _mockLlmClient.CompleteAsync(systemPrompt, userPrompt, ct);
    }
}
