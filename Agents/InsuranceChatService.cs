
using System.Text.Json;
using Google.GenAI;
using Insurance.AI.Api.Models;
using Insurance.AI.Api.Repositories;
using Insurance.AI.Api.Security;
using Microsoft.Agents.AI;
using Insurance.AI.Api.Contracts;
using System.Runtime.CompilerServices;

namespace Insurance.AI.Api.Agents;

public sealed class InsuranceChatService
{
    private readonly InsuranceAgentFactory _agentFactory;
    private readonly IConversationRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<InsuranceChatService> _logger;

    private const int MaxAttempts = 4;

    public InsuranceChatService(
        InsuranceAgentFactory agentFactory,
        IConversationRepository repository,
        ICurrentUser currentUser,
        ILogger<InsuranceChatService> logger)
    {
        _agentFactory = agentFactory;
        _repository = repository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(
        string message,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var agent = _agentFactory.CreateAgent();

        AgentSession session;
        var isNewConversation = !conversationId.HasValue;

        if (isNewConversation)
        {
            session = await agent.CreateSessionAsync(
                cancellationToken);
        }
        else
        {
            var conversation = await _repository.GetByIdAsync(
                conversationId!.Value,
                userId,
                cancellationToken);

            if (conversation is null)
            {
                throw new KeyNotFoundException(
                    "Conversation not found.");
            }

            using var document = JsonDocument.Parse(
                conversation.SessionData);

            session = await agent.DeserializeSessionAsync(
                document.RootElement.Clone(),
                cancellationToken: cancellationToken);
        }

        var response = await RunWithRetryAsync(
            agent,
            message,
            session,
            cancellationToken);

        var serializedSession = await agent.SerializeSessionAsync(
            session,
            cancellationToken: cancellationToken);

        var sessionData = serializedSession.GetRawText();

        if (isNewConversation)
        {
            var newConversation = new AIConversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = message.Length > 80
                    ? message[..80]
                    : message,
                SessionData = sessionData
            };

            await _repository.CreateAsync(
                newConversation,
                cancellationToken);

            conversationId = newConversation.Id;
        }
        else
        {
            await _repository.UpdateSessionAsync(
                conversationId!.Value,
                userId,
                sessionData,
                cancellationToken);
        }

        return new Contracts.ChatResponse(
            conversationId!.Value,
            response.Text);
    }

    private async Task<AgentResponse> RunWithRetryAsync(
        AIAgent agent,
        string message,
        AgentSession session,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await agent.RunAsync(
                    message,
                    session,
                    cancellationToken: cancellationToken);
            }
            catch (ServerError ex)
                when (ex.StatusCode is 500 or 502 or 503 or 504)
            {
                if (attempt == MaxAttempts)
                    throw;

                var delay = GetRetryDelay(attempt);

                _logger.LogWarning(
                    ex,
                    "Gemini server error. Attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    MaxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
            catch (ClientError ex) when (ex.StatusCode == 429)
            {
                if (attempt == MaxAttempts)
                    throw;

                var delay = GetRetryDelay(attempt);

                _logger.LogWarning(
                    ex,
                    "Gemini rate limit. Attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    MaxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Gemini could not process the request.");
    }

    private static TimeSpan GetRetryDelay(int attempt)
    {
        var seconds = Math.Pow(2, attempt - 1);
        var jitter = Random.Shared.NextDouble() * 0.5;

        return TimeSpan.FromSeconds(seconds + jitter);
    }


    public async IAsyncEnumerable<ChatStreamEvent> StreamMessageAsync(
        string message,
        Guid? conversationId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var agent = _agentFactory.CreateAgent();

        AgentSession session;
        var isNewConversation = !conversationId.HasValue;

        AIConversation? conversation = null;

        if (isNewConversation)
        {
            session = await agent.CreateSessionAsync(cancellationToken);

            var initialSession = await agent.SerializeSessionAsync(
                session,
                cancellationToken: cancellationToken);

            conversation = new AIConversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = message.Length > 80 ? message[..80] : message,
                SessionData = initialSession.GetRawText()
            };

            await _repository.CreateAsync(conversation, cancellationToken);

            conversationId = conversation.Id;
        }
        else
        {
            conversation = await _repository.GetByIdAsync(
                conversationId!.Value,
                userId,
                cancellationToken);

            if (conversation is null)
                throw new KeyNotFoundException("Conversation not found.");

            using var document = JsonDocument.Parse(conversation.SessionData);

            session = await agent.DeserializeSessionAsync(
                document.RootElement.Clone(),
                cancellationToken: cancellationToken);
        }

        yield return new ChatStreamEvent(
            "start",
            conversationId);

        await foreach (var update in agent.RunStreamingAsync(
            message,
            session,
            cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return new ChatStreamEvent(
                    "chunk",
                    Content: update.Text);
            }
        }

        var serializedSession = await agent.SerializeSessionAsync(
            session,
            cancellationToken: cancellationToken);

        await _repository.UpdateSessionAsync(
            conversationId!.Value,
            userId,
            serializedSession.GetRawText(),
            cancellationToken);

        yield return new ChatStreamEvent(
            "completed",
            conversationId);
    }

}
