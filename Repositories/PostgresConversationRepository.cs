
using Insurance.AI.Api.Models;
using Npgsql;
using NpgsqlTypes;

namespace Insurance.AI.Api.Repositories;

public sealed class PostgresConversationRepository
    : IConversationRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresConversationRepository(
        NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<AIConversation?> GetByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                user_id,
                title,
                session_data::text,
                created_at,
                updated_at
            FROM ai_conversations
            WHERE id = @id
              AND user_id = @userId;
            """;

        await using var connection =
            await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", conversationId);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new AIConversation
        {
            Id = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            Title = reader.IsDBNull(2) ? null : reader.GetString(2),
            SessionData = reader.GetString(3),
            CreatedAt = reader.GetDateTime(4),
            UpdatedAt = reader.GetDateTime(5)
        };
    }

    public async Task CreateAsync(
        AIConversation conversation,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO ai_conversations (
                id,
                user_id,
                title,
                session_data
            )
            VALUES (
                @id,
                @userId,
                @title,
                @sessionData
            );
            """;

        await using var connection =
            await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", conversation.Id);
        command.Parameters.AddWithValue("userId", conversation.UserId);

        command.Parameters.AddWithValue(
            "title",
            (object?)conversation.Title ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "sessionData",
            NpgsqlDbType.Jsonb,
            conversation.SessionData);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateSessionAsync(
        Guid conversationId,
        Guid userId,
        string sessionData,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE ai_conversations
            SET session_data = @sessionData,
                updated_at = NOW()
            WHERE id = @id
              AND user_id = @userId;
            """;

        await using var connection =
            await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", conversationId);
        command.Parameters.AddWithValue("userId", userId);

        command.Parameters.AddWithValue(
            "sessionData",
            NpgsqlDbType.Jsonb,
            sessionData);

        var affectedRows =
            await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                "Conversation not found or access denied.");
        }
    }
}
