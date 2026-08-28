# Competitive Bracket Organizer

**Live at [brackets.icu](https://brackets.icu)**

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later
- [Node.js](https://nodejs.org/) 22 or later
- Docker with the Compose plugin
  - Windows [Docker Desktop](https://www.docker.com/products/docker-desktop/)
  - Linux [Docker Engine](https://docs.docker.com/engine/install/)
- [mkcert](https://github.com/FiloSottile/mkcert) (see [HTTPS certificates](#https-certificates-for-development) section)

On WSL, Windows `PATH` is appended automatically. After installing, confirm you are using the Linux binaries (`which dotnet` and `which node` should not start with `/mnt/c`). WSL file watching (`dotnet watch`, Vite HMR), `npm install`, and `dotnet build` are much slower if the repo lives on `/mnt/c`. Always clone into `~/...`

#### Docker Engine (WSL and Linux / Ubuntu)

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
  https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker $USER
```

Re-enter the environment so the `docker` group applies: on Linux, log out and back in. On WSL, close terminals and run `wsl --shutdown` from Windows, then open a new WSL shell.

Current Ubuntu WSL images have systemd enabled, which is what the Docker service needs. If `docker run --rm hello-world` says the daemon is not running, ensure `/etc/wsl.conf` contains:

```ini
[boot]
systemd=true
```

then `wsl --shutdown` again.

### Environment configuration

1. Copy `.env.example` to `.env` (gitignored) in the repo root and fill in local values:

- `DOMAIN` (your domain, for local development `http://localhost`)
- `POSTGRES_PASSWORD` (any password, can generate one: `openssl rand -base64 24` _it initializes the database volume on first start and must match the backend connection string in step 4_)
- `JWT_KEY` (any random 64+ character string, can generate one: `openssl rand -hex 64`)
- `JWT_ISSUER` / `JWT_AUDIENCE` (your URL, for local development e.g. `http://localhost:8080`)

2. Copy `compose.override.yaml.example` to `compose.override.yaml` (gitignored). It publishes the database and API container ports on localhost for local development. Production servers skip this step, keeping those ports internal.

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
  },
  "Kestrel": {
    "Certificates": {
      "Default": {
        "Path": "localhost.pem",
        "KeyPath": "localhost-key.pem"
      }
    }
  }
}
```

### HTTPS certificates for development

**Step 1** Install [mkcert](https://github.com/FiloSottile/mkcert) and trust its root CA (once per machine):

**Windows** (native, and also the first half of the WSL setup):

```powershell
winget install FiloSottile.mkcert
mkcert -install
```

Run `mkcert -CAROOT` and note the folder (typically `C:\Users\<you>\AppData\Local\mkcert`).

**Linux:**

```bash
sudo apt install -y mkcert libnss3-tools
```

**WSL:** the API and Vite run in Linux. The browser runs on Windows. Both must trust the same root CA.

```bash
mkdir -p ~/.local/share/mkcert
cp /mnt/c/Users/<you>/AppData/Local/mkcert/rootCA*.pem ~/.local/share/mkcert/
```

Replace `<you>` with your Windows username. Do not run `mkcert -install` in WSL _before_ copying the Windows CA, that would create a second, untrusted root. Then:

```bash
mkcert -install
```

**Step 2** issue the leaf certificates

```bash
cd frontend
mkcert -cert-file localhost.crt -key-file localhost.key localhost 127.0.0.1 ::1
cd ../backend/Cbo.API
mkcert -cert-file localhost.pem -key-file localhost-key.pem localhost 127.0.0.1 ::1
```

Vite loads `localhost.crt` / `localhost.key` automatically (`frontend/vite.config.js`). Kestrel uses the PEM pair via the `Kestrel` section of `appsettings.Development.json`.

### Database

The database runs in Docker and is published on `localhost:5432` for local development (`compose.override.yaml` binds that port on loopback only):

```bash
docker compose up -d db
```

No need to create the `cbo_db` database. Entity Framework applies migrations (and creates the database) automatically on API startup.

WSL and Linux: `localhost:5432` from Windows tools (pgAdmin, etc.) still works. WSL forwards loopback ports to Windows.

### Database Migrations

When you add or change models, create and apply Entity Framework Core migrations:

```bash
# Install EF Core CLI tools if not already installed
dotnet tool install --global dotnet-ef
```

```bash
cd backend/Cbo.API
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

## Build and Run

### Backend

From the root directory:

```bash
cd backend/Cbo.API
dotnet build
dotnet run --launch-profile https
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

On WSL, use a Windows browser. If localhost stops forwarding after sleep/resume, `wsl --shutdown` from Windows and reopen the distro.

### Full Stack in Docker (production-like)

SPA and API baked into one image, behind the Caddy reverse proxy run:

```bash
docker compose up -d --build
```

The app is served at `http://localhost` (through Caddy) and `http://localhost:8080` (API container directly). The same database container and data are used as in local development.

## Deployment

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for the step-by-step guide
