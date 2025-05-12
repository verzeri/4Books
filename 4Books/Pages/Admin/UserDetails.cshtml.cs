using System.Threading.Tasks;
using _4Books.Pages.Data;
using _4Books.Pages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace _4Books.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UserDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User UserDetails { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            UserDetails = await _context.Users.FindAsync(id);

            if (UserDetails == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Verifica che l'admin non stia eliminando se stesso
            if (User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value == user.Email)
            {
                StatusMessage = "Errore: Non puoi eliminare il tuo account.";
                return RedirectToPage("/Admin/Users");
            }

            // Verifica che l'admin non stia eliminando un altro admin
            if (user.Role == "Admin" && user.Email != User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value)
            {
                StatusMessage = "Errore: Non puoi eliminare un altro amministratore.";
                return RedirectToPage("/Admin/Users");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            StatusMessage = $"L'utente {user.FullName} è stato eliminato con successo.";
            return RedirectToPage("/Admin/Users");
        }
    }
}