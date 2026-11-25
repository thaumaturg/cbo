using Cbo.Core.Accounts;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Data.Interceptors;
using Cbo.Core.Matches;
using Cbo.Core.Topics;
using Cbo.Core.Tournaments;
using Cbo.Core.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the database, the authorization handlers, the services, every command and query handler and every
    /// request validator, and applies the FluentValidation options.
    /// The host provides <see cref="ICurrentUserService"/>, <see cref="IJwtTokenService"/> and ASP.NET Core Identity.
    /// </summary>
    public static IServiceCollection AddCore(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AuditSaveChangesInterceptor>();

        services.AddDbContext<CboDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IAuthorizationHandler, TopicAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, TournamentAuthorizationHandler>();

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITournamentAccessService, TournamentAccessService>();
        services.AddScoped<ITopicAccessService, TopicAccessService>();
        services.AddScoped<IMatchGenerationService, MatchGenerationService>();
        services.AddScoped<IMatchScoringService, MatchScoringService>();

        services.AddCqrsHandlers(typeof(ServiceCollectionExtensions).Assembly);

        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        ValidationSetup.ConfigureGlobalOptions();

        return services;
    }
}
