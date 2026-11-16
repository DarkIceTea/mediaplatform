FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["MediaPlatform.ServiceDefaults/MediaPlatform.ServiceDefaults.csproj", "MediaPlatform.ServiceDefaults/"]
COPY ["MediaPlatform.Media.Api/MediaPlatform.Media.Api.csproj", "MediaPlatform.Media.Api/"]
RUN dotnet restore "MediaPlatform.Media.Api/MediaPlatform.Media.Api.csproj"

COPY . .
WORKDIR "/src/MediaPlatform.Media.Api"
RUN dotnet publish "MediaPlatform.Media.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

RUN apk add --no-cache ffmpeg

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "MediaPlatform.Media.Api.dll"]