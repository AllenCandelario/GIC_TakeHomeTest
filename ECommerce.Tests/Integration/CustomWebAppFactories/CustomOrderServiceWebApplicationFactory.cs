using ECommerce.OrderService.Infrastructure.Persistence;
using ECommerce.Shared.Kafka.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Tests.Integration.CustomWebAppFactories
{
    public sealed class CustomOrderServiceWebApplicationFactory : WebApplicationFactory<OrderService.Program>
    {
        private readonly InMemoryDatabaseRoot _dbRoot = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // remove existing registrations
                services.RemoveAll(typeof(DbContextOptions<OrderDbContext>));
                services.RemoveAll(typeof(OrderDbContext));
                services.RemoveAll(typeof(IKafkaProducer));

                services.AddDbContext<OrderDbContext>(options => options.UseInMemoryDatabase("OrdersDb_Test", _dbRoot));
                services.AddSingleton<IKafkaProducer, NoOpKafkaProducer>();
            });
        }
    }
}
