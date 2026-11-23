using System.Security.Claims;
using ImproveYourCv.Data;
using ImproveYourCv.Models;
using ImproveYourCv.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv.Controllers;

// Giriş yapan kullanıcı kendi GitHub repolarını profiline aktarır
[Authorize]
public class GitHubController(GitHubSync github, AppDbContext db, ILogger<GitHubController> log) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(string? user, CancellationToken ct)
    {
        var uid = Uid;
        // Varsayılan kullanıcı adı profildeki GitHub linkinden alınır
        user ??= (await db.Profiles.AsNoTracking().Where(p => p.UserId == uid).Select(p => p.GitHubUrl).FirstOrDefaultAsync(ct))
            ?.TrimEnd('/').Split('/').LastOrDefault();
        ViewData["User"] = user;
        if (string.IsNullOrWhiteSpace(user)) return View(new List<RepoInfo>());
        try
        {
            return View(await github.ListAsync(user.Trim(), uid, ct));
        }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "GitHub repoları alınamadı");
            ViewData["Error"] = e.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => $"\"{user}\" adında bir GitHub kullanıcısı bulunamadı.",
                System.Net.HttpStatusCode.Forbidden => "GitHub istek sınırına ulaşıldı. Biraz sonra tekrar dene.",
                _ => "GitHub'a ulaşılamadı. İnternet bağlantını kontrol et.",
            };
            return View(new List<RepoInfo>());
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(string user, List<string> repos, CancellationToken ct)
    {
        if (repos.Count == 0)
        {
            TempData["Message"] = "Hiç repo seçilmedi.";
            return RedirectToAction(nameof(Index), new { user });
        }
        var uid = Uid;
        try
        {
            TempData["Message"] = await github.SyncAsync(user, uid, repos, ct);
            // Profilde GitHub linki yoksa içe aktarılan hesabın linki eklenir
            var profile = await db.Profiles.FindAsync([uid], ct);
            if (profile is null) db.Profiles.Add(profile = new UserProfile { UserId = uid });
            profile.GitHubUrl ??= $"https://github.com/{Uri.EscapeDataString(user)}";
            await db.SaveChangesAsync(ct);
        }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "GitHub içe aktarma başarısız");
            TempData["Message"] = "İçe aktarma başarısız oldu; GitHub'a ulaşılamadı ya da istek sınırı doldu.";
        }
        return RedirectToAction(nameof(Index), new { user });
    }
}
