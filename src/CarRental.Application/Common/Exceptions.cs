namespace CarRental.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public string Code { get; }

    public ConflictException(string code, string message) : base(message)
        => Code = code;
}

public class ValidationAppException : Exception
{
    public ValidationAppException(string message) : base(message) { }
}


public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}