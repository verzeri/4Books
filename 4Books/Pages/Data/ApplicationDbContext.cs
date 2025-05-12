using _4Books.Pages.Models;
using Microsoft.EntityFrameworkCore;

namespace _4Books.Pages.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet esistenti
        public DbSet<User> Users { get; set; }
        public DbSet<_4Books.Pages.Models.Book> Books { get; set; }
        // ... altri DbSet ...

        // Nuovo DbSet per i preferiti
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configura la conversione per gli Authors come stringa separata da virgole
            modelBuilder.Entity<_4Books.Pages.Models.Book>()
                .Property(b => b.Authors)
                .HasConversion(
                    list => list != null ? string.Join(',', list) : null,
                    str => str != null ? str.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>()
                );

            // Configura la conversione per le Categories come stringa separata da virgole
            modelBuilder.Entity<_4Books.Pages.Models.Book>()
                .Property(b => b.Categories)
                .HasConversion(
                    list => list != null ? string.Join(',', list) : null,
                    str => str != null ? str.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>()
                );

            // Configura la relazione per i preferiti
            modelBuilder.Entity<_4Books.Pages.Models.Favorite>()
                .HasKey(f => f.Id);

            modelBuilder.Entity<_4Books.Pages.Models.Favorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<_4Books.Pages.Models.Favorite>()
                .HasOne(f => f.Book)
                .WithMany()
                .HasForeignKey(f => f.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Aggiungi un indice unico per evitare duplicati nei preferiti
            modelBuilder.Entity<_4Books.Pages.Models.Favorite>()
                .HasIndex(f => new { f.UserId, f.BookId })
                .IsUnique();
        }
    }
}