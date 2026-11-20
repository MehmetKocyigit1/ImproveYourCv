using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DevRota.Data;
using DevRota.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DevRota.Controllers;

public class LoginForm
{
    [Required(ErrorMessage = "E-posta gerekli."), EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi gir.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Şifre gerekli."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

public class RegisterForm
{
    [Required(ErrorMessage = "E-posta gerekli."), EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi gir."), StringLength(256)]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Şifre gerekli."), DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalı.")]
    public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = "";
}

[AutoValidateAntiforgeryToken]
public class AccountController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, AppDbContext db, IConfiguration config) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginForm());

    [HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginForm form, string? returnUrl)
    {
        if (!ModelState.IsValid) return View(form);
        var result = await signIn.PasswordSignInAsync(form.Email, form.Password, form.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        ModelState.AddModelError("", result.IsLockedOut
            ? "Çok fazla hatalı deneme yapıldı; hesap birkaç dakikalığına kilitlendi."
            : "E-posta ya da şifre hatalı.");
        return View(form);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterForm());

    [HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var user = new IdentityUser { UserName = form.Email, Email = form.Email };
        var result = await users.CreateAsync(user, form.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(form);
        }
        // Site sahibinin e-postasıyla açılan hesap yönetici olur
        if (string.Equals(form.Email, config["Admin:Email"], StringComparison.OrdinalIgnoreCase))
            await users.AddToRoleAsync(user, Program.AdminRole);
        await signIn.SignInAsync(user, isPersistent: false);
        TempData["Message"] = "Hesabın oluşturuldu. Doldurduğun anketler artık hesabında saklanacak.";
        return RedirectToAction(nameof(Me));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return Redirect("/");
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> Me()
    {
        var id = users.GetUserId(User);
        var responses = await db.SurveyResponses.AsNoTracking().Where(r => r.UserId == id)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        var roadmaps = responses
            .Select(r => (r.PublicId, r.CreatedAt, r.RoadmapJson is null ? null : JsonSerializer.Deserialize<Roadmap>(r.RoadmapJson, SurveyResponse.Json)))
            .ToList();
        ViewData["Email"] = User.Identity?.Name;
        return View(roadmaps);
    }

    // Kişisel veriler için: hesap silinir, yol haritaları anonimleşir
    [HttpPost, Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        if (await users.GetUserAsync(User) is { } user)
        {
            await signIn.SignOutAsync();
            await users.DeleteAsync(user);
        }
        TempData["Message"] = "Hesabın silindi.";
        return Redirect("/");
    }

    [HttpGet]
    public IActionResult Denied() => View();
}
