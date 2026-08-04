# Competitive Bracket Organizer

**Live at [brackets.icu](https://brackets.icu)**

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later
- [Node.js](https://nodejs.org/) (version 22 or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (runs the PostgreSQL database)

### Environment Configuration

1. Copy `.env.example` to `.env` (gitignored) in the repo root and fill in local values:
   - `DOMAIN=http://localhost`
   - `POSTGRES_PASSWORD` - any password; it initializes the database volume on first start and must match the backend connection string in step 3
   - `JWT_KEY` — any random 64+ character string
   - `JWT_ISSUER` / `JWT_AUDIENCE` — e.g. `http://localhost:8080`

2. Copy `compose.override.yaml.example` to `compose.override.yaml` (gitignored). It publishes the database and API container ports on localhost for local development. Servers skip this step, keeping those ports internal.

3. Create `frontend/.env.local` (gitignored) with your PrimeVue license key, or an empty value if you have none (see `frontend/.env` for the expected variables):

   ```
   VITE_PRIMEUI_LICENSE_KEY=your-key-here
   ```

4. Create `backend/Cbo.API/appsettings.Development.json` (gitignored)

- connection string password should match `POSTGRES_PASSWORD`
- JWT settings (see the tracked `appsettings.json` for all expected keys)

```json
{
  "ConnectionStrings": {
    "CboDb": "Host=localhost; Port=5432; Database=cbo_db; Username=postgres; Password=yourpassword; TimeZone=UTC"
  },
  "Jwt": {
    "Key": "any random 64+ character string",
    "Issuer": "https://localhost:7053",
    "Audience": "https://localhost:7053"
  }
}
```

### Database

The database runs in Docker and is published on `localhost:5432` for local development:

```bash
docker compose up -d db
```

No need to create the `cbo_db` database. Entity Framework applies migrations (and creates the database) automatically on API startup.

### Database Migrations

If you add or change models, you may need to create and apply Entity Framework Core migrations:

```bash
# Install EF Core CLI tools if not already installed
dotnet tool install --global dotnet-ef
```

```bash
cd backend/Cbo.API
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### HTTPS Certificate Setup for Development

To enable secure communication between the Vue frontend and .NET backend, you'll need to set up an SSL certificate for localhost.

**Step 1:** Trust the .NET development certificate (may prompt for admin/sudo password):

```bash
dotnet dev-certs https --trust
```

**Step 2:** Export the certificate for the Vue dev server:

```bash
cd frontend
dotnet dev-certs https --export-path localhost.crt --format Pem --no-password
```

This creates `localhost.crt` and `localhost.key` files that Vite will automatically use.

## Build and Run

### Backend

From the root directory:

```bash
cd backend/Cbo.API
dotnet build
dotnet run
```

The API will be available at:

- **HTTPS**: `https://localhost:7053`
- **HTTP**: `http://localhost:5100`
- **API Documentation**: `https://localhost:7053/scalar/v1` (development only)

### Frontend

From the root directory:

```bash
cd frontend
npm install
npm run dev
```

The Vue application will be available at `https://localhost:5173`

### Full Application

When both backend and frontend are running:

- Navigate to `https://localhost:7053` in your browser
- The backend is configured to proxy non-API requests to the Vue dev server during development
- The frontend makes API calls to relative URLs (e.g., `/api/tournaments`) which are automatically routed to the backend
- Hot reload is enabled for both frontend (Vite) and backend (.NET)
- The application will run entirely over HTTPS

### Full Stack in Docker (production-like)

SPA and API baked into one image, behind the Caddy reverse proxy run:

```bash
docker compose up -d --build
```

The app is served at `http://localhost` (through Caddy) and `http://localhost:8080` (API container directly). The same database container and data are used as in local development.

## Deployment

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for the step-by-step guide
