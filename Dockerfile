# Root-level Dockerfile
# Multi-stage build that publishes the backend project and creates a runtime image.
# This Dockerfile is placed at the repository root so you can build the backend image
# without having to cd into the backend folder.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy just the project file first to leverage Docker layer caching for restore
COPY backend/Backend.csproj backend/
RUN dotnet restore backend/Backend.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish backend/Backend.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# ASP.NET Core will serve static files from backend/wwwroot when present
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000

# The published app produces Backend.dll; run it.
ENTRYPOINT ["dotnet", "Backend.dll"]
