namespace CarRental.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, object id) : base($"{entity} {id} không tồn tại.") { }
}

public class ConflictException : Exception
{
    public string Code { get; }
    public ConflictException(string code, string message) : base(message) => Code = code;
}

public class ValidationAppException : Exception
{
    public string? Code { get; }
    public ValidationAppException(string message) : base(message) { }
    public ValidationAppException(string code, string message) : base(message) => Code = code;
}

public class BadRequestException : ValidationAppException
{
    public BadRequestException(string message) : base(message) { }
    public BadRequestException(string code, string message) : base(code, message) { }
}

public class ForbiddenException : Exception
{
    public string? Code { get; }
    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException(string code, string message) : base(message) => Code = code;
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
