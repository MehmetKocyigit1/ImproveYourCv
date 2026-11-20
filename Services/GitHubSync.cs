using System.Text.Json.Serialization;
using ImproveYourCv.Data;
using ImproveYourCv.Models;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv.Services;

public record RepoInfo(string Name, string? Description, string? Language, string HtmlUrl, bool Imported);

public class GitHubSync(HttpClient http, AppDbContext db)
{
    record Repo(
        string Name,
        string? Description,
        string? Homepage,
        string? Language,
        bool Fork,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("languages_url")] string LanguagesUrl);

    // shortcut: ilk 100 repo okunur, daha fazlası olursa sayfalama ekle
    async Task<List<Repo>> FetchAsync(string user, CancellationToken ct) =>
        (await http.GetFromJsonAsync<List<Repo>>($"users/{Uri.EscapeDataString(user)}/repos?per_page=100&type=owner&sort=pushed", ct) ?? [])
        .Where(r => !r.Fork).ToList();

    public async Task<List<RepoInfo>> ListAsync(string user, CancellationToken ct = default)
    {
        var repos = await FetchAsync(user, ct);
        var imported = (await db.Projects.Where(p => p.RepoUrl != null).Select(p => p.RepoUrl!).ToListAsync(ct)).ToHashSet();
        return repos.Select(r => new RepoInfo(r.Name, r.Description, r.Language, r.HtmlUrl, imported.Contains(r.HtmlUrl))).ToList();
    }

    // names null ise tüm repolar içe aktarılır
    public async Task<string> SyncAsync(string user, IReadOnlyCollection<string>? names = null, CancellationToken ct = default)
    {
        var repos = (await FetchAsync(user, ct)).Where(r => names is null || names.Contains(r.Name)).ToList();
        var repoCountByLang = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in repos)
        {
            var langs = await http.GetFromJsonAsync<Dictionary<string, long>>(r.LanguagesUrl, ct) ?? [];
            foreach (var l in langs.Keys) repoCountByLang[l] = repoCountByLang.GetValueOrDefault(l) + 1;

            var p = await db.Projects.FirstOrDefaultAsync(x => x.RepoUrl == r.HtmlUrl, ct);
            if (p is null) db.Projects.Add(p = new Project { Title = r.Name, RepoUrl = r.HtmlUrl });
            // Elle yazılmış açıklama ve demo linki ezilmez
            p.Description ??= r.Description;
            p.DemoUrl ??= string.IsNullOrWhiteSpace(r.Homepage) ? null : r.Homepage;
            p.Technologies = string.Join(", ", langs.OrderByDescending(x => x.Value).Select(x => x.Key));
        }

        var existing = await db.Skills.Select(s => s.Name.ToLower()).ToListAsync(ct);
        foreach (var (lang, count) in repoCountByLang.Where(x => !existing.Contains(x.Key.ToLower())))
            db.Skills.Add(new Skill { Name = lang, Level = LevelFor(count) });

        await db.SaveChangesAsync(ct);
        return $"{repos.Count} repo ve {repoCountByLang.Count} dil işlendi.";
    }

    // Yalnızca yeni eklenen yeteneklere uygulanır; mevcut seviyeler elle ayarlanır
    public static SkillLevel LevelFor(int repoCount) => repoCount switch
    {
        >= 5 => SkillLevel.Advanced,
        >= 2 => SkillLevel.Intermediate,
        _ => SkillLevel.Beginner,
    };
}
