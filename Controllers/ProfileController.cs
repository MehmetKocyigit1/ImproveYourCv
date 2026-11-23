using System.Security.Claims;
using ImproveYourCv.Data;
using ImproveYourCv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv.Controllers;

public record ChecklistItem(string Label, bool Done, string Anchor);

public record ProfileView(
    UserProfile Profile, List<Project> Projects, List<Skill> Skills, List<ProfileEntry> Entries,
    List<Goal> Goals, int RoadmapCount, List<ChecklistItem> Checklist)
{
    public int Completeness => Checklist.Count(c => c.Done) * 100 / Checklist.Count;
    public IEnumerable<ProfileEntry> Of(EntryKind k) => Entries.Where(e => e.Kind == k);
}

// Giriş yapan kullanıcının kendi profili (yalnızca kendisi görür)
[Authorize]
public class ProfileController(AppDbContext db) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var uid = Uid;
        var profile = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == uid) ?? new UserProfile { UserId = uid };
        var projects = await db.Projects.AsNoTracking().Where(p => p.UserId == uid).OrderBy(p => p.Title).ToListAsync();
        var skills = await db.Skills.AsNoTracking().Where(s => s.UserId == uid).OrderByDescending(s => s.Level).ThenBy(s => s.Name).ToListAsync();
        var entries = await db.Entries.AsNoTracking().Where(e => e.UserId == uid)
            .OrderByDescending(e => e.EndDate == null && e.StartDate != null).ThenByDescending(e => e.EndDate ?? e.StartDate).ToListAsync();
        var goals = await db.Goals.AsNoTracking().Include(g => g.Tasks).Where(g => g.UserId == uid).OrderBy(g => g.TargetDate).ToListAsync();
        var roadmaps = await db.SurveyResponses.CountAsync(r => r.UserId == uid);

        bool Has(EntryKind k) => entries.Any(e => e.Kind == k);
        // Mühendis CV'lerinde beklenen başlıklar; eksik olanlar öneri olarak gösterilir
        List<ChecklistItem> checklist =
        [
            new("Ad soyad ve unvan", !string.IsNullOrWhiteSpace(profile.FullName) && !string.IsNullOrWhiteSpace(profile.Headline), "profile"),
            new("Kısa özet", !string.IsNullOrWhiteSpace(profile.Summary), "profile"),
            new("GitHub veya LinkedIn linki", !string.IsNullOrWhiteSpace(profile.GitHubUrl) || !string.IsNullOrWhiteSpace(profile.LinkedInUrl), "profile"),
            new("Eğitim bilgisi", Has(EntryKind.Education), "Education"),
            new("İş ya da staj deneyimi", Has(EntryKind.Experience) || Has(EntryKind.Internship), "Internship"),
            new("En az 2 proje", projects.Count >= 2, "projects"),
            new("En az 5 yetenek", skills.Count >= 5, "skills"),
            new("Yabancı dil", Has(EntryKind.Language), "Language"),
            new("Sertifika ya da kurs", Has(EntryKind.Certificate), "Certificate"),
            new("Yarışma, başarı ya da topluluk", Has(EntryKind.Achievement) || Has(EntryKind.Volunteer), "Achievement"),
        ];
        return View(new ProfileView(profile, projects, skills, entries, goals, roadmaps, checklist));
    }

    public async Task<IActionResult> Goals()
    {
        var uid = Uid;
        return View(await db.Goals.AsNoTracking().Include(g => g.Tasks).Where(g => g.UserId == uid).OrderBy(g => g.TargetDate).ToListAsync());
    }

    public async Task<IActionResult> Notes()
    {
        var uid = Uid;
        return View(await db.Notes.AsNoTracking().Where(n => n.UserId == uid).OrderByDescending(n => n.CreatedAt).ToListAsync());
    }
}
