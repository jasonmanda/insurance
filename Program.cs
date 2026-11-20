
using Insurance.AI.Api.Agents;
using Insurance.AI.Api.Contracts;
using Insurance.AI.Api.Security;
using Insurance.AI.Api.Services;
using Npgsql;
using Insurance.AI.Api.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

var keycloakAuthority = builder.Configuration["Keycloak:Authority"]
    ?? throw new InvalidOperationException(
        "Keycloak Authority is not configured.");

var keycloakAudience = builder.Configuration["Keycloak:Audience"]
    ?? throw new InvalidOperationException(
        "Keycloak Audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.RequireHttpsMetadata = false; // Local development only
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakAuthority,

            ValidateAudience = true,
            ValidAudience = keycloakAudience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IContractService, ContractService>();

builder.Services.AddScoped<InsuranceTools>();
builder.Services.AddScoped<InsuranceAgentFactory>();
builder.Services.AddScoped<InsuranceChatService>();


var connectionString =
    builder.Configuration.GetConnectionString("InsuranceDb")
    ?? throw new InvalidOperationException(
        "InsuranceDb connection string is missing.");

builder.Services.AddSingleton(
    NpgsqlDataSource.Create(connectionString));

builder.Services.AddScoped<
    IConversationRepository,
    PostgresConversationRepository>();
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var dataSource = scope.ServiceProvider
        .GetRequiredService<NpgsqlDataSource>();

    await using var connection =
        await dataSource.OpenConnectionAsync();

    Console.WriteLine(
        $"PostgreSQL connected: {connection.PostgreSqlVersion}");
}
app.UseAuthentication();
app.UseAuthorization();


app.MapPost("/api/assistant/chat", async (
    ChatRequest request,
    InsuranceChatService chatService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new
        {
            error = "Le message est obligatoire."
        });
    }

    try
    {
        var response = await chatService.SendMessageAsync(
            request.Message,
            request.ConversationId,
            cancellationToken);

        return Results.Ok(response);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new
        {
            error = "Conversation introuvable."
        });
    }
    catch (OperationCanceledException)
        when (cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Insurance AI request failed.");

        return Results.Problem(
            title: "Service IA temporairement indisponible",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.RequireAuthorization();

app.Run();

