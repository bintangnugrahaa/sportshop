# Multi-stage build for SportShop.Api
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

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SportShop.Api.dll"]
