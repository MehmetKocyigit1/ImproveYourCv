using ImproveYourCv.Data;
using ImproveYourCv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv.Controllers;

// Yetenek, proje ve hedef yönetimi (yalnızca Admin rolü)
[Authorize(Roles = Program.AdminRole), AutoValidateAntiforgeryToken]
public class ManageController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View((
        await db.Skills.AsNoTracking().OrderByDescending(s => s.Level).ThenBy(s => s.Name).ToListAsync(),
        await db.Projects.AsNoTracking().OrderBy(p => p.Title).ToListAsync(),
        await db.Goals.AsNoTracking().Include(g => g.Tasks).OrderBy(g => g.TargetDate).ToListAsync()));

    IActionResult Done(string message, string anchor)
    {
        TempData["Message"] = message;
        return Redirect(Url.Action(nameof(Index)) + "#" + anchor);
    }

    static string? Clean(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];

    [HttpPost]
    public async Task<IActionResult> AddSkill(string name, SkillLevel level)
    {
        if (Clean(name, 50) is not { } n) return Done("Yetenek adı boş olamaz.", "skills");
        if (!Enum.IsDefined(level)) return Done("Geçersiz seviye.", "skills");
        var s = await db.Skills.FirstOrDefaultAsync(x => x.Name.ToLower() == n.ToLower());
        if (s is null) db.Skills.Add(new Skill { Name = n, Level = level });
        else s.Level = level;
        await db.SaveChangesAsync();
        return Done(s is null ? $"{n} eklendi." : $"{n} zaten vardı, seviyesi güncellendi.", "skills");
    }

    [HttpPost]
    public async Task<IActionResult> SetSkillLevel(int id, SkillLevel level)
    {
        if (await db.Skills.FindAsync(id) is not { } s || !Enum.IsDefined(level)) return Done("Yetenek bulunamadı.", "skills");
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
            Title = t, Description = Clean(description, 1000), Technologies = Clean(technologies, 300),
            RepoUrl = repoUrl, DemoUrl = demoUrl,
        });
        await db.SaveChangesAsync();
        return Done($"{t} eklendi.", "projects");
    }

    [HttpPost]
    public async Task<IActionResult> AddGoal(string title, string? description, DateOnly? targetDate)
    {
        if (Clean(title, 100) is not { } t) return Done("Hedef adı boş olamaz.", "goals");
        db.Goals.Add(new Goal { Title = t, Description = Clean(description, 1000), TargetDate = targetDate });
        await db.SaveChangesAsync();
        return Done($"{t} hedefi eklendi.", "goals");
    }

    [HttpPost]
    public async Task<IActionResult> AddTask(int goalId, string title, DateOnly? dueDate)
    {
        if (Clean(title, 200) is not { } t) return Done("Görev adı boş olamaz.", "goals");
        if (!await db.Goals.AnyAsync(g => g.Id == goalId)) return Done("Hedef bulunamadı.", "goals");
        db.GoalTasks.Add(new GoalTask { GoalId = goalId, Title = t, DueDate = dueDate });
        await db.SaveChangesAsync();
        return Done("Görev eklendi.", "goals");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleTask(int id)
    {
        if (await db.GoalTasks.FindAsync(id) is not { } t) return Done("Görev bulunamadı.", "goals");
        t.IsDone = !t.IsDone;
        await db.SaveChangesAsync();
        return Done(t.IsDone ? "Görev tamamlandı." : "Görev yeniden açıldı.", "goals");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string kind, int id)
    {
        object? e = kind switch
        {
            "skill" => await db.Skills.FindAsync(id),
            "project" => await db.Projects.FindAsync(id),
            "goal" => await db.Goals.FindAsync(id),
            "task" => await db.GoalTasks.FindAsync(id),
            _ => null,
        };
        var anchor = kind switch { "skill" => "skills", "project" => "projects", _ => "goals" };
        if (e is null) return Done("Kayıt bulunamadı.", anchor);
        db.Remove(e);
        await db.SaveChangesAsync();
        return Done("Silindi.", anchor);
    }
}
