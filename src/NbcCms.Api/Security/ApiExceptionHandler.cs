using Microsoft.AspNetCore.Diagnostics;
using NbcCms.Api.Models;

namespace NbcCms.Api.Security;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string title;
        string detail;
        int statusCode;

        if (exception is ApiProblemException apiProblem)
        {
            statusCode = apiProblem.StatusCode;
            title = apiProblem.Title;
            detail = apiProblem.Message;
        }
        else
        {
            logger.LogError(exception, "Unhandled API exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            statusCode = StatusCodes.Status500InternalServerError;
            title = "Unexpected error";
            detail = "The request could not be completed. Try again or contact the system administrator.";
        }

        await Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            instance: httpContext.Request.Path).ExecuteAsync(httpContext);

        return true;
    }
}
