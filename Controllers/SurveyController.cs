using System.Security.Claims;
using System.Text.Json;
using DevRota.Data;
using DevRota.Models;
using DevRota.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DevRota.Controllers;

// Ziyaretçi anketi doldurur, yol haritası anında üretilir.
// Claude isterse MCP save_roadmap ile planı daha kişisel bir planla değiştirebilir.
public class SurveyController(AppDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new SurveyAnswers());

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("survey")]
    public async Task<IActionResult> Index(SurveyAnswers answers, CancellationToken ct)
    {
        SurveyOptions.Validate(answers, ModelState);
        if (!ModelState.IsValid) return View(answers);

        var response = new SurveyResponse
        {
            AnswersJson = JsonSerializer.Serialize(answers, SurveyResponse.Json),
            RoadmapJson = JsonSerializer.Serialize(RoadmapBuilder.Build(answers), SurveyResponse.Json),
            // Giriş yapılmışsa plan hesaba bağlanır ve Hesabım sayfasında listelenir
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        };
        db.SurveyResponses.Add(response);
        await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Result), new { id = response.PublicId });
    }

    [HttpGet]
    public async Task<IActionResult> Result(Guid id)
    {
        var r = await db.SurveyResponses.FirstOrDefaultAsync(x => x.PublicId == id);
        if (r is null) return NotFound();
        var answers = JsonSerializer.Deserialize<SurveyAnswers>(r.AnswersJson, SurveyResponse.Json)!;
        // Eski (yol haritası olmadan kaydedilmiş) cevaplar için plan ilk açılışta üretilir
        if (r.RoadmapJson is null)
        {
            r.RoadmapJson = JsonSerializer.Serialize(RoadmapBuilder.Build(answers), SurveyResponse.Json);
            await db.SaveChangesAsync();
        }
        return View((answers, JsonSerializer.Deserialize<Roadmap>(r.RoadmapJson, SurveyResponse.Json)));
    }
}
