namespace NbcCms.Api.Models;

public class ApiProblemException(int statusCode, string title, string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;

    public static ApiProblemException BadRequest(string detail) => new(400, "Invalid request", detail);
    public static ApiProblemException Unauthorized(string detail) => new(401, "Not authorised", detail);
    public static ApiProblemException Forbidden(string detail) => new(403, "Forbidden", detail);
    public static ApiProblemException NotFound(string detail) => new(404, "Not found", detail);
    public static ApiProblemException Conflict(string detail) => new(409, "Conflict", detail);
}
