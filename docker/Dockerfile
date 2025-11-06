# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["KChief.MarineAutomationPlatform.sln", "./"]
COPY ["src/KChief.Platform.Core/KChief.Platform.Core.csproj", "src/KChief.Platform.Core/"]
COPY ["src/KChief.Platform.API/KChief.Platform.API.csproj", "src/KChief.Platform.API/"]
COPY ["src/KChief.DataAccess/KChief.DataAccess.csproj", "src/KChief.DataAccess/"]
COPY ["src/KChief.VesselControl/KChief.VesselControl.csproj", "src/KChief.VesselControl/"]
COPY ["src/KChief.AlarmSystem/KChief.AlarmSystem.csproj", "src/KChief.AlarmSystem/"]
COPY ["src/KChief.Protocols.OPC/KChief.Protocols.OPC.csproj", "src/KChief.Protocols.OPC/"]
COPY ["src/KChief.Protocols.Modbus/KChief.Protocols.Modbus.csproj", "src/KChief.Protocols.Modbus/"]

# Restore dependencies
RUN dotnet restore "KChief.MarineAutomationPlatform.sln"

# Copy all source files
COPY . .

# Build the solution
WORKDIR "/src/src/KChief.Platform.API"
RUN dotnet build "KChief.Platform.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "KChief.Platform.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create directory for OPC UA certificates
RUN mkdir -p /app/OPC\ Foundation/CertificateStores

EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "KChief.Platform.API.dll"]

