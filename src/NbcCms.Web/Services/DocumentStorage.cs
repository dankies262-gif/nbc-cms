namespace NbcCms.Web.Services;

public class DocumentStorage(IWebHostEnvironment environment, IConfiguration configuration)
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx" };

    private const long DefaultMaxBytes = 10 * 1024 * 1024;

    private long MaxBytes => configuration.GetValue<long?>("Cms:MaxUploadBytes") ?? DefaultMaxBytes;

    public long MaxMegabytes => MaxBytes / (1024 * 1024);

    public string[] Extensions => AllowedExtensions.OrderBy(x => x).ToArray();

    public string UploadRoot => Path.Combine(environment.ContentRootPath, "App_Data", "uploads");

    public async Task<(string RelativePath, string OriginalName)> SaveAsync(int claimId, IFormFile file)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("The selected file is empty.");
        }

        if (file.Length > MaxBytes)
        {
            throw new InvalidOperationException($"Files must be {MaxMegabytes} MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only PDF, PNG, JPG, DOCX or XLSX files are allowed.");
        }

        var folder = Path.Combine(UploadRoot, claimId.ToString());
        Directory.CreateDirectory(folder);

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(folder, storedName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return ($"claims/{claimId}/{storedName}", Path.GetFileName(file.FileName));
    }

    public string? ResolvePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var root = Path.GetFullPath(UploadRoot);
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(candidate))
        {
            return null;
        }

        return candidate;
    }

    public void Delete(string? relativePath)
    {
        var path = ResolvePath(relativePath);
        if (path is not null)
        {
            System.IO.File.Delete(path);
        }
    }
}
