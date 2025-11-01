# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["RobDeliveryAPI.sln", "./"]
COPY ["RobDeliveryAPI/RobDeliveryAPI.csproj", "RobDeliveryAPI/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Entities/Entities.csproj", "Entities/"]

# Restore dependencies
RUN dotnet restore "RobDeliveryAPI.sln"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/RobDeliveryAPI"
RUN dotnet build "RobDeliveryAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RobDeliveryAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create directories for data persistence
RUN mkdir -p /app/Infrastructure/DB_Storage && \
    mkdir -p /app/Uploads/Profiles && \
    mkdir -p /app/Backups

# Copy published application
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Set entrypoint
ENTRYPOINT ["dotnet", "RobDeliveryAPI.dll"]
