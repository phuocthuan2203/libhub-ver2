using Microsoft.EntityFrameworkCore;
using LibHub.CatalogService.Models.Entities;

namespace LibHub.CatalogService.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Book> Books { get; set; }

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId);
            
            entity.Property(e => e.Isbn)
                .IsRequired()
                .HasMaxLength(13);
            
            entity.HasIndex(e => e.Isbn)
                .IsUnique();
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Author)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Genre)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Description)
                .HasColumnType("TEXT");
            
            entity.Property(e => e.TotalCopies)
                .IsRequired();
            
            entity.Property(e => e.AvailableCopies)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });
    }
}

