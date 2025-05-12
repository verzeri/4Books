using System;
using System.Threading.Tasks;
using _4Books.Pages.Data;
using _4Books.Pages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations; // Aggiungi questo namespace

namespace _4Books.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserEditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserEditModel(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public User UserEdit { get; set; }

        [BindProperty]
        // Aggiungiamo [Required(false)] per indicare che questo campo non è obbligatorio
        [Required(AllowEmptyStrings = true)]
        public string NewPassword { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Carica l'utente dal database e imposta UserEdit
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            UserEdit = user;
            NewPassword = string.Empty;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Rimuovi manualmente eventuali errori di validazione relativi a NewPassword
            if (ModelState.ContainsKey("NewPassword"))
            {
                ModelState.Remove("NewPassword");
            }

            if (!User.IsInRole("Admin"))
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            if (!ModelState.IsValid)
            {
                // Aggiungiamo debug per vedere gli errori di validazione
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        // In produzione questi dovrebbero essere loggati
                        System.Diagnostics.Debug.WriteLine($"Errore di validazione: {error.ErrorMessage}");
                    }
                }
                return Page();
            }

            var userToUpdate = await _context.Users.FindAsync(UserEdit.Id);

            if (userToUpdate == null)
            {
                return NotFound();
            }

            // Verifica che l'admin non stia cambiando il ruolo di un altro admin
            if (userToUpdate.Role == "Admin" &&
                UserEdit.Role != "Admin" &&
                userToUpdate.Email != User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value)
            {
                StatusMessage = "Errore: Non puoi modificare il ruolo di un altro amministratore.";
                return RedirectToPage("/Admin/UserDetails", new { id = UserEdit.Id });
            }

            // Verifica che l'email non sia già usata da un altro utente
            if (userToUpdate.Email != UserEdit.Email)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == UserEdit.Email && u.Id != UserEdit.Id);

                if (existingUser != null)
                {
                    ModelState.AddModelError("UserEdit.Email", "Questa email è già in uso da un altro utente.");
                    return Page();
                }
            }

            // Aggiorna le proprietà manualmente
            userToUpdate.FirstName = UserEdit.FirstName;
            userToUpdate.LastName = UserEdit.LastName;
            userToUpdate.Email = UserEdit.Email;
            userToUpdate.Role = UserEdit.Role;

            // Se è stata fornita una nuova password, aggiornala
            if (!string.IsNullOrEmpty(NewPassword))
            {
                userToUpdate.PasswordHash = _passwordHasher.HashPassword(userToUpdate, NewPassword);
            }

            // Imposta esplicitamente lo stato dell'entità su Modified
            _context.Entry(userToUpdate).State = EntityState.Modified;

            try
            {
                // Salvare le modifiche e verificare quante righe sono state modificate
                var changedRows = await _context.SaveChangesAsync();

                if (changedRows > 0)
                {
                    StatusMessage = "Le informazioni dell'utente sono state aggiornate con successo.";
                }
                else
                {
                    StatusMessage = "Attenzione: Nessuna modifica salvata. Verifica i dati inseriti.";
                }
            }
            catch (Exception ex)
            {
                // In produzione, registra l'eccezione
                StatusMessage = $"Errore durante il salvataggio: {ex.Message}";
                return Page();
            }

            return RedirectToPage("/Admin/UserDetails", new { id = UserEdit.Id });
        }
    }
}