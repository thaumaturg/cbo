# 🚧 Under Construction 🚧

# Competitive Bracket Organizer

## Setup

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later
- [Node.js](https://nodejs.org/) (version 18 or later)
- [PostgreSQL](https://www.postgresql.org/download/) database server

### Backend Configuration

1. Copy `backend/Cbo.API/appsettings.Development.json` and update the `CboDb` connection string with your PostgreSQL credentials if needed:
   ```json
   {
     "ConnectionStrings": {
       "CboDb": "Host=localhost; Port=5432; Database=cbo_db; Username=postgres; Password=yourpassword; TimeZone=UTC"
     }
   }
   ```
2. No need to ensure the target database (`cbo_db` by default) exists in your PostgreSQL instance - Entity Framework will handle creation.

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

#### Step 1: Trust the .NET Development Certificate

First, ensure the ASP.NET Core HTTPS development certificate is trusted:

```bash
dotnet dev-certs https --trust
```

#### Step 2: Export the Certificate for Vue

1. Open the Certificate Manager by typing "certificates" into the Windows start menu and selecting
   **Manage user certificates**
2. Navigate to **Personal → Certificates**
3. Locate the certificate with "Friendly Name" of **ASP.NET Core HTTPS development certificate**
4. Right-click it and select **All Tasks → Export...**
5. In the export wizard:
   - Choose **Yes, export the private key**
   - Select **.pfx** format and leave default options
   - Set a password (e.g., "secret" - remember this for step 3)
   - Save as `frontend/localhost.pfx`

#### Step 3: Update Vite Configuration

Open `frontend/vite.config.js` and update the passphrase to match the password you set when exporting the certificate:

```javascript
const httpsSettings = fs.existsSync(developmentCertificateName)
  ? {
      pfx: fs.readFileSync(developmentCertificateName),
      passphrase: "YOUR_ACTUAL_PASSPHRASE_HERE", // Replace with your actual password
    }
  : {};
```

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
- The frontend makes API calls to relative URLs (e.g., `/api/WeatherForecast`) which are automatically routed to the backend
- Hot reload is enabled for both frontend (Vite) and backend (.NET)
- The application will run entirely over HTTPS
