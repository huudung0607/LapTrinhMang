namespace NetworkProgramming.Services;

public static class UploadStore
{
    public const string Folder = "uploads";
    public static readonly string[] ImageExt = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public static string PathOf(string storedName) => Path.GetFullPath(Path.Combine(Folder, Path.GetFileName(storedName)));
}
