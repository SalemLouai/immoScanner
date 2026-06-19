# ── Stage 1: Build the React frontend ──────────────────────────────────────
FROM node:22-alpine AS frontend-build
WORKDIR /app/frontend
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci --silent
COPY frontend/ ./
RUN npm run build

# ── Stage 2: Build the .NET backend ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dotnet-build
WORKDIR /src

# Copy solution and project files first (layer cache)
COPY ImmoScorer.sln ./
COPY ImmoScorer.Domain/ImmoScorer.Domain.csproj         ImmoScorer.Domain/
COPY ImmoScorer.Application/ImmoScorer.Application.csproj ImmoScorer.Application/
COPY ImmoScorer.Infrastructure/ImmoScorer.Infrastructure.csproj ImmoScorer.Infrastructure/
COPY ImmoScorer.Api/ImmoScorer.Api.csproj               ImmoScorer.Api/
RUN dotnet restore

# Copy source code and build
COPY ImmoScorer.Domain/       ImmoScorer.Domain/
COPY ImmoScorer.Application/  ImmoScorer.Application/
COPY ImmoScorer.Infrastructure/ ImmoScorer.Infrastructure/
COPY ImmoScorer.Api/          ImmoScorer.Api/

RUN dotnet publish ImmoScorer.Api/ImmoScorer.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Install Playwright browser binaries
RUN dotnet tool install --global Microsoft.Playwright.CLI --version 1.52.0 || true

# ── Stage 3: Runtime image ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install Playwright system dependencies (Chromium)
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
        libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 \
        libxrandr2 libgbm1 libasound2 libpango-1.0-0 libcairo2 \
        fonts-liberation curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published backend
COPY --from=dotnet-build /app/publish ./

# Copy Playwright browsers from build stage
COPY --from=dotnet-build /root/.cache/ms-playwright /root/.cache/ms-playwright

# Copy built React SPA into wwwroot for static file serving
COPY --from=frontend-build /app/frontend/dist ./wwwroot

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

ENTRYPOINT ["dotnet", "ImmoScorer.Api.dll"]
