using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _4Books.Pages.Models;
using _4Books.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _4Books.Pages.Users
{
    [Authorize] // Solo utenti autenticati possono accedere
    public class FavoritesModel : PageModel
    {
        private readonly FavoriteService _favoriteService;

        public FavoritesModel(FavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        public List<_4Books.Pages.Models.Book> FavoriteBooks { get; set; } = new List<_4Books.Pages.Models.Book>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            FavoriteBooks = await _favoriteService.GetUserFavoritesAsync(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveFavoriteAsync(string bookId)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            await _favoriteService.RemoveFromFavoritesAsync(userId, bookId);
            return RedirectToPage();
        }
    }
}
