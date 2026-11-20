using ImproveYourCv.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImproveYourCv.Controllers;

[Authorize(Roles = Program.AdminRole)]
public class GitHubController(GitHubSync github, IConfiguration config, ILogger<GitHubController> log) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? user, CancellationToken ct)
    {
        // Varsayılan kullanıcı adı profil linkinden alınır
        user ??= config["Profile:GitHubUrl"]?.TrimEnd('/').Split('/').LastOrDefault();
        ViewData["User"] = user;
        if (string.IsNullOrWhiteSpace(user)) return View(new List<RepoInfo>());
        try
        {
            return View(await github.ListAsync(user.Trim(), ct));
        }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "GitHub repoları alınamadı");
            ViewData["Error"] = e.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => $"\"{user}\" adında bir GitHub kullanıcısı bulunamadı.",
                System.Net.HttpStatusCode.Forbidden => "GitHub istek sınırına ulaşıldı (saatte 60). Biraz sonra tekrar dene.",
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
        try
        {
            TempData["Message"] = await github.SyncAsync(user, repos, ct);
        }
        catch (HttpRequestException e)
        {
            log.LogWarning(e, "GitHub içe aktarma başarısız");
            TempData["Message"] = "İçe aktarma başarısız oldu; GitHub'a ulaşılamadı ya da istek sınırı doldu.";
        }
        return RedirectToAction(nameof(Index), new { user });
    }
}
