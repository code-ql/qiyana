namespace qiyana.Services;

public static class AgnesAiConfig
{
    public const string ApiKey = "sk-xr17oejrwFj6cjF4YYSXiYrL6efMuEe9Q6geN38G7ah75hs9";
    public const string Endpoint = "https://apihub.agnes-ai.com/v1/chat/completions";
    public const string Model = "agnes-2.0-flash";

    public static bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
