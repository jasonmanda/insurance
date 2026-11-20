
using Google.GenAI;
using Insurance.AI.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

using OpenAIChatClient = OpenAI.Chat.ChatClient;

namespace Insurance.AI.Api.Agents;

public sealed class InsuranceAgentFactory
{
    private readonly InsuranceTools _tools;
    private readonly IConfiguration _configuration;

    public InsuranceAgentFactory(
        InsuranceTools tools,
        IConfiguration configuration)
    {
        _tools = tools;
        _configuration = configuration;
    }

    public AIAgent CreateAgent()
    {
        var provider = _configuration["AI:Provider"] ?? "Gemini";
        var model = _configuration["AI:Model"]
                    ?? "gemini-2.5-flash";

        IChatClient chatClient = provider.ToLowerInvariant() switch
        {
            "ollama" => CreateOllamaClient(model),

            "gemini" => CreateGeminiClient(model),

            _ => throw new InvalidOperationException(
                $"Unsupported AI provider: {provider}")
        };

        return new ChatClientAgent(
            chatClient,
            instructions: """
                Tu es un assistant intelligent spécialisé en assurance.

                Tes responsabilités :
                - Aider les clients à comprendre leurs contrats.
                - Consulter leurs contrats avec les outils disponibles.
                - Expliquer les garanties et les échéances.
                - Fournir des réponses claires et professionnelles.

                Règles de sécurité :
                - Ne jamais inventer des informations.
                - Utiliser les outils pour consulter les données contractuelles.
                - Ne consulter que les données de l'utilisateur authentifié.
                - Ne jamais révéler les données d'un autre utilisateur.
                """,
            name: "InsuranceAssistant",
            tools:
            [
                AIFunctionFactory.Create(
                    _tools.GetMyActiveContractsAsync),

                AIFunctionFactory.Create(
                    _tools.GetNextRenewalAmountAsync)
            ]);
    }

    private IChatClient CreateOllamaClient(string model)
    {
        var baseUrl = _configuration["AI:BaseUrl"]
            ?? "http://localhost:11434/v1";

        var apiKey = _configuration["AI:ApiKey"] ?? "ollama";

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(baseUrl)
        };

        var client = new OpenAIChatClient(
            model,
            new ApiKeyCredential(apiKey),
            options);

        return client.AsIChatClient();
    }

    private IChatClient CreateGeminiClient(string model)
    {
        var apiKey = Environment.GetEnvironmentVariable(
            "GOOGLE_GENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GOOGLE_GENAI_API_KEY is not configured.");
        }

        var geminiClient = new Client(apiKey: apiKey);

        return geminiClient.AsIChatClient(model);
    }
}
