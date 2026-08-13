using Amazon.S3;
using Amazon.S3.Model;

namespace Grantify.Services;

// Where a document actually lives, and how to show it to the officer.
//   IsRemote = true  -> Url is a temporary S3 link; the page redirects to it.
//   IsRemote = false -> Url is a path on this machine; the page serves the file.
public record DocumentLocation(bool IsRemote, string Url, string ContentType, string FileName);

// SHARED SERVICE — announce changes in the group chat.
//
// Handles WHERE uploaded documents are kept. There are two modes, chosen by
// configuration, and that is deliberate:
//
//   Storage__BucketName set   -> Amazon S3 (the deployed site)
//   not set                   -> App_Data/uploads on this machine (our laptops)
//
// Why both: files written to the Elastic Beanstalk server do not survive a
// redeploy, and a second server would not see them, so the deployed site must
// use S3. But teammates run the app locally with no AWS credentials, so forcing
// S3 everywhere would break everyone's F5. The fallback keeps local development
// exactly as it was.
public class DocumentStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DocumentStorageService> _logger;
    private readonly string? _bucketName;

    // How long an officer's link to a document stays valid.
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(15);

    public DocumentStorageService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<DocumentStorageService> logger)
    {
        _environment = environment;
        _logger = logger;

        var name = configuration["Storage:BucketName"];
        _bucketName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    // Saves an uploaded file and returns the value to store in
    // ApplicationDocument.StoragePath — an S3 object key, or a relative path.
    public async Task<string> SaveAsync(int applicationId, string storedFileName, Stream content)
    {
        // Same shape either way: "7/abc123.pdf". In S3 the slash just makes the
        // console show it as a folder; on disk it really is one.
        var relativePath = $"{applicationId}/{storedFileName}";

        if (_bucketName is null)
        {
            var folder = Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", applicationId.ToString());
            Directory.CreateDirectory(folder);

            var fullPath = Path.Combine(folder, storedFileName);
            await using var file = File.Create(fullPath);
            await content.CopyToAsync(file);

            return relativePath;
        }

        // On Elastic Beanstalk the client picks up the instance's role
        // (LabRole) automatically, so there are no keys in our code or config.
        using var s3 = new AmazonS3Client();
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = relativePath,
            InputStream = content,
            ContentType = GuessContentType(storedFileName)
        });

        return relativePath;
    }

    // Works out how to show one stored document.
    // Returns null when the file cannot be found.
    public async Task<DocumentLocation?> GetAsync(string storagePath, string originalFileName)
    {
        var contentType = GuessContentType(originalFileName);

        if (_bucketName is null)
        {
            var uploadsRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "uploads");
            var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, storagePath));

            // Never serve a file from outside the uploads folder, even if the
            // stored path were ever tampered with to contain "..".
            var rootWithSeparator = Path.GetFullPath(uploadsRoot) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            {
                return null;
            }

            return new DocumentLocation(false, fullPath, contentType, originalFileName);
        }

        try
        {
            using var s3 = new AmazonS3Client();

            // A PRE-SIGNED URL: a link that works for a short time and then
            // stops. The bucket itself stays completely private, so a student's
            // transcript is never publicly readable and an old link cannot be
            // passed around.
            var url = await s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = storagePath,
                Expires = DateTime.UtcNow.Add(LinkLifetime),
                Verb = HttpVerb.GET
            });

            return new DocumentLocation(true, url, contentType, originalFileName);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Could not create a link for document {Key} in bucket {Bucket}.",
                storagePath, _bucketName);
            return null;
        }
    }

    // Enough types for what students actually upload.
    private static string GuessContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
