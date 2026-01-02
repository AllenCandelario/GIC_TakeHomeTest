using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using ECommerce.UserService.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration
{
    public sealed  class CustomUserServiceWebApplicationFactory : WebApplicationFactory<UserService.Program>
    {
        private readonly InMemoryDatabaseRoot _dbRoot = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // remove existing registrations
                services.RemoveAll(typeof(DbContextOptions<UserDbContext>));
                services.RemoveAll(typeof(UserDbContext));

                // add db context using shared root
                services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase("UsersDb_Test", _dbRoot));
            });
        }
    }
}
