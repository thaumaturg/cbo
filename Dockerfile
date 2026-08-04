# =============================================================================
# Stage 1: Build the Vue SPA
# =============================================================================
FROM node:24-alpine AS frontend-build
WORKDIR /src

# Copy only the manifests first so `npm ci` is cached as long as they don't change
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./

# frontend/.env.local is mounted as a BuildKit secret for this step only, so
# Vite inlines its VITE_* values without the file entering any image layer
RUN --mount=type=secret,id=frontend-env,target=/src/.env.local npm run build

# =============================================================================
# Stage 2: Build and publish the .NET API
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

# Copy only the files that affect NuGet restore so the restore layer is cached
COPY backend/Directory.Packages.props ./
COPY backend/Cbo.API/Cbo.API.csproj Cbo.API/
RUN dotnet restore Cbo.API/Cbo.API.csproj

COPY backend/ ./
# UseAppHost=false: no native launcher needed, the entrypoint uses `dotnet`
RUN dotnet publish Cbo.API/Cbo.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# =============================================================================
# Stage 3: Runtime image (only the published output, no SDK or sources)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /src/dist ./wwwroot

# Run as the non-root 'app' user built into the aspnet base image
USER $APP_UID

ENTRYPOINT ["dotnet", "Cbo.API.dll"]
