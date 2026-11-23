namespace ScreenDraw.Core.Diagnostics;

public static class Log
{
    private const long MaxBytes = 512 * 1024;

    private static readonly Lock Sync = new();

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenDraw",
        "screen-draw.log");

    public static void Write(string message)
    {
        lock (Sync)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length > MaxBytes)
                {
                    file.MoveTo(FilePath + ".old", overwrite: true);
                }

                File.AppendAllText(FilePath, $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public static void Error(string context, Exception exception) => Write($"error in {context}: {exception}");
}
