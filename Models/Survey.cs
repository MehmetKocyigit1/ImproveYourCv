using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ImproveYourCv.Models;

// Ziyaretçinin doldurduğu anket (form modeli)
public class SurveyAnswers
{
    [Required(ErrorMessage = "Lütfen bir seçenek işaretle.")]
    public string? Status { get; set; }

    public List<string> Fields { get; set; } = [];

    [Required(ErrorMessage = "Lütfen bir seçenek işaretle.")]
    public string? Level { get; set; }

    public List<string> Known { get; set; } = [];

    [StringLength(200, ErrorMessage = "En fazla 200 karakter yazabilirsin.")]
    public string? OtherKnown { get; set; }

    [StringLength(500, ErrorMessage = "En fazla 500 karakter yazabilirsin.")]
    public string? WantToLearn { get; set; }

    [Required(ErrorMessage = "Lütfen bir seçenek işaretle.")]
    public string? Goal { get; set; }

    [Required(ErrorMessage = "Lütfen bir seçenek işaretle.")]
    public string? Hours { get; set; }
}

public static class SurveyOptions
{
    public const string Undecided = "Henüz hiçbir fikrim yok";
    public static readonly string[] Status = ["Alanımı seçtim", "Birkaç alan arasında kararsızım", Undecided];
    public static readonly string[] Fields =
    [
        "Backend", "Frontend", "Mobil", "Veri Bilimi ve Yapay Zekâ",
        "Siber Güvenlik", "DevOps ve Bulut", "Oyun Geliştirme", "Gömülü Sistemler ve IoT",
    ];
    public static readonly string[] Level = ["1. sınıf", "2. sınıf", "3. sınıf", "4. sınıf", "Mezun / çalışıyorum"];
    public static readonly string[] Known = ["C", "C++", "C#", "Java", "Python", "JavaScript", "SQL", "HTML/CSS", "Git", "Linux"];
    public static readonly string[] Goal = ["Staj bulmak", "İlk işimi bulmak", "Kendi projemi geliştirmek", "Alanları keşfetmek"];
    public static readonly string[] Hours = ["Haftada 1–3 saat", "Haftada 4–7 saat", "Haftada 8–15 saat", "Haftada 15+ saat"];

    // Formdan gelen değerler yalnızca listedeki seçeneklerden olabilir
    public static void Validate(SurveyAnswers a, ModelStateDictionary ms)
    {
        if (a.Status is not null && !Status.Contains(a.Status)) ms.AddModelError(nameof(a.Status), "Geçersiz seçim.");
        if (a.Level is not null && !Level.Contains(a.Level)) ms.AddModelError(nameof(a.Level), "Geçersiz seçim.");
        if (a.Goal is not null && !Goal.Contains(a.Goal)) ms.AddModelError(nameof(a.Goal), "Geçersiz seçim.");
        if (a.Hours is not null && !Hours.Contains(a.Hours)) ms.AddModelError(nameof(a.Hours), "Geçersiz seçim.");
        if (a.Fields.Any(f => !Fields.Contains(f)) || a.Known.Any(k => !Known.Contains(k)))
            ms.AddModelError(nameof(a.Fields), "Geçersiz seçim.");
        if (a.Fields.Count > 3) ms.AddModelError(nameof(a.Fields), "En fazla üç alan seçebilirsin.");
        if (a.Fields.Count == 0 && a.Status is not null && a.Status != Undecided)
            ms.AddModelError(nameof(a.Fields), "En az bir alan seç.");
    }
}

public class SurveyResponse
{
    public static readonly System.Text.Json.JsonSerializerOptions Json = new(System.Text.Json.JsonSerializerDefaults.Web);

    public int Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid(); // sonuç linki tahmin edilemesin diye
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; } // giriş yapmış ziyaretçinin hesabı (anonim ise null)
    public required string AnswersJson { get; set; }
    public string? RoadmapJson { get; set; } // null: yol haritası henüz hazırlanmadı
}

// Claude'un MCP save_roadmap aracıyla yazdığı yol haritası
public record Roadmap(
    string Summary,
    string RecommendedField,
    string FieldReason,
    List<RoadmapPhase> Phases,
    List<string> FirstWeek,
    List<RoadmapResource> Resources);

public record RoadmapPhase(string Title, string Duration, List<string> Topics, string Project);

public record RoadmapResource(string Title, string Type);
