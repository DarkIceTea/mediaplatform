var builder = DistributedApplication.CreateBuilder(args);

var jwtSecret = "super_secret_key_which_must_be_very_long_and_secure_12345!";
var jwtIssuer = "MediaPlatformApi";
var jwtAudience = "MediaPlatformClient";

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var identityDb = postgres.AddDatabase("identitydb");
var mediaDb = postgres.AddDatabase("mediadb");
var blogDb = postgres.AddDatabase("blogdb"); 
var chatDb = postgres.AddDatabase("chatdb");

var redis = builder.AddRedis("redis");

var minio = builder.AddContainer("minio", "cgr.dev/chainguard/minio:latest")
    .WithEnvironment("MINIO_ROOT_USER", "miniouser")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "miniopassword")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "s3")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithVolume("miniodata", "/data");

var identityApi = builder.AddProject<Projects.MediaPlatform_Identity_Api>("identity-api")
    .WithEnvironment("JwtSettings__Secret", jwtSecret)
    .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
    .WithEnvironment("JwtSettings__Audience", jwtAudience)
    .WithReference(identityDb);

var mediaApi = builder.AddProject<Projects.MediaPlatform_Media_Api>("media-api")
    .WithEnvironment("JwtSettings__Secret", jwtSecret)
    .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
    .WithEnvironment("JwtSettings__Audience", jwtAudience)
    .WithReference(mediaDb);

var contentApi = builder.AddProject<Projects.MediaPlatform_Content_Api>("content-api")
    .WithEnvironment("JwtSettings__Secret", jwtSecret)
    .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
    .WithEnvironment("JwtSettings__Audience", jwtAudience)
    .WithReference(blogDb)
    .WithReference(redis); 

var chatApi = builder.AddProject<Projects.MediaPlatform_Chat_Api>("chat-api")
    .WithEnvironment("JwtSettings__Secret", jwtSecret)
    .WithEnvironment("JwtSettings__Issuer", jwtIssuer)
    .WithEnvironment("JwtSettings__Audience", jwtAudience)
    .WithReference(chatDb)
    .WithReference(identityApi)
    .WithReference(redis);

var gateway = builder.AddProject<Projects.MediaPlatform_Gateway>("gateway")
    .WithReference(identityApi)
    .WithReference(mediaApi)
    .WithReference(contentApi)
    .WithReference(chatApi)
    .WithHttpEndpoint(port: 5000, name: "public");


builder.Build().Run();