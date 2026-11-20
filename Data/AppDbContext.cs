using ImproveYourCv.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<GoalTask> GoalTasks => Set<GoalTask>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // Identity tabloları
        b.Entity<Skill>().HasIndex(s => s.Name).IsUnique();
        b.Entity<SurveyResponse>().HasIndex(s => s.PublicId).IsUnique();
        b.Entity<SurveyResponse>().HasIndex(s => s.CreatedAt);
        // Hesap silinince yol haritaları kalır, yalnızca hesapla bağı kopar
        b.Entity<SurveyResponse>().HasOne<IdentityUser>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
