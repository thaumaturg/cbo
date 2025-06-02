using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace Cbo.API;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string connectionString = builder.Configuration.GetConnectionString("CboDb")
            ?? throw new InvalidOperationException("Connection string" + "'CboDb' not found.");

        builder.Services.AddDbContext<CboDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        builder.Services.AddScoped<ITournamentRepository, PostgresTournamentRepository>();
        builder.Services.AddScoped<ISettingsRepository, PostgresSettingsRepository>();
        builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
