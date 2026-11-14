# Marks API

A .NET 10 REST API for managing students, courses, enrollments, and marks. It uses PostgreSQL, Entity Framework Core, and JWT bearer authentication.

## Requirements

Install the following before starting:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/) running locally or on a reachable server
- Git

## Run from a clean clone

The commands below assume a Bash-compatible terminal. Run them from a new, empty working directory.

### 1. Clone the repository

```bash
git clone https://github.com/moonlightaliza/webapi.git
cd webapi
```

### 2. Create a PostgreSQL database

Create a database named `marksapi` in your PostgreSQL server. For example, if `psql` is available and your local PostgreSQL user is `postgres`:

```bash
createdb -U postgres marksapi
```

If `createdb` is not available, create a database named `marksapi` using your PostgreSQL administration tool. Keep your database host, port, username, and password handy for the next step.

### 3. Configure the database and JWT key

The application needs a PostgreSQL connection string and a secret key for signing JWTs. Set them as environment variables; do not commit real passwords or keys to the repository.

```bash
export ConnectionStrings__Database='Host=localhost;Port=5432;Database=marksapi;Username=postgres;Password=YOUR_POSTGRES_PASSWORD'
export Jwt__Key='replace-this-with-a-long-random-secret-key-of-at-least-32-characters'
```

Replace `YOUR_POSTGRES_PASSWORD` with the password for your PostgreSQL user. The JWT key should be a private random value of at least 32 characters. Keep these variables set in the same terminal when running the commands below.

For PowerShell, use:

```powershell
$env:ConnectionStrings__Database = 'Host=localhost;Port=5432;Database=marksapi;Username=postgres;Password=YOUR_POSTGRES_PASSWORD'
$env:Jwt__Key = 'replace-this-with-a-long-random-secret-key-of-at-least-32-characters'
```

### 4. Install Entity Framework tooling and create the schema

Install the EF Core command-line tool if you do not already have it:

```bash
dotnet tool install --global dotnet-ef --version 10.0.14
```

If it is already installed, update it to the required version with `dotnet tool update --global dotnet-ef --version 10.0.14`.

Apply the included database migrations from the repository root:

```bash
dotnet ef database update --project src/MarksApi/MarksApi.csproj
```

### 5. Build and run the API

```bash
dotnet restore src/MarksApi/MarksApi.csproj
dotnet build src/MarksApi/MarksApi.csproj
dotnet run --project src/MarksApi/MarksApi.csproj --launch-profile https
```

The API will be available at `https://localhost:7159` and `http://localhost:5250`. If your local HTTPS development certificate is not trusted, run `dotnet dev-certs https --trust`, or use the HTTP URL for local testing. The application seeds sample data on startup after the database has been migrated.

## API documentation and health check

- Interactive API reference: [https://localhost:7159/scalar](https://localhost:7159/scalar)
- OpenAPI document: [https://localhost:7159/openapi/v1.json](https://localhost:7159/openapi/v1.json)
- Health check: [https://localhost:7159/health](https://localhost:7159/health)

The health check also verifies database connectivity. The API redirects HTTP requests to HTTPS.

## Authentication

Most endpoints require a JWT bearer token. Log in using `POST /api/auth/login` with JSON like:

```json
{
  "username": "javeria56",
  "password": "oiYwe32_09"
}
```

The seeded demo accounts are:

| Role | Username | Password |
|---|---|---|
| Teacher | `javeria56` | `oiYwe32_09` |
| Student | `saraahmed1` | `Adscxx234_` |
| Student | `areeshasaqib23` | `DfseE45_3` |

Use the returned token on protected requests as `Authorization: Bearer <token>`. Teacher-only operations are identified below. A student can retrieve their own student record; teachers can retrieve student records and lists. These credentials are for local assignment/demo use only—replace them and use appropriate secret management before deploying the API.

## Endpoints

All paths are relative to the API base URL.

| Method | Path | Access | Purpose |
|---|---|---|---|
| `POST` | `/api/auth/login` | Public | Log in and receive a JWT |
| `POST` | `/api/students` | Teacher | Create a student |
| `GET` | `/api/students` | Teacher | List students; supports `page`, `pageSize`, `search`, and `sort` query parameters |
| `GET` | `/api/students/{id}` | Teacher or that student | Retrieve a student |
| `PUT` | `/api/students/{id}` | Teacher | Update a student |
| `DELETE` | `/api/students/{id}` | Teacher | Delete a student |
| `POST` | `/api/courses` | Teacher | Create a course |
| `GET` | `/api/courses` | Authenticated | List courses; supports `page`, `pageSize`, `search`, and `sort` query parameters |
| `GET` | `/api/courses/{id}` | Authenticated | Retrieve a course |
| `PUT` | `/api/courses/{id}` | Teacher | Update a course |
| `DELETE` | `/api/courses/{id}` | Teacher | Delete a course |
| `POST` | `/api/courses/{courseId}/enrollments` | Teacher | Enroll a student in a course |
| `GET` | `/api/courses/{courseId}/enrollments` | Teacher | List a course's enrollments; supports `page` and `pageSize` |
| `PUT` | `/api/enrollments/{id}/mark` | Teacher | Set an enrollment mark |
| `DELETE` | `/api/enrollments/{id}` | Teacher | Delete an enrollment |

Request and response schemas are available in the Scalar API reference. The configured CORS policy allows `http://localhost:3000` by default.

## Troubleshooting

- **Database connection fails:** Confirm PostgreSQL is running, the `marksapi` database exists, and `ConnectionStrings__Database` matches your local connection details.
- **JWT configuration error:** Confirm `Jwt__Key` is set in the terminal running the API and is at least 32 characters long.
- **HTTPS certificate warning:** Run `dotnet dev-certs https --trust`, or use `http://localhost:5250`.
- **`dotnet ef` is not found:** Ensure the .NET global tools directory is on your `PATH`, then open a new terminal.

## Project structure

```text
src/MarksApi/
├── Data/       # Entities, EF Core DbContext, and demo data seeder
├── Dtos/       # API request and response models
├── Migrations/ # EF Core database migrations
├── Services/   # Business logic, validation, and exception handling
├── Program.cs  # Application setup and API routes
└── MarksApi.csproj
```
