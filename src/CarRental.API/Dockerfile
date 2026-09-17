# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CarRental.slnx", "./"]
COPY ["src/CarRental.Domain/CarRental.Domain.csproj", "src/CarRental.Domain/"]
COPY ["src/CarRental.Application/CarRental.Application.csproj", "src/CarRental.Application/"]
COPY ["src/CarRental.Infrastructure/CarRental.Infrastructure.csproj", "src/CarRental.Infrastructure/"]
COPY ["src/CarRental.API/CarRental.API.csproj", "src/CarRental.API/"]

# Restore dependencies
RUN dotnet restore "src/CarRental.API/CarRental.API.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/CarRental.API"
RUN dotnet build "CarRental.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "CarRental.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5001

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CarRental.API.dll"]
