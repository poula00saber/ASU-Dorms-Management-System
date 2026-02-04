# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["ASU Dorms Management System/ASU Dorms Management System.api.csproj", "ASU Dorms Management System/"]
COPY ["ASUDorms.Application/ASUDorms.Application.csproj", "ASUDorms.Application/"]
COPY ["ASUDorms.Domain/ASUDorms.Domain.csproj", "ASUDorms.Domain/"]
COPY ["ASUDorms.Infrastructure/ASUDorms.Infrastructure.csproj", "ASUDorms.Infrastructure/"]

RUN dotnet restore "ASU Dorms Management System/ASU Dorms Management System.api.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/ASU Dorms Management System"
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:${PORT:-5000}

# Switch to non-root user
USER appuser

# Health check (commented out - Railway handles healthchecks via railway.json)
# HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
#   CMD curl -f http://localhost:${PORT:-5000}/swagger/index.html || exit 1

ENTRYPOINT ["dotnet", "ASU Dorms Management System.api.dll"]
