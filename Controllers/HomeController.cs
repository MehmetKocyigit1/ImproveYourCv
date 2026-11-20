using System.Diagnostics;
using DevRota.Data;
using DevRota.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevRota.Controllers;

// Site salt okunur; veriler Claude (MCP araçları) üzerinden yazılır
public class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View((
        await db.Projects.AsNoTracking().OrderBy(p => p.Title).ToListAsync(),
        await db.Skills.AsNoTracking().OrderBy(s => s.Name).ToListAsync(),
        await db.Goals.CountAsync()));

    public async Task<IActionResult> Goals() =>
        View(await db.Goals.AsNoTracking().Include(g => g.Tasks).OrderBy(g => g.TargetDate).ToListAsync());

    public async Task<IActionResult> Notes() =>
        View(await db.Notes.AsNoTracking().OrderByDescending(n => n.CreatedAt).ToListAsync());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
