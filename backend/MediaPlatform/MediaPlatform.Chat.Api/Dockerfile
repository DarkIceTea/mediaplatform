FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["MediaPlatform.ServiceDefaults/MediaPlatform.ServiceDefaults.csproj", "MediaPlatform.ServiceDefaults/"]
COPY ["MediaPlatform.Chat.Api/MediaPlatform.Chat.Api.csproj", "MediaPlatform.Chat.Api/"]
RUN dotnet restore "MediaPlatform.Chat.Api/MediaPlatform.Chat.Api.csproj"

COPY . .
WORKDIR "/src/MediaPlatform.Chat.Api"
RUN dotnet publish "MediaPlatform.Chat.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "MediaPlatform.Chat.Api.dll"]