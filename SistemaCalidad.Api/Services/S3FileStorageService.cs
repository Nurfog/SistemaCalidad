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
            AutoCloseStream = true,
            ContentType = GetContentType(fileName)
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

        var response = await _s3Client.GetObjectAsync(getRequest);
        // Retornamos el stream directamente, el controlador se encargará de cerrarlo al terminar la respuesta
        return (response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream", Path.GetFileName(filePath));
    }

    public async Task DeleteFileAsync(string filePath)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, filePath);
    }

    private string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
