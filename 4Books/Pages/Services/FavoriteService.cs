using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _4Books.Pages.Data;
using _4Books.Pages.Models;
using Microsoft.EntityFrameworkCore;

namespace _4Books.Services
{
    public class FavoriteService
    {
        private readonly ApplicationDbContext _context;

        public FavoriteService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Ottieni tutti i libri preferiti di un utente
        public async Task<List<Book>> GetUserFavoritesAsync(int userId)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .Select(f => f.Book)
                .ToListAsync();
        }

        // Aggiungi un libro ai preferiti
        // Aggiorna il metodo AddToFavoritesAsync
        public async Task<bool> AddToFavoritesAsync(int userId, string bookId, Book bookDetails = null)
        {
            // Verifica se il libro è già nei preferiti
            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);

            if (existing != null)
            {
                return false; // Il libro è già nei preferiti
            }

            // Verifica se il libro esiste già nel database
            var bookInDb = await _context.Books.FindAsync(bookId);

            // Se il libro non esiste nel database e abbiamo i dettagli, lo aggiungiamo
            if (bookInDb == null && bookDetails != null)
            {
                // Assicurati che tutti i campi NOT NULL abbiano valori validi
                bookDetails.Description = bookDetails.Description ?? "Nessuna descrizione disponibile";
                bookDetails.Title = bookDetails.Title ?? "Titolo non disponibile";
                bookDetails.Publisher = bookDetails.Publisher ?? "Editore non disponibile";
                bookDetails.PublishedDate = bookDetails.PublishedDate ?? "Data non disponibile";
                bookDetails.Language = bookDetails.Language ?? "non specificato";
                bookDetails.ThumbnailUrl = bookDetails.ThumbnailUrl ?? "";
                bookDetails.InfoLink = bookDetails.InfoLink ?? "";

                // Assicurati che le liste non siano null
                bookDetails.Authors = bookDetails.Authors ?? new List<string>();
                bookDetails.Categories = bookDetails.Categories ?? new List<string>();

                _context.Books.Add(bookDetails);
                await _context.SaveChangesAsync();
            }
            // Se il libro non esiste e non abbiamo i dettagli, non possiamo procedere
            else if (bookInDb == null)
            {
                throw new InvalidOperationException("Il libro non esiste nel database e non sono stati forniti i dettagli.");
            }

            // Ora possiamo aggiungere il preferito
            var favorite = new Favorite
            {
                UserId = userId,
                BookId = bookId,
                AddedDate = DateTime.UtcNow
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            return true;
        }
        // Rimuovi un libro dai preferiti
        public async Task<bool> RemoveFromFavoritesAsync(int userId, string bookId)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);

            if (favorite == null)
            {
                return false; // Il libro non è nei preferiti
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
            return true;
        }

        // Verifica se un libro è nei preferiti
        public async Task<bool> IsInFavoritesAsync(int userId, string bookId)
        {
            return await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.BookId == bookId);
        }
    }
}