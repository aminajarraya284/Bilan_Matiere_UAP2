## Manufacturing References — Run & Test

Build and test the backend, run migrations and frontend checks.

Backend (Windows/PowerShell):

```powershell
# Build solution
dotnet build backend\BilanMatiereUAP2.sln

# Apply EF migrations (ensure connection string in appsettings points to your DB)
cd backend\src\BilanMatiereUAP2.Infrastructure
# To generate SQL script for review
dotnet ef migrations script -o AddManufacturingReferencesPersistence.sql
# Or apply migrations directly
dotnet ef database update

# Run backend tests
cd ..\..\tests\BilanMatiereUAP2.Api.IntegrationTests
dotnet test
```

Frontend:

```bash
cd frontend
npm install
npm run typecheck
npm run lint
npm run test
npm run build
```

Notes:
- The integration tests use an in-memory Sqlite instance via `ApiWebApplicationFactory`.
- Review the generated migration SQL before applying to production.
- To run import endpoints locally, start the API (`dotnet run` in `backend/src/BilanMatiereUAP2.Api`) and use the frontend flag `VITE_USE_REAL_API=true`.
