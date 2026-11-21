# Multi-stage production build for SportShop.Api
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy directory-level build configs
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Copy project files for caching restore layer
COPY ["src/SportShop.Domain/SportShop.Domain.csproj", "src/SportShop.Domain/"]
COPY ["src/SportShop.Application/SportShop.Application.csproj", "src/SportShop.Application/"]
COPY ["src/SportShop.Infrastructure/SportShop.Infrastructure.csproj", "src/SportShop.Infrastructure/"]
COPY ["src/SportShop.Api/SportShop.Api.csproj", "src/SportShop.Api/"]

RUN dotnet restore "src/SportShop.Api/SportShop.Api.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/SportShop.Api"
RUN dotnet publish "SportShop.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Production runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck & create upload directory with non-root ownership
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/wwwroot/uploads/products \
    && chown -R $APP_UID:$APP_UID /app

# Switch to non-root user (app UID 1654)
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

HEALTHCHECK --interval=15s --timeout=5s --retries=3 --start-period=10s \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "SportShop.Api.dll"]
