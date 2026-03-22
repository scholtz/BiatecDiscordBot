using System.Text.Json.Serialization;
using BiatecDiscordBot.Data;
using BiatecDiscordBot.Services;
using Microsoft.EntityFrameworkCore;

namespace BiatecDiscordBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Entity Framework
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString) ||
                builder.Environment.IsDevelopment() &&
                builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                builder.Services.AddDbContext<BotDbContext>(options =>
                    options.UseInMemoryDatabase("BiatecDiscordBot"));
            }
            else
            {
                builder.Services.AddDbContext<BotDbContext>(options =>
                    options.UseNpgsql(connectionString));
            }

            // Configure Discord bot settings
            builder.Services.Configure<DiscordBotSettings>(
                builder.Configuration.GetSection("Discord"));

            // Register services
            builder.Services.AddSingleton<IDiscordBotService, DiscordBotService>();
            builder.Services.AddScoped<IMessageTrackingService, MessageTrackingService>();
            builder.Services.AddScoped<IQuestionService, QuestionService>();
            builder.Services.AddHostedService<DiscordBotHostedService>();

            // Configure controllers with JSON options to handle EF circular references
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Configure OpenAPI / Swagger
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Ensure database is created
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                db.Database.EnsureCreated();
            }

            // Enable Swagger in all environments for API exploration
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Biatec Discord Bot API v1");
                options.RoutePrefix = string.Empty; // Serve Swagger UI at root
            });

            app.MapOpenApi();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
