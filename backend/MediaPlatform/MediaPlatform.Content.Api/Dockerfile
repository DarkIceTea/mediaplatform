FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["MediaPlatform.ServiceDefaults/MediaPlatform.ServiceDefaults.csproj", "MediaPlatform.ServiceDefaults/"]
COPY ["MediaPlatform.Content.Api/MediaPlatform.Content.Api.csproj", "MediaPlatform.Content.Api/"]
RUN dotnet restore "MediaPlatform.Content.Api/MediaPlatform.Content.Api.csproj"

COPY . .
WORKDIR "/src/MediaPlatform.Content.Api"
RUN dotnet publish "MediaPlatform.Content.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "MediaPlatform.Content.Api.dll"]