namespace ExamPrep.Infrastructure.Content;

/// <summary>Where uploaded pictures and audio tracks are kept. Paths returned are relative to the storage root.</summary>
public interface IAssetStorage
{
    Task<string> SaveAsync(byte[] content, string sha256, string extension, CancellationToken cancellationToken);
}

/// <summary>
/// Content-addressed files on disk: <c>ab/abcdef….png</c> under the configured root, so the same file is
/// written once however many papers use it.
/// </summary>
public sealed class FileSystemAssetStorage(string root) : IAssetStorage
{
    public async Task<string> SaveAsync(byte[] content, string sha256, string extension, CancellationToken cancellationToken)
    {
        var folder = sha256[..2];
        var fileName = $"{sha256}{extension.ToLowerInvariant()}";
        var directory = Path.Combine(root, folder);
        var fullPath = Path.Combine(directory, fileName);

        if (!File.Exists(fullPath))
        {
            Directory.CreateDirectory(directory);
            var temporary = $"{fullPath}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllBytesAsync(temporary, content, cancellationToken);
            File.Move(temporary, fullPath, overwrite: true);
        }

        return $"{folder}/{fileName}";
    }
}
