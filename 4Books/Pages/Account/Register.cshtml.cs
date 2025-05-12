using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using _4Books.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace _4Books.Pages.Account
{
    [AllowAnonymous]//accesso senza autenticazione
    public class RegisterModel : PageModel
    {
        private readonly UserService _userService;

        public RegisterModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string Message { get; set; }
        public bool IsSuccess { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Il nome è obbligatorio")]
            [Display(Name = "Nome")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Il cognome è obbligatorio")]
            [Display(Name = "Cognome")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "L'email è obbligatoria")]
            [EmailAddress(ErrorMessage = "Formato email non valido")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La password è obbligatoria")]
            [StringLength(100, ErrorMessage = "La {0} deve essere almeno di {2} caratteri e massimo di {1} caratteri.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Conferma password")]
            [Compare("Password", ErrorMessage = "Le password non corrispondono.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet()
        {
            // Metodo vuoto per visualizzare la pagina iniziale
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _userService.RegisterAsync(
                Input.FirstName,
                Input.LastName,
                Input.Email,
                Input.Password);

            if (result.Success)
            {
                // Effettua automaticamente il login dopo la registrazione
                var user = await _userService.GetUserByEmailAsync(Input.Email);

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Input.FirstName),
            new Claim(ClaimTypes.Surname, Input.LastName),
            new Claim(ClaimTypes.Email, Input.Email),
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role) // Aggiunto claim per il ruolo
        };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // Reindirizza in base al ruolo
                if (user.Role == "Admin")
                {
                    return RedirectToPage("/Admin/Dashboard");
                }

                // Altrimenti, reindirizza alla pagina di ricerca standard
                return RedirectToPage("/Book/Index");
            }
            else
            {
                IsSuccess = false;
                Message = result.Message;
            }

            return Page();
        }
    }
}