using System.ComponentModel.DataAnnotations.Schema;

namespace DevRota.Models;

public enum SkillLevel { Beginner, Intermediate, Advanced }

public class Project
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Technologies { get; set; } // virgülle ayrılmış: "C#, SQL"
    public string? RepoUrl { get; set; }
    public string? DemoUrl { get; set; }

    // Linkler href olarak basıldığı için yalnızca http(s) kabul edilir (javascript: gibi şemalar engellenir)
    public static bool IsHttpUrl(string? url) => url is null
        || Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}

public class Skill
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public SkillLevel Level { get; set; }
}

public class Goal
{
    public int Id { get; set; }
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

public class Note
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
