using Microsoft.EntityFrameworkCore;

namespace Roomr.Auth;

internal class AuthDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
}