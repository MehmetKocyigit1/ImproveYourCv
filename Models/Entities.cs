using System.ComponentModel.DataAnnotations.Schema;

namespace ImproveYourCv.Models;

public enum SkillLevel { Beginner, Intermediate, Advanced }
public enum SkillCategory { Technical, Soft }

// Tüm kayıtlar bir kullanıcıya aittir (UserId). Hesap silinince kayıtları da silinir.
// UserId null olan kayıtlar eski tek kullanıcılı sürümden kalmadır; ilk yönetici hesabına bağlanır.
public interface IOwned { string? UserId { get; set; } }

public class UserProfile
{
    public required string UserId { get; set; }
    public string? FullName { get; set; }
    public string? Headline { get; set; }       // Örn. "Bilgisayar Mühendisliği 3. sınıf · Backend"
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string? GitHubUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}

public class Project : IOwned
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Technologies { get; set; } // virgülle ayrılmış: "C#, SQL"
    public string? RepoUrl { get; set; }
    public string? DemoUrl { get; set; }

    // Linkler href olarak basıldığı için yalnızca http(s) kabul edilir (javascript: gibi şemalar engellenir)
    public static bool IsHttpUrl(string? url) => url is null
        || Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}

public class Skill : IOwned
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public required string Name { get; set; }
    public SkillLevel Level { get; set; }
    public SkillCategory Category { get; set; }
}

public class Goal : IOwned
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? TargetDate { get; set; }
    public List<GoalTask> Tasks { get; set; } = [];

    [NotMapped]
    public int Progress => Tasks.Count == 0 ? 0 : Tasks.Count(t => t.IsDone) * 100 / Tasks.Count;
}

public class GoalTask
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public required string Title { get; set; }
    public bool IsDone { get; set; }
    public DateOnly? DueDate { get; set; }
}

public class Note : IOwned
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Eğitim, deneyim, sertifika, dil ve başarılar aynı yapıda tutulur; alan adları türe göre değişir (EntryKinds)
public enum EntryKind { Education, Experience, Internship, Certificate, Language, Achievement, Volunteer }

public class ProfileEntry : IOwned
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public EntryKind Kind { get; set; }
    public required string Title { get; set; }   // Okul / Şirket / Sertifika / Dil / Başarı / Topluluk
    public string? Subtitle { get; set; }        // Bölüm / Pozisyon / Veren kurum / Seviye / Etkinlik / Rol
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }       // Başlangıç var, bitiş yoksa "Devam ediyor"
    public string? Detail { get; set; }          // GNO / Teknolojiler / Sınav puanı / Derece
    public string? Description { get; set; }
    public string? Url { get; set; }
}

public enum DateMode { None, Range, Single }

// Her türün sayfadaki başlığı ve form etiketleri
public record EntryKindInfo(string Section, string TitleLabel, string? SubtitleLabel, string? DetailLabel,
    DateMode Dates, string TitleExample, string? SubtitleExample = null, string? DetailExample = null);

public static class EntryKinds
{
    public static readonly Dictionary<EntryKind, EntryKindInfo> Info = new()
    {
        [EntryKind.Education] = new("Eğitim", "Okul", "Bölüm / derece", "Not ortalaması", DateMode.Range, "Örn. Ege Üniversitesi", "Bilgisayar Mühendisliği (Lisans)", "3.20 / 4.00"),
        [EntryKind.Experience] = new("İş deneyimi", "Şirket", "Pozisyon", "Kullanılan teknolojiler", DateMode.Range, "Şirket adı", "Junior Backend Developer", "C#, SQL Server"),
        [EntryKind.Internship] = new("Stajlar", "Şirket / kurum", "Staj alanı", "Kullanılan teknolojiler", DateMode.Range, "Şirket adı", "Yazılım stajı", "Java, Spring"),
        [EntryKind.Certificate] = new("Sertifikalar ve kurslar", "Sertifika adı", "Veren kurum", "Sertifika no / puan", DateMode.Single, "AWS Cloud Practitioner", "Amazon Web Services"),
        [EntryKind.Language] = new("Yabancı diller", "Dil", "Seviye (A1–C2)", "Sınav ve puan", DateMode.None, "İngilizce", "B2", "YDS 75 / IELTS 6.5"),
        [EntryKind.Achievement] = new("Yarışmalar ve başarılar", "Başarı", "Etkinlik / kurum", "Derece", DateMode.Single, "Finalist", "TEKNOFEST, TÜBİTAK 2209-A, hackathon", "1.lik"),
        [EntryKind.Volunteer] = new("Topluluk ve gönüllülük", "Topluluk / kuruluş", "Rol", null, DateMode.Range, "IEEE Öğrenci Kolu, GDSC", "Yönetim kurulu üyesi"),
    };

    public static string Dates(ProfileEntry e) => (Info[e.Kind].Dates, e.StartDate, e.EndDate) switch
    {
        (DateMode.None, _, _) or (_, null, null) => "",
        (DateMode.Single, { } s, _) => $"{s:MM.yyyy}",
        (_, { } s, null) => $"{s:MM.yyyy} – devam ediyor",
        (_, null, { } en) => $"{en:MM.yyyy}",
        (_, { } s, { } en) => $"{s:MM.yyyy} – {en:MM.yyyy}",
    };
}
