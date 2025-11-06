# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for dependency restoration
COPY ["src/KChief.Platform.Core/KChief.Platform.Core.csproj", "src/KChief.Platform.Core/"]
COPY ["src/KChief.Platform.API/KChief.Platform.API.csproj", "src/KChief.Platform.API/"]
COPY ["src/KChief.DataAccess/KChief.DataAccess.csproj", "src/KChief.DataAccess/"]
COPY ["src/KChief.VesselControl/KChief.VesselControl.csproj", "src/KChief.VesselControl/"]
COPY ["src/KChief.AlarmSystem/KChief.AlarmSystem.csproj", "src/KChief.AlarmSystem/"]
COPY ["src/KChief.Protocols.OPC/KChief.Protocols.OPC.csproj", "src/KChief.Protocols.OPC/"]
COPY ["src/KChief.Protocols.Modbus/KChief.Protocols.Modbus.csproj", "src/KChief.Protocols.Modbus/"]

# Restore dependencies for the main API project (this will restore all dependencies)
RUN dotnet restore "src/KChief.Platform.API/KChief.Platform.API.csproj"

# Copy all source files (excluding tests and other non-essential files)
COPY ["src/", "src/"]

# Build the API project
WORKDIR "/src/src/KChief.Platform.API"
RUN dotnet build "KChief.Platform.API.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "KChief.Platform.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create directories for OPC UA certificates and database
RUN mkdir -p "/app/OPC Foundation/CertificateStores" && \
    mkdir -p /app/data

# Create non-root user for security
RUN adduser --disabled-password --gecos "" --uid 1000 appuser && \
    chown -R appuser:appuser /app
USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Copy published application
COPY --from=publish --chown=appuser:appuser /app/publish .

# Add health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "KChief.Platform.API.dll"]

