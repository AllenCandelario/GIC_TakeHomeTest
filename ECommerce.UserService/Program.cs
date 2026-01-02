using ECommerce.Shared.Middleware;
using Serilog;
using ECommerce.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ECommerce.UserService.Application.Interfaces;
using ECommerce.UserService.Application.Services;


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

            // Controller endponts + swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // DI services
            builder.Services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase("UsersDb"));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserQueryHandler, UserQueryHandler>();

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
