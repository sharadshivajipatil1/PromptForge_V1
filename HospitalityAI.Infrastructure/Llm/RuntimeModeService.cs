namespace HospitalityAI.Infrastructure.Llm;

public class RuntimeModeService
{
    public bool UseBedrock { get; set; }

    public string BedrockModelId { get; set; } = "anthropic.claude-3-haiku-20240307-v1:0";

    public void SetUseBedrock(bool value) => UseBedrock = value;

    public bool ToggleUseBedrock() => UseBedrock = !UseBedrock;
}
