using IKPro.API.Middleware;
using IKPro.API.Services;
using IKPro.Application;
using IKPro.Application.Common.Interfaces;
using IKPro.Application.Common.Security;
using IKPro.Domain.Constants;
using IKPro.Infrastructure;
using Microsoft.OpenApi.Models;
using Serilog;

// Bootstrap logger — captures failures during startup before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog reads its full configuration from appsettings.json.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Application (MediatR + FluentValidation + Mapster) ve
    // Infrastructure (EF Core + MSSQL + audit interceptor + Identity/JWT).
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // İstek bağlamındaki kullanıcı (audit + yetki kapsamı).
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    // Rol policy'leri — frontend routes.js roles[] matrisiyle birebir.
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(Policies.HrAdminOnly, policy => policy.RequireRole(Roles.HrAdmin));
        options.AddPolicy(Policies.Management, policy => policy.RequireRole(Roles.HrAdmin, Roles.Manager));
        options.AddPolicy(Policies.PayrollAccess, policy => policy.RequireRole(Roles.HrAdmin, Roles.Employee));
    });

    // MVC controllers + JSON.
    builder.Services.AddControllers();

    // RFC 7807 ProblemDetails for consistent error envelopes.
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // OpenAPI / Swagger with a JWT bearer scheme so later phases can authorize from the UI.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "İK Pro API",
            Version = "v1",
            Description = "İK Pro insan kaynakları platformu backend API'si."
        });

        var jwtScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT token'ını 'Bearer {token}' formatında girin.",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };
        options.AddSecurityDefinition("Bearer", jwtScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [jwtScheme] = Array.Empty<string>()
        });
    });

    // Health checks (self + veritabanı bağlantısı).
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<IKPro.Infrastructure.Persistence.AppDbContext>("database");

    // CORS — allow the local frontend origin during development.
    const string frontendCors = "frontend";
    builder.Services.AddCors(options => options.AddPolicy(frontendCors, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    // Development'ta migration + demo seed otomatik uygulanır (docker-compose MSSQL).
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IKPro.Infrastructure.Persistence.AppDbContextInitializer>();
        await initializer.InitialiseAsync();
        await initializer.SeedAsync();
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "İK Pro API v1");
            options.DocumentTitle = "İK Pro API";
        });
    }

    app.UseHttpsRedirection();
    app.UseCors(frontendCors);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "İK Pro API başlatılırken kritik hata oluştu.");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
