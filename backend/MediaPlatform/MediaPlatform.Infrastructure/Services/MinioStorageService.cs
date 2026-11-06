using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using MediaPlatform.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MediaPlatform.Infrastructure.Services;

public class MinioStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _endpoint;

    public MinioStorageService(IConfiguration configuration)
    {
        _endpoint = configuration["Minio:Endpoint"]!;
        _bucketName = configuration["Minio:BucketName"]!;

        var config = new AmazonS3Config
        {
            ServiceURL = _endpoint,
            ForcePathStyle = true 
        };

        _s3Client = new AmazonS3Client(
            configuration["Minio:AccessKey"],
            configuration["Minio:SecretKey"],
            config
        );
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

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = uniqueFileName,
            InputStream = fileStream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(putRequest);

        return $"{_endpoint}/{_bucketName}/{uniqueFileName}";
    }
}