
using ECommerce.OrderService.Application.Interfaces;
using ECommerce.OrderService.Application.Services;
using ECommerce.OrderService.Infrastructure.Persistence;
using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Interfaces;
using ECommerce.Shared.Kafka.Options;
using ECommerce.Shared.Kafka.Producer;
using ECommerce.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ECommerce.OrderService
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
                .Enrich.WithProperty("Service", "OrderService")
                .WriteTo.Console();
            });

            // Controller endponts + swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // DI services
            builder.Services.AddDbContext<OrderDbContext>(options => options.UseInMemoryDatabase("OrdersDb"));
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IOrderService, Application.Services.OrderService>();

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

            builder.Services.AddKeyedScoped<IKafkaEventHandler, UserCreatedEventHandler>("user.created.v1");

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
