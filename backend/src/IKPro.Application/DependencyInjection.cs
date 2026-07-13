using FluentValidation;
using IKPro.Application.Common.Behaviors;
using IKPro.Application.Common.Notifications;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IKPro.Application;

/// <summary>
/// Application katmanı servis kayıtları: MediatR (CQRS), FluentValidation
/// (pipeline'da otomatik doğrulama) ve Mapster (DTO eşleme) konfigürasyonu.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // IRegister implementasyonları (modül DTO eşlemeleri) assembly'den taranır.
        TypeAdapterConfig.GlobalSettings.Scan(assembly);

        // Bildirim tetikleyicileri (Faz 11): ayar toggle'larına uyar, IEmailSender'a yazar.
        services.AddScoped<INotificationTrigger, NotificationTrigger>();

        return services;
    }
}
