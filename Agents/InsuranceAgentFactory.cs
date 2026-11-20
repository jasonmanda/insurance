
using Google.GenAI;
using Insurance.AI.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

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
        var apiKey = Environment.GetEnvironmentVariable(
            "GOOGLE_GENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GOOGLE_GENAI_API_KEY is not configured.");
        }

        var model = _configuration["AI:Model"]
                    ?? "gemini-2.5-flash";

        // Initialize Google Gemini
        var geminiClient = new Client(apiKey: apiKey);

        // Adapt Gemini to Microsoft's IChatClient
        IChatClient chatClient =
            geminiClient.AsIChatClient(model);

        // Create the Microsoft Agent Framework agent
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
}
