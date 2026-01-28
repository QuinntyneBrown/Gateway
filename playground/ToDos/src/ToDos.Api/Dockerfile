# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["playground/ToDos/src/ToDos.Api/ToDos.Api.csproj", "playground/ToDos/src/ToDos.Api/"]
COPY ["src/Gateway.Core/Gateway.Core.csproj", "src/Gateway.Core/"]

# Restore dependencies
RUN dotnet restore "playground/ToDos/src/ToDos.Api/ToDos.Api.csproj"

# Copy source code
COPY ["playground/ToDos/src/ToDos.Api/", "playground/ToDos/src/ToDos.Api/"]
COPY ["src/Gateway.Core/", "src/Gateway.Core/"]

# Build
WORKDIR "/src/playground/ToDos/src/ToDos.Api"
RUN dotnet build "ToDos.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ToDos.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ToDos.Api.dll"]
