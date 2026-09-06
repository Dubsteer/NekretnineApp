using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NekretnineApp.Models;

namespace NekretnineApp.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "Izaberite vrstu naloga.")]
            [Display(Name = "Vrsta naloga")]
            public string AccountType { get; set; } = RoleNames.User;

            [Required(ErrorMessage = "Ime i prezime ili naziv su obavezni.")]
            [StringLength(100)]
            [Display(Name = "Ime i prezime / naziv")]
            public string FullName { get; set; } = string.Empty;

            [StringLength(150)]
            [Display(Name = "Naziv firme (za oglašivača)")]
            public string? CompanyName { get; set; }

            [Required(ErrorMessage = "Broj telefona je obavezan.")]
            [Phone(ErrorMessage = "Unesite ispravan broj telefona.")]
            [Display(Name = "Telefon")]
            public string PhoneNumber { get; set; } = string.Empty;

            [StringLength(200)]
            [Display(Name = "Adresa")]
            public string? Address { get; set; }

            [Required(ErrorMessage = "Email adresa je obavezna.")]
            [EmailAddress(ErrorMessage = "Unesite ispravnu email adresu.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Lozinka je obavezna.")]
            [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje {2} karaktera.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Lozinka")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Potvrda lozinke")]
            [Compare(nameof(Password), ErrorMessage = "Lozinke se ne podudaraju.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            if (!RoleNames.PublicRoles.Contains(Input.AccountType))
                ModelState.AddModelError(nameof(Input.AccountType), "Izaberite ispravnu vrstu naloga.");

            if (!ModelState.IsValid)
                return Page();

            var user = new ApplicationUser
            {
                UserName = Input.Email.Trim(),
                Email = Input.Email.Trim(),
                EmailConfirmed = true,
                FullName = Input.FullName.Trim(),
                CompanyName = string.IsNullOrWhiteSpace(Input.CompanyName) ? null : Input.CompanyName.Trim(),
                PhoneNumber = Input.PhoneNumber.Trim(),
                Address = string.IsNullOrWhiteSpace(Input.Address) ? null : Input.Address.Trim(),
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, Input.AccountType);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError(string.Empty, "Nalog nije moguće kreirati. Pokušajte ponovo.");
                    return Page();
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, TranslateIdentityError(error.Code, error.Description));

            return Page();
        }

        private static string TranslateIdentityError(string code, string description)
        {
            return code switch
            {
                "DuplicateEmail" or "DuplicateUserName" => "Korisnik sa ovom email adresom već postoji.",
                "PasswordTooShort" => "Lozinka je prekratka.",
                "PasswordRequiresDigit" => "Lozinka mora sadržati broj.",
                "PasswordRequiresUpper" => "Lozinka mora sadržati veliko slovo.",
                "PasswordRequiresLower" => "Lozinka mora sadržati malo slovo.",
                _ => description
            };
        }
    }
}
