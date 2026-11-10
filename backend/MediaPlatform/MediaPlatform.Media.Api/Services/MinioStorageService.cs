using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace MediaPlatform.Media.Api.Services;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
}

public class MinioStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName = "media-platform";
    private readonly string _endpoint = "http://localhost:9000";

    public MinioStorageService(IConfiguration configuration)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = _endpoint,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client("miniouser", "miniopassword", config);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
        if (!bucketExists)
        {
            await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName });

            var publicPolicy = $$"""
            {
                "Version": "2012-10-17",
                "Statement": [
                    {
                        "Sid": "PublicRead",
                        "Effect": "Allow",
                        "Principal": "*",
                        "Action": ["s3:GetObject"],
                        "Resource": ["arn:aws:s3:::{{_bucketName}}/*"]
                    }
                ]
            }
            """;
            await _s3Client.PutBucketPolicyAsync(new PutBucketPolicyRequest
            {
                BucketName = _bucketName,
                Policy = publicPolicy
            });
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = uniqueFileName,
            InputStream = fileStream,
            ContentType = contentType
        });

        return $"{_endpoint}/{_bucketName}/{uniqueFileName}";
    }
}