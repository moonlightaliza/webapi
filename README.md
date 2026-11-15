# Marks API

A .NET REST API for managing students, courses, enrollments and marks. It uses PostgreSQL, Entity Framework Core and JWT bearer authentication for protected endpoints.

## Project description

This API supports a complete student marks management workflow:

- Student registration and lookup
- Course creation and listing
- Enrollment of students in courses
- Assignment and update of marks
- Teacher-only administrative actions
- Student self-access to their own profile
- Validation and conflict handling for duplicate data
- Database-enforced integrity rules

The system follows a teacher-student model: teachers manage academic records, while students can only view their own record and related information.

## Requirements

- .NET 10 SDK
- PostgreSQL, running locally or on a reachable server
- Git
- Optional: Postman, for testing the API
- Optional: pgAdmin or psql, for database management

## Run from a clean clone

The commands below assume a Bash-compatible terminal. PowerShell equivalents are given where they differ.

### 1. Clone the repository

```bash
git clone https://github.com/moonlightaliza/webapi.git
cd webapi
```

### 2. Create the PostgreSQL database

Create a database named `marks_db`:

```bash
createdb -U postgres marks_db
```

If `createdb` is not available, use psql or pgAdmin:

```bash
psql -U postgres
```

```sql
CREATE DATABASE marks_db;
\q
```

### 3. Configure database and JWT settings

Set the connection string and the JWT key as environment variables. Do not put them in any committed file.

**Linux / macOS**

```bash
export ConnectionStrings__Database="Host=localhost;Port=5432;Database=marks_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
export Jwt__Key="replace-this-with-a-long-random-secret-key-of-at-least-32-characters"
```

**Windows (PowerShell)**

```powershell
$env:ConnectionStrings__Database = "Host=localhost;Port=5432;Database=marks_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
$env:Jwt__Key = "replace-this-with-a-long-random-secret-key-of-at-least-32-characters"
```

The other settings (`Jwt:Issuer`, `Jwt:Audience`, CORS, logging) are non-secret. See `appsettings.Example.json` for every setting and its expected shape.

### 4. Install the EF Core tools and apply migrations

```bash
dotnet tool install --global dotnet-ef
```

If it is already installed, update it instead:

```bash
dotnet tool update --global dotnet-ef
```

Apply the migrations:

```bash
dotnet ef database update --project src/MarksApi/MarksApi.csproj
```

### 5. Build and run the API

```bash
dotnet restore src/MarksApi/MarksApi.csproj
dotnet build src/MarksApi/MarksApi.csproj
dotnet run --project src/MarksApi/MarksApi.csproj
```

The API typically runs on:

- HTTPS: `https://localhost:7159`
- HTTP: `http://localhost:5250`

If you see an HTTPS certificate warning, run:

```bash
dotnet dev-certs https --trust
```

Seed data (demo accounts) is created automatically on startup.

## API documentation and health check

- Interactive API reference: `https://localhost:7159/scalar`
- OpenAPI document: `https://localhost:7159/openapi/v1.json`
- Health check: `https://localhost:7159/health` (verifies database connectivity)

## Authentication

Most endpoints require a JWT bearer token. Log in with `POST /api/auth/login`.

Example request body:

```json
{
  "username": "javeria56",
  "password": "oiYwe32_09"
}
```

Send the returned token on later requests:

```http
Authorization: Bearer <token>
```

### Demo accounts (development only)

| Role | Username | Password |
| --- | --- | --- |
| Teacher | `javeria56` | `oiYwe32_09` |
| Student | `saraahmed1` | `Adscxx234_` |
| Student | `areeshasaqib23` | `DfseE45_3` |

These accounts exist only for local development and testing.

## Endpoints

All paths are relative to the API base URL.

### Authentication

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | Public | Log in and receive a JWT |

### Students

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/students` | Teacher | Create a student |
| GET | `/api/students` | Teacher | List students |
| GET | `/api/students/{id}` | Teacher or self | Get one student |
| PUT | `/api/students/{id}` | Teacher | Update a student |
| DELETE | `/api/students/{id}` | Teacher | Delete a student |

### Courses

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/courses` | Teacher | Create a course |
| GET | `/api/courses` | Authenticated | List courses |
| GET | `/api/courses/{id}` | Authenticated | Get one course |
| PUT | `/api/courses/{id}` | Teacher | Update a course |
| DELETE | `/api/courses/{id}` | Teacher | Delete a course |

### Enrollments and marks

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/courses/{courseId}/enrollments` | Teacher | Enroll a student in a course |
| GET | `/api/courses/{courseId}/enrollments` | Teacher | List enrollments for a course |
| PUT | `/api/enrollments/{id}/mark` | Teacher | Set or update a mark |
| DELETE | `/api/enrollments/{id}` | Teacher | Remove an enrollment |

List endpoints accept `page` (default 1) and `pageSize` (default 10, maximum 50). Student and course lists also accept `search` and `sort`.

## Postman

Import these files into Postman:

- `MarksApi.postman_collection.json`
- `Local.postman_environment.json`

Then:

1. Select the **Local** environment.
2. Make sure the base URL variable matches your running API address.
3. Run the login request first. It saves the token to the `token` variable used by the other requests.
4. Run the whole collection with the Collection Runner.

The environment file in the repository has no token value. Do not commit a real token.

## Smoke test

`<Replace this with how your smoke test is started, for example: dotnet run --project src/MarksApi/MarksApi.csproj -- --smoke-test>`

The smoke test:

- Runs only when requested, never on normal startup
- Makes real calls to every endpoint, including failure cases
- Checks login, student, course and enrollment creation, mark updates, duplicate conflicts (409) and forbidden access (403)
- Reads credentials from configuration, with no secrets in code
- Cleans up the data it creates
- Prints clear results and exits with code `0` when all checks pass

## Design decisions

### 1. Why 409 Conflict for duplicates?

A duplicate is not malformed input. The request is valid, but it breaks a uniqueness rule. HTTP 409 Conflict describes that exactly. Examples:

- Duplicate student registration number or email
- Duplicate course code
- Duplicate enrollment for the same student and course

### 2. Why 400 Bad Request for invalid payloads?

Empty fields, missing values and out-of-range numbers are client-side validation problems, so they return 400 in the standard `ValidationProblem` format. For example, a mark outside 0 to 100, or a missing mark, returns 400 with the key `mark`.

### 3. Why block deletes that still have enrollments?

Deleting a student or course that still has enrollments would break referential integrity. The delete is rejected with 409 while related enrollment rows exist.

### 4. Why enforce rules in the database?

The database enforces business rules with unique indexes and check constraints, so the rules hold even if a caller bypasses API validation:

- Unique registration numbers, emails and course codes
- Unique student-course enrollment pair
- Mark must be between 0 and 100
- Credit hours must be between 1 and 4
- Email stored in lowercase

### 5. How duplicates and concurrent requests are handled

The code does not check first and then insert, because two simultaneous requests could both pass that check. Instead it inserts directly and relies on the unique index. If two clients create the same record at the same time, PostgreSQL rejects the second insert with a unique violation (`23505`). The service catches that exception and returns a clean 409 Conflict.

### 6. Why 404 is checked before insert for enrollments

Whether a course or student exists is not a uniqueness question, so these are checked with a simple existence query before the insert. A missing course or student returns 404.

## Database files

- `src/MarksApi/Migrations/`: EF Core migrations
- `db/schema.sql`: schema-only export of the database

To regenerate the schema export:

```bash
pg_dump --schema-only -f db/schema.sql marks_db
```

## ER diagram

`ER_Diagram.png` shows the four tables and their relationships:

- `students`
- `courses`
- `enrollments`
- `users`

Relationships:

- One student has many enrollments
- One course has many enrollments
- Each enrollment belongs to one student and one course
- One user may optionally link to one student

## Configuration example

`appsettings.Example.json` lists every setting with fake values only:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=marks_db;Username=postgres;Password=fake_password_123"
  },
  "Jwt": {
    "Key": "this-is-a-fake-secret-key-for-example-only-123456",
    "Issuer": "MarksApi",
    "Audience": "MarksApiClients",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"
    ]
  }
}
```

## Project structure

```text
.
├── src/
│   └── MarksApi/
│       ├── Data/
│       ├── Dtos/
│       ├── Services/
│       ├── Migrations/
│       ├── Program.cs
│       ├── appsettings.Example.json
│       └── MarksApi.csproj
├── db/
│   └── schema.sql
├── MarksApi.postman_collection.json
├── Local.postman_environment.json
├── ER_Diagram.png
└── README.md
```
