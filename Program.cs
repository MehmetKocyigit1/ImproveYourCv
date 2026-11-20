using System.Threading.RateLimiting;
using ImproveYourCv.Data;
using ImproveYourCv.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ImproveYourCv;

public class Program
{
    public const string AdminRole = "Admin";

    public static async Task Main(string[] args)
    {
        // "--mcp": Claude'un bağlandığı MCP sunucusu (stdio). Web'e hiç açılmaz.
        if (args.Contains("--mcp"))
        {
            // "--mcp" çıkarılır; yoksa yapılandırma sonraki argümanı onun değeri sanar
            var mcp = Host.CreateApplicationBuilder(args.Where(a => a != "--mcp").ToArray());
            // stdout MCP protokolüne ayrılmış; loglar stderr'e gider
            mcp.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
            AddCore(mcp.Services, mcp.Configuration);
            mcp.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
            var host = mcp.Build();
            await MigrateAsync(host.Services);
            await host.RunAsync();
            return;
        }

        var builder = WebApplication.CreateBuilder(args);
        AddCore(builder.Services, builder.Configuration);
        builder.Services.AddControllersWithViews();

        builder.Services.AddIdentity<IdentityUser, IdentityRole>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = false;
                o.Lockout.MaxFailedAccessAttempts = 5; // 5 hatalı denemede hesap 5 dk kilitlenir
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<TurkishIdentityErrors>();
        builder.Services.ConfigureApplicationCookie(o =>
        {
            o.LoginPath = "/Account/Login";
            o.AccessDeniedPath = "/Account/Denied";
        });

        // Anket ve hesap formları IP başına sınırlı (spam ve kaba kuvvet denemelerine karşı)
        // shortcut: Azure'da gerçek IP için uygulama ayarına ASPNETCORE_FORWARDEDHEADERS_ENABLED=true ekle
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = (ctx, ct) => new ValueTask(ctx.HttpContext.Response.WriteAsync(
                "Çok fazla deneme yaptın. Lütfen bir saat sonra tekrar dene.", ct));
            void PerIp(string name, int limit) => o.AddPolicy(name, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromHours(1) }));
            PerIp("survey", builder.Configuration.GetValue("Survey:HourlyLimitPerIp", 3));
            PerIp("auth", builder.Configuration.GetValue("Auth:HourlyLimitPerIp", 20));
        });

        var app = builder.Build();
        await MigrateAsync(app.Services);
        await EnsureAdminAsync(app.Services, app.Configuration);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        await app.RunAsync();
    }

    static void AddCore(IServiceCollection services, IConfiguration config)
    {
        // Yerelde LocalDB; yayında ConnectionStrings__Default ortam değişkeniyle Azure SQL bağlantısı verilir
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=ImproveYourCv;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddHttpClient<GitHubSync>(c =>
        {
            c.BaseAddress = new Uri("https://api.github.com/");
            c.DefaultRequestHeaders.UserAgent.ParseAdd("ImproveYourCv");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            // Token yoksa saatte 60 istek sınırı var; user-secrets ile GitHub:Token eklenebilir
            if (config["GitHub:Token"] is { Length: > 0 } token)
                c.DefaultRequestHeaders.Authorization = new("Bearer", token);
        });
    }

    static async Task MigrateAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        // Şema değişiklikleri Data/Migrations içinden uygulanır (yeni değişiklik: dotnet ef migrations add <Ad>)
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    // Admin:Email ayarındaki hesap kayıtlıysa yönetici yapılır (kayıt sırasında da kontrol edilir)
    static async Task EnsureAdminAsync(IServiceProvider sp, IConfiguration config)
    {
        using var scope = sp.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(AdminRole)) await roles.CreateAsync(new IdentityRole(AdminRole));
        if (config["Admin:Email"] is not { Length: > 0 } email) return;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        if (await users.FindByEmailAsync(email) is { } admin && !await users.IsInRoleAsync(admin, AdminRole))
            await users.AddToRoleAsync(admin, AdminRole);
    }
}
