# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["AzureSmartCost.Api.csproj", "AzureSmartCost.Api/"]
COPY ["../AzureSmartCost.Shared/AzureSmartCost.Shared.csproj", "AzureSmartCost.Shared/"]
RUN dotnet restore "AzureSmartCost.Api/AzureSmartCost.Api.csproj"

# Copy source code
COPY . AzureSmartCost.Api/
COPY ../AzureSmartCost.Shared/ AzureSmartCost.Shared/

# Build the application
WORKDIR "/src/AzureSmartCost.Api"
RUN dotnet build "AzureSmartCost.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AzureSmartCost.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose ports
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:80/api/health || exit 1

ENTRYPOINT ["dotnet", "AzureSmartCost.Api.dll"]