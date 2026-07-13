using FluentValidation;
using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Employees.Files;

/// <summary>Özlük evrakı yükler (nüfus cüzdanı, ikametgah, adli sicil vb.).</summary>
public sealed record UploadEmployeeDocumentCommand(
    int EmployeeId,
    string DocumentType,
    Stream Content,
    string FileName,
    string? ContentType,
    long Length) : IRequest<EmployeeDocumentDto>;

public sealed class UploadEmployeeDocumentCommandValidator : AbstractValidator<UploadEmployeeDocumentCommand>
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"];

    public UploadEmployeeDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(64);

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("Dosya boş olamaz.")
            .LessThanOrEqualTo(MaxBytes).WithMessage("Evrak en fazla 10 MB olabilir.");

        RuleFor(x => x.FileName)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage($"İzin verilen dosya türleri: {string.Join(", ", AllowedExtensions)}");
    }
}

public sealed class UploadEmployeeDocumentCommandHandler(IApplicationDbContext context, IFileStorage fileStorage)
    : IRequestHandler<UploadEmployeeDocumentCommand, EmployeeDocumentDto>
{
    public async Task<EmployeeDocumentDto> Handle(
        UploadEmployeeDocumentCommand request, CancellationToken cancellationToken)
    {
        var employeeExists = await context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            throw new NotFoundException("Personel", request.EmployeeId);
        }

        var stored = await fileStorage.SaveAsync(
            request.Content, request.FileName, $"documents-emp-{request.EmployeeId}", cancellationToken);

        var document = new EmployeeDocument
        {
            EmployeeId = request.EmployeeId,
            DocumentType = request.DocumentType.Trim(),
            FileName = request.FileName,
            FilePath = stored.Path,
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? null : request.ContentType,
            SizeBytes = stored.SizeBytes,
        };

        context.EmployeeDocuments.Add(document);
        await context.SaveChangesAsync(cancellationToken);

        return new EmployeeDocumentDto(
            document.Id, document.DocumentType, document.FileName,
            document.ContentType, document.SizeBytes, document.CreatedAtUtc);
    }
}
