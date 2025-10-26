FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
LABEL author="Jukman-Projects"
LABEL org.opencontainers.image.title="RippleSyncBackend"
WORKDIR /src

# Copy everything
COPY . .

# Restore dependencies
RUN dotnet restore "RippleSync.sln"

# Build the solution
RUN dotnet build "RippleSync.sln" -c $BUILD_CONFIGURATION --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN dotnet publish "src/API/API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final


WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "RippleSync.API.dll"]
