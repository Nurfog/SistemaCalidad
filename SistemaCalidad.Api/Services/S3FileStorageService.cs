using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace SistemaCalidad.Api.Services;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3FileStorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["FileStorage:S3:BucketName"] ?? "sistemacalidad-nch2728";
        
        // Inicializar bucket si no existe (Desde 0)
        InitializeBucketAsync().Wait();
    }

    private async Task InitializeBucketAsync()
    {
        try
        {
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName))
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                };
                await _s3Client.PutBucketAsync(putBucketRequest);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al inicializar Bucket S3: {ex.Message}");
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string subDirectory)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var key = $"{subDirectory}/{uniqueFileName}".Replace("\\", "/");

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            AutoCloseStream = true
        };

        await _s3Client.PutObjectAsync(putRequest);

        return key;
    }

    public async Task<(Stream Content, string ContentType, string FileName)> GetFileAsync(string filePath)
    {
        var getRequest = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = filePath
        };

        using var response = await _s3Client.GetObjectAsync(getRequest);
        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return (memoryStream, response.Headers.ContentType, Path.GetFileName(filePath));
    }

    public void DeleteFile(string filePath)
    {
        _s3Client.DeleteObjectAsync(_bucketName, filePath).Wait();
    }
}
