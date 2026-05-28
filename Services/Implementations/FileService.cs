using DentBridge.Services.Interfaces;

namespace DentBridge.Services.Implementations;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] _allowedImages = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] _allowedDocs = [".pdf", ".jpg", ".jpeg", ".png"];
    private const long MaxImageSize = 5 * 1024 * 1024;  // 5 MB
    private const long MaxDocSize = 10 * 1024 * 1024;   // 10 MB

    public FileService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveCaseImageAsync(IFormFile file, int caseId)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "cases", caseId.ToString());
        return await SaveFileAsync(file, folder, "uploads/cases/" + caseId);
    }

    public async Task<string> SaveProofDocumentAsync(IFormFile file, string userId)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "proofs");
        return await SaveFileAsync(file, folder, "uploads/proofs");
    }

    public async Task<string> SaveAvatarAsync(IFormFile file, string userId)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        return await SaveFileAsync(file, folder, "uploads/avatars");
    }

    private static async Task<string> SaveFileAsync(IFormFile file, string folder, string relativeFolderPrefix)
    {
        Directory.CreateDirectory(folder);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/{relativeFolderPrefix}/{fileName}";
    }

    public void DeleteFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public bool IsValidImage(IFormFile file) =>
        file is { Length: > 0 } &&
        file.Length <= MaxImageSize &&
        _allowedImages.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());

    public bool IsValidDocument(IFormFile file) =>
        file is { Length: > 0 } &&
        file.Length <= MaxDocSize &&
        _allowedDocs.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
}
