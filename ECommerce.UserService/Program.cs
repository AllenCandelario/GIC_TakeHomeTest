using ECommerce.Shared.Middleware;
using Serilog;
using ECommerce.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ECommerce.UserService.Application.Interfaces;
using ECommerce.UserService.Application.Services;
using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Options;
using ECommerce.Shared.Kafka.Interfaces;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.AspNetCore.Mvc;


namespace ECommerce.UserService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Serilog
            builder.Host.UseSerilog((context, loggerConfig) =>
            {
                loggerConfig.ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", "UserService")
                .WriteTo.Console();
            });

            // Catch boundary errors 
            builder.Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        // request logger services
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                        logger.LogWarning("Invalid request payload at {Path}", context.HttpContext.Request.Path);

                        var errors = context.ModelState
                            .Where(e => e.Value?.Errors.Count > 0)
                            .SelectMany(kvp => kvp.Value!.Errors.Select(err => new
                            {
                                Field = kvp.Key,
                                Message = err.ErrorMessage
                            }))
                            .Where(e => e.Field != "userDto") // remove this error to avoid confusion
                            .ToList();

                        var problem = new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Invalid request payload",
                            Detail = "The request body is malformed or contains invalid values.",
                            Instance = context.HttpContext.Request.Path
                        };

                        problem.Extensions["errors"] = errors;

                        return new BadRequestObjectResult(problem)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    };
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // DI services
            builder.Services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase("UsersDb"));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, Application.Services.UserService>();

            // Kafka
            builder.Services.AddOptions<KafkaConsumerConfigOptions>()
                .Bind(builder.Configuration.GetSection("Kafka:Consumer"))
                .Validate(o =>
                    !string.IsNullOrWhiteSpace(o.BootstrapServers) &&
                    !string.IsNullOrWhiteSpace(o.GroupId) &&
                    o.Topics is { Length: > 0 } &&
                    o.Topics.All(t => !string.IsNullOrWhiteSpace(t)),
                    "Kafka:Consumer config invalid")
                .ValidateOnStart();

            builder.Services.AddOptions<KafkaProducerConfigOptions>()
                .Bind(builder.Configuration.GetSection("Kafka:Producer"))
                .Validate(o =>
                    !string.IsNullOrWhiteSpace(o.BootstrapServers) &&
                    !string.IsNullOrWhiteSpace(o.ClientId),
                    "Kafka:Producer config invalid")
                .ValidateOnStart();

            // Kafka producer singleton
            builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

            // Kafka consumer hosted service
            builder.Services.AddHostedService<KafkaConsumer>();

            builder.Services.AddKeyedScoped<IKafkaEventHandler, OrderCreatedEventHandler>("order.created.v1");

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("vite", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });


            var app = builder.Build();

            app.UseSerilogRequestLogging();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Use Middleware
            app.UseMiddleware<ExceptionMappingMiddleware>();

            // Use CORS
            app.UseCors("vite");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
