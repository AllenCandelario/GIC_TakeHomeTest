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
            /* Pre-build order, generally doesn't matter
             * 1. Create builder
             * 2. Logging
             * 3. Configuration binding esp if using options pattern
             * 4. Framework services: AddControllers, Swagger, CORS, AuthN/AuthZ, Health checks etc.
             * 5. App services: repository, business logic etc.
             * 6. Infra services: DbContext, Kafka Producer, external clients etc.
             * 7. Hosted/BG services
             */

            #region 1. Create builder. Eager loads config (appsettings, env vars etc.)

            var builder = WebApplication.CreateBuilder(args);

            #endregion

            #region 2. Logging. Use Serilog with enrichment
            
            builder.Host.UseSerilog((context, loggerConfig) =>
            {
                loggerConfig.ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", "UserService")
                .WriteTo.Console();
            });

            #endregion

            #region 3. Configuration binding
            
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

            #endregion

            #region 4. Framework services

            // AddControllers + additional boundary configuration to match exception handling errors
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

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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

            #endregion

            #region 5. App services

            // Repository
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            
            // Business logic
            builder.Services.AddScoped<IUserService, Application.Services.UserService>();
            builder.Services.AddKeyedScoped<IKafkaEventHandler, OrderCreatedEventHandler>("order.created.v1"); // key to match kafka topic

            #endregion

            #region 6. Infra services

            // DbContext
            builder.Services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase("UsersDb"));

            // Kafka producer 
            builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

            #endregion

            #region 7. Hosted / BG services

            // Kafka consumer hosted service
            builder.Services.AddHostedService<KafkaConsumer>();

            #endregion


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
