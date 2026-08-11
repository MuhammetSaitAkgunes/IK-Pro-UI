using IKPro.Application.Common.Interfaces;
using IKPro.Infrastructure.Identity;
using IKPro.Infrastructure.Persistence;
using IKPro.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace IKPro.Infrastructure;

/// <summary>
/// Infrastructure katmanı servis kayıtları: DbContext + audit interceptor,
/// Identity + JWT Bearer kimlik doğrulama, token servisleri.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil.");

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        // Platform veritabanı: kiracı kimliği. Uygulama veritabanından AYRI bir
        // katalog; bağlantı dizesi tanımlı değilse fail-fast (sessizce uygulama
        // veritabanına düşmek, iki katmanı gizlice birleştirmek olurdu).
        var platformConnectionString = configuration.GetConnectionString("PlatformConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:PlatformConnection tanımlı değil.");

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseSqlServer(platformConnectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(PlatformDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory_Platform");
            }));

        services.AddScoped<IPlatformDbContext>(sp => sp.GetRequiredService<PlatformDbContext>());
        services.AddScoped<PlatformDbInitializer>();

        // Identity çekirdeği + roller + SignInManager (lockout kontrolü için).
        // Parola politikası demo hesaplarla (demo123) uyumlu tutulur.
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders(); // davet/şifre-belirleme token'ları (GeneratePasswordResetTokenAsync) için

        // JWT ayarları + Bearer kimlik doğrulama.
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);

        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt ayar bölümü eksik.");
        if (jwtOptions.Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret en az 32 karakter olmalı.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Token'lar kısa claim adlarıyla üretilir; eski SOAP claim eşlemesi kapalı.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IWorkingDayCalculator, SqlWorkingDayCalculator>();
        services.AddSingleton<Domain.Services.IPayrollEngine, Domain.Services.PayrollEngine>();
        services.AddSingleton<Application.Features.Payroll.Payslips.IPayslipGenerator, Pdf.QuestPdfPayslipGenerator>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserDirectorySource, Identity.UserDirectorySource>();
        services.AddScoped<ITenantDirectory, Tenancy.TenantDirectory>();
        services.AddScoped<ITenantPurger, Persistence.TenantPurger>();
        // Kiracıya bağlı olduğu için SCOPED: singleton olsaydı ilk isteğin kiracısı
        // sonsuza kadar yapışırdı (captive dependency).
        services.AddScoped<IFileStorage, Storage.LocalFileStorage>();
        // E-posta: Email:Mode=smtp → gerçek SMTP (MailKit, fail-fast doğrulamalı);
        // aksi halde (varsayılan "outbox") dosya stub'ı — dev/test davranışı değişmez.
        var emailMode = configuration["Email:Mode"] ?? "outbox";
        if (string.Equals(emailMode, "smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddOptions<Email.SmtpOptions>()
                .Bind(configuration.GetSection(Email.SmtpOptions.SectionName))
                .Validate(o => { o.Validate(); return true; })
                .ValidateOnStart();
            services.AddSingleton<IEmailSender, Email.MailKitSmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, Email.FileOutboxEmailSender>();
        }
        services.AddScoped<AppDbContextInitializer>();

        return services;
    }
}
