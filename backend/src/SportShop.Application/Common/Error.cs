using System.Diagnostics.CodeAnalysis;

namespace SportShop.Application.Common;

public enum ErrorType
{
    Validation,      // 400
    Unauthorized,    // 401
    Forbidden,       // 403
    NotFound,        // 404
    Conflict,        // 409
    BusinessRule,    // 422
    RateLimit,       // 429
    Unexpected       // 500
}

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Architecture specification requires Error type name.")]
public sealed record Error(
    string Code,
    string Description,
    ErrorType Type = ErrorType.BusinessRule,
    Dictionary<string, string[]>? ValidationErrors = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string code, string description, Dictionary<string, string[]>? errors = null) =>
        new(code, description, ErrorType.Validation, errors);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error BusinessRule(string code, string description) =>
        new(code, description, ErrorType.BusinessRule);

    public static Error RateLimit(string code, string description) =>
        new(code, description, ErrorType.RateLimit);

    public static Error Unexpected(string code, string description) =>
        new(code, description, ErrorType.Unexpected);
}
