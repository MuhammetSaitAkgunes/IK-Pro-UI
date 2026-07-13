using FluentValidation;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Constants;
using MediatR;

namespace IKPro.Application.Features.Auth.Register;

/// <summary>Rol verilmezse en düşük yetkili rol (employee) atanır.</summary>
public sealed record RegisterCommand(string Name, string Email, string Password, string? Role)
    : IRequest<AuthResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role)
            .Must(role => role is null || Roles.All.Contains(role))
            .WithMessage($"Rol şunlardan biri olmalı: {string.Join(", ", Roles.All)}");
    }
}

public sealed class RegisterCommandHandler(IIdentityService identityService)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        => identityService.RegisterAsync(
            request.Name, request.Email, request.Password, request.Role ?? Roles.Employee, cancellationToken);
}
