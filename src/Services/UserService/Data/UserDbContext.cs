using Microsoft.EntityFrameworkCore;
using LibHub.UserService.Models.Entities;

namespace LibHub.UserService.Data;

public class UserDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public UserDbContext(DbContextOptions<UserDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            entity.Property(e => e.HashedPassword)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });
    }
}

