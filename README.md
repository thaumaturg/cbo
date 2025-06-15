# Competitive Bracket Organizer

## Build and Run Instructions

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later
- [PostgreSQL](https://www.postgresql.org/download/) database server

### Configuration
1. Copy `backend/Cbo.API/appsettings.Development.json` and update the `CboDb` connection string with your PostgreSQL credentials if needed:
   ```json
   "ConnectionStrings": {
     "CboDb": "Host=localhost; Port=5432; Database=cbo_db; Username=postgres; Password=yourpassword; TimeZone=UTC"
   }
   ```
2. No need to ensure the target database (`cbo_db` by default) exists in your PostgreSQL instance, ef will handle creation.

### Build
From the root directory, run:
```bash
cd backend/Cbo.API
 dotnet build
```

### Run
To start the API locally:
```bash
dotnet run
```
The API will be available at `https://localhost:5001` or `http://localhost:5000` by default.

### Database Migrations
If you add or change models, you may need to add and apply Entity Framework Core migrations:
(You may need to install the EF Core CLI tools: `dotnet tool install --global dotnet-ef`)
```bash
dotnet ef migrations add <Name>
 dotnet ef database update
```
