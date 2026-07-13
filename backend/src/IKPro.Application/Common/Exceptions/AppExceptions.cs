namespace IKPro.Application.Common.Exceptions;

/// <summary>Kimlik doğrulama başarısız (401). Mesaj kullanıcıya gösterilebilir.</summary>
public class UnauthorizedException(string message) : Exception(message);

/// <summary>Yetki kapsamı dışı erişim (403).</summary>
public class ForbiddenAccessException(string message = "Bu işlem için yetkiniz yok.") : Exception(message);

/// <summary>Kayıt bulunamadı (404).</summary>
public class NotFoundException(string name, object key)
    : Exception($"{name} bulunamadı (anahtar: {key}).");

/// <summary>Çakışan kayıt / iş kuralı ihlali (409).</summary>
public class ConflictException(string message) : Exception(message);
