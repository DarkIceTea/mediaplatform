FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["MediaPlatform.ServiceDefaults/MediaPlatform.ServiceDefaults.csproj", "MediaPlatform.ServiceDefaults/"]
COPY ["MediaPlatform.Gateway.Api/MediaPlatform.Gateway.Api.csproj", "MediaPlatform.Gateway.Api/"]
RUN dotnet restore "MediaPlatform.Gateway.Api/MediaPlatform.Gateway.Api.csproj"

COPY . .
WORKDIR "/src/MediaPlatform.Gateway.Api"
RUN dotnet publish "MediaPlatform.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "MediaPlatform.Gateway.Api.dll"]