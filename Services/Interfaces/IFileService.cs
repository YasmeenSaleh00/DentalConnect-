namespace DentBridge.Services.Interfaces;

public interface IFileService
{
    Task<string> SaveCaseImageAsync(IFormFile file, int caseId);
    Task<string> SaveProofDocumentAsync(IFormFile file, string userId);
    Task<string> SaveAvatarAsync(IFormFile file, string userId);
    void DeleteFile(string relativePath);
    bool IsValidImage(IFormFile file);
    bool IsValidDocument(IFormFile file);
}
