using ECommerce.UserService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UserService.Infrastructure.Persistence
{
    public sealed class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .IsRequired();
                entity.HasIndex(x => x.Email)
                    .IsUnique();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();
            });
        }
    }
}
