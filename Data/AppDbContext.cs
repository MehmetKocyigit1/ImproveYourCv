using ImproveYourCv.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<GoalTask> GoalTasks => Set<GoalTask>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<ProfileEntry> Entries => Set<ProfileEntry>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // Identity tabloları

        b.Entity<UserProfile>().HasKey(p => p.UserId);
        b.Entity<UserProfile>().HasOne<IdentityUser>().WithOne().HasForeignKey<UserProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

        // Kullanıcıya ait kayıtlar hesapla birlikte silinir
        b.Entity<Project>().HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Skill>().HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Goal>().HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Note>().HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileEntry>().HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Skill>().HasIndex(s => new { s.UserId, s.Name }).IsUnique();
        b.Entity<ProfileEntry>().HasIndex(e => new { e.UserId, e.Kind });
        b.Entity<SurveyResponse>().HasIndex(s => s.PublicId).IsUnique();
        b.Entity<SurveyResponse>().HasIndex(s => s.CreatedAt);
        // Hesap silinince yol haritaları kalır, yalnızca hesapla bağı kopar
        b.Entity<SurveyResponse>().HasOne<IdentityUser>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.SetNull);
    }

    // Tek kullanıcılı sürümden kalan (sahipsiz) kayıtları verilen hesaba bağlar
    public async Task ClaimLegacyDataAsync(string userId)
    {
        await Projects.Where(x => x.UserId == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, userId));
        await Skills.Where(x => x.UserId == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, userId));
        await Goals.Where(x => x.UserId == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, userId));
        await Notes.Where(x => x.UserId == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, userId));
    }
}
