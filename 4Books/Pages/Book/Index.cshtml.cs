using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _4Books.Pages.Book
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Controlla se l'utente è autenticato
            if (!User.Identity.IsAuthenticated)
            {
                // Reindirizza alla pagina di registrazione
                return RedirectToPage("/Account/Register");
            }

            return Page();
        }
    }
}