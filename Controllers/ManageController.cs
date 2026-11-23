using System.Security.Claims;
using ImproveYourCv.Data;
using ImproveYourCv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv.Controllers;

public record ManageView(UserProfile Profile, List<Skill> Skills, List<Project> Projects, List<Goal> Goals, List<ProfileEntry> Entries);

// Kullanıcının kendi bilgilerini düzenlediği sayfa. Her sorgu UserId ile sınırlıdır;
// başkasının kaydının id'si gönderilse bile bulunamaz.
[Authorize, AutoValidateAntiforgeryToken]
public class ManageController(AppDbContext db) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var uid = Uid;
        return View(new ManageView(
            await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == uid) ?? new UserProfile { UserId = uid },
            await db.Skills.AsNoTracking().Where(s => s.UserId == uid).OrderBy(s => s.Category).ThenByDescending(s => s.Level).ThenBy(s => s.Name).ToListAsync(),
            await db.Projects.AsNoTracking().Where(p => p.UserId == uid).OrderBy(p => p.Title).ToListAsync(),
            await db.Goals.AsNoTracking().Include(g => g.Tasks).Where(g => g.UserId == uid).OrderBy(g => g.TargetDate).ToListAsync(),
            await db.Entries.AsNoTracking().Where(e => e.UserId == uid).OrderByDescending(e => e.StartDate).ToListAsync()));
    }

    IActionResult Done(string message, string anchor)
    {
        TempData["Message"] = message;
        return Redirect(Url.Action(nameof(Index)) + "#" + anchor);
    }

    static string? Clean(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];

    [HttpPost]
    public async Task<IActionResult> SaveProfile(UserProfile form)
    {
        var uid = Uid;
        var links = new[] { Clean(form.GitHubUrl, 300), Clean(form.LinkedInUrl, 300), Clean(form.WebsiteUrl, 300) };
        if (links.Any(l => !Project.IsHttpUrl(l))) return Done("Linkler http:// ya da https:// ile başlamalı.", "profile");
        var p = await db.Profiles.FindAsync(uid);
        if (p is null) db.Profiles.Add(p = new UserProfile { UserId = uid });
        p.FullName = Clean(form.FullName, 100);
        p.Headline = Clean(form.Headline, 150);
        p.Location = Clean(form.Location, 100);
        p.Summary = Clean(form.Summary, 1500);
        p.ContactEmail = Clean(form.ContactEmail, 256);
        p.Phone = Clean(form.Phone, 30);
        (p.GitHubUrl, p.LinkedInUrl, p.WebsiteUrl) = (links[0], links[1], links[2]);
        await db.SaveChangesAsync();
        return Done("Profil bilgilerin kaydedildi.", "profile");
    }

    [HttpPost]
    public async Task<IActionResult> AddEntry(EntryKind kind, string title, string? subtitle, DateOnly? startDate, DateOnly? endDate,
        string? detail, string? description, string? url)
    {
        if (!EntryKinds.Info.TryGetValue(kind, out var info)) return Done("Geçersiz kategori.", "profile");
        if (Clean(title, 150) is not { } t) return Done($"{info.TitleLabel} boş olamaz.", kind.ToString());
        url = Clean(url, 300);
        if (!Project.IsHttpUrl(url)) return Done("Link http:// ya da https:// ile başlamalı.", kind.ToString());
        if (startDate is { } s && endDate is { } e && e < s) return Done("Bitiş tarihi başlangıçtan önce olamaz.", kind.ToString());
        db.Entries.Add(new ProfileEntry
        {
            UserId = Uid, Kind = kind, Title = t, Subtitle = Clean(subtitle, 150),
            StartDate = info.Dates == DateMode.None ? null : startDate,
            EndDate = info.Dates == DateMode.Range ? endDate : null,
            Detail = info.DetailLabel is null ? null : Clean(detail, 150),
            Description = Clean(description, 1500), Url = url,
        });
        await db.SaveChangesAsync();
        return Done($"{info.Section}: {t} eklendi.", kind.ToString());
    }

    [HttpPost]
    public async Task<IActionResult> AddSkill(string name, SkillLevel level, SkillCategory category)
    {
        var uid = Uid;
        if (Clean(name, 50) is not { } n) return Done("Yetenek adı boş olamaz.", "skills");
        if (!Enum.IsDefined(level) || !Enum.IsDefined(category)) return Done("Geçersiz seçim.", "skills");
        var s = await db.Skills.FirstOrDefaultAsync(x => x.UserId == uid && x.Name.ToLower() == n.ToLower());
        if (s is null) db.Skills.Add(new Skill { UserId = uid, Name = n, Level = level, Category = category });
        else (s.Level, s.Category) = (level, category);
        await db.SaveChangesAsync();
        return Done(s is null ? $"{n} eklendi." : $"{n} zaten vardı, güncellendi.", "skills");
    }

    [HttpPost]
    public async Task<IActionResult> SetSkillLevel(int id, SkillLevel level)
    {
        var uid = Uid;
        if (await db.Skills.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid) is not { } s || !Enum.IsDefined(level))
            return Done("Yetenek bulunamadı.", "skills");
        s.Level = level;
        await db.SaveChangesAsync();
        return Done($"{s.Name} seviyesi güncellendi.", "skills");
    }

    [HttpPost]
    public async Task<IActionResult> AddProject(string title, string? description, string? technologies, string? repoUrl, string? demoUrl)
    {
        if (Clean(title, 100) is not { } t) return Done("Proje adı boş olamaz.", "projects");
        repoUrl = Clean(repoUrl, 300);
        demoUrl = Clean(demoUrl, 300);
        if (!Project.IsHttpUrl(repoUrl) || !Project.IsHttpUrl(demoUrl)) return Done("Linkler http:// ya da https:// ile başlamalı.", "projects");
        db.Projects.Add(new Project
        {
            UserId = Uid, Title = t, Description = Clean(description, 1000), Technologies = Clean(technologies, 300),
            RepoUrl = repoUrl, DemoUrl = demoUrl,
        });
        await db.SaveChangesAsync();
        return Done($"{t} eklendi.", "projects");
    }

    [HttpPost]
    public async Task<IActionResult> AddGoal(string title, string? description, DateOnly? targetDate)
    {
        if (Clean(title, 100) is not { } t) return Done("Hedef adı boş olamaz.", "goals");
        db.Goals.Add(new Goal { UserId = Uid, Title = t, Description = Clean(description, 1000), TargetDate = targetDate });
        await db.SaveChangesAsync();
        return Done($"{t} hedefi eklendi.", "goals");
    }

    [HttpPost]
    public async Task<IActionResult> AddTask(int goalId, string title, DateOnly? dueDate)
    {
        var uid = Uid;
        if (Clean(title, 200) is not { } t) return Done("Görev adı boş olamaz.", "goals");
        if (!await db.Goals.AnyAsync(g => g.Id == goalId && g.UserId == uid)) return Done("Hedef bulunamadı.", "goals");
        db.GoalTasks.Add(new GoalTask { GoalId = goalId, Title = t, DueDate = dueDate });
        await db.SaveChangesAsync();
        return Done("Görev eklendi.", "goals");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleTask(int id)
    {
        if (await OwnTask(id) is not { } t) return Done("Görev bulunamadı.", "goals");
        t.IsDone = !t.IsDone;
        await db.SaveChangesAsync();
        return Done(t.IsDone ? "Görev tamamlandı." : "Görev yeniden açıldı.", "goals");
    }

    Task<GoalTask?> OwnTask(int id)
    {
        var uid = Uid;
        return db.GoalTasks.FirstOrDefaultAsync(t => t.Id == id && db.Goals.Any(g => g.Id == t.GoalId && g.UserId == uid));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string kind, int id)
    {
        var uid = Uid;
        object? e = kind switch
        {
            "skill" => await db.Skills.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid),
            "project" => await db.Projects.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid),
            "goal" => await db.Goals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid),
            "entry" => await db.Entries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid),
            "task" => await OwnTask(id),
            _ => null,
        };
        var anchor = e switch
        {
            Skill => "skills",
            Project => "projects",
            ProfileEntry pe => pe.Kind.ToString(),
            _ => "goals",
        };
        if (e is null) return Done("Kayıt bulunamadı.", anchor);
        db.Remove(e);
        await db.SaveChangesAsync();
        return Done("Silindi.", anchor);
    }
}
