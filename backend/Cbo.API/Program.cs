using Cbo.API.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace Cbo.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("CboDb")
                ?? throw new InvalidOperationException("Connection string" + "'CboDb' not found.");

            builder.Services.AddDbContext<CboDbContext>(options =>
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

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
}
