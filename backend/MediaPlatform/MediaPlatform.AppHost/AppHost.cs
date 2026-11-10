var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var identityDb = postgres.AddDatabase("identitydb");

var redis = builder.AddRedis("redis");

var minio = builder.AddContainer("minio", "cgr.dev/chainguard/minio:latest")
    .WithEnvironment("MINIO_ROOT_USER", "miniouser")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "miniopassword")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "s3")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithVolume("miniodata", "/data");

builder.Build().Run();