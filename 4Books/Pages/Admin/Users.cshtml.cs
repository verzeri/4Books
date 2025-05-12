using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _4Books.Pages.Data;
using _4Books.Pages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace _4Books.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UsersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.IsInRole("Admin"))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            Users = await _context.Users
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();

            return Page();
        }
    }
}