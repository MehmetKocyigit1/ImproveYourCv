using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ImproveYourCv.Data;
using ImproveYourCv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv.Controllers;

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
        // Yönetici: Admin:Email ile kayıt olan; ayar yoksa siteye ilk kayıt olan hesap
        // shortcut: siteyi yayına almadan önce Admin__Email ayarla ya da kendin kayıt ol, yoksa ilk ziyaretçi yönetici olur
        var adminEmail = config["Admin:Email"];
        var isAdmin = string.IsNullOrWhiteSpace(adminEmail)
            ? (await users.GetUsersInRoleAsync(Program.AdminRole)).Count == 0
            : string.Equals(form.Email, adminEmail, StringComparison.OrdinalIgnoreCase);
        if (isAdmin)
        {
            await users.AddToRoleAsync(user, Program.AdminRole);
            await db.ClaimLegacyDataAsync(user.Id); // eski tek kullanıcılı sürümün verileri
        }
        await signIn.SignInAsync(user, isPersistent: false);
        TempData["Message"] = "Hesabın oluşturuldu. Profilini doldurarak başla; ne kadar çok bilgi eklersen CV'n o kadar güçlü olur.";
        return RedirectToAction("Index", "Profile");
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
