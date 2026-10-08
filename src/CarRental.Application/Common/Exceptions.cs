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
    public ValidationAppException(string message) : base(message) { }
}

public class BadRequestException : ValidationAppException
{
    public BadRequestException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
