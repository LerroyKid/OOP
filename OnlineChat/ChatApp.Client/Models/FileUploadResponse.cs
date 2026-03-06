namespace ChatApp.Client.Views;

public class FileUploadResponse
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public long Size { get; set; }
}
