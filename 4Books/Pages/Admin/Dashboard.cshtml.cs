using System;
using System.Linq;
using System.Threading.Tasks;
using _4Books.Pages.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace _4Books.Pages.Admin
{
    [Authorize(Roles = "Admin")] // Cambiato da "Medico" a "Admin"
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int ActiveUsersLastWeek { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verifica se l'utente è un admin
            if (!User.IsInRole("Admin")) // Cambiato da "Medico" a "Admin"
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            // Ottieni le statistiche
            TotalUsers = await _context.Users.CountAsync();

            var today = DateTime.UtcNow.Date;
            NewUsersToday = await _context.Users
                .Where(u => u.CreatedDate.Date == today)
                .CountAsync();

            var weekAgo = DateTime.UtcNow.AddDays(-7);
            ActiveUsersLastWeek = await _context.Users
                .Where(u => u.LastLoginDate.HasValue && u.LastLoginDate.Value >= weekAgo)
                .CountAsync();

            return Page();
        }
    }
}