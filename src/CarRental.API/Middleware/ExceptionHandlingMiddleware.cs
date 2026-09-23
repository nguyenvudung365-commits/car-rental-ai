using CarRental.Application.Common;

namespace CarRental.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, ex.Message, ex.Code);
        }
        catch (ValidationAppException ex)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            await WriteProblem(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
    }

    private static Task WriteProblem(HttpContext context, int statusCode, string message, string? code = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { code, message });
    }
}