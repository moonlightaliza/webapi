# Marks API

A .NET REST API for managing students, courses, enrollments and marks. It uses PostgreSQL, Entity Framework Core and JWT bearer authentication for protected endpoints.

## Project description

This API supports a student marks management workflow:

- Student registration and lookup
- Course creation and listing
- Enrollment of students in courses
- Assignment and update of marks
- Teacher-only administrative actions
- Student self-access to their own profile (`GET /api/students/{id}` only)
- Validation and conflict handling for duplicate data
- Database-enforced integrity rules

The system follows a teacher-student model: teachers manage academic records, while a student can read only their own student record.

### Not implemented

- There are no statistics endpoints (averages, rankings, course summaries).
- There are no transcript endpoints.
- Students cannot list their own enrollments or marks. The only student self-access rule is on `GET /api/students/{id}`.

## Requirements

- .NET 10 SDK
- PostgreSQL, running locally or on a reachable server
- Git
- Optional: Postman, for testing the API
- Optional: pgAdmin or psql, for database management

## Run from a clean clone

Run every command from the repository root. The commands work in Bash and in PowerShell unless a section says otherwise. Follow the steps in order: the connection string (step 3) must be set before the migrations (step 4), or `dotnet ef` fails with a misleading error.

### 1. Clone the repository

```bash
git clone https://github.com/moonlightaliza/webapi.git
cd webapi
```

### 2. Create the database user and database

Use the `postgres` admin account only for this one-time step. The API itself never runs as `postgres`.

```bash
psql -U postgres
```

If your PostgreSQL does not run on the default port 5432, add `-p <your port>` to the command above.

On Windows, if `psql` is not recognized, open **SQL Shell (psql)** from the Start menu, or use the full path: `"C:\Program Files\PostgreSQL\<version>\bin\psql" -U postgres`.

```sql
CREATE USER marks_user WITH PASSWORD 'YOUR_MARKS_USER_PASSWORD';
CREATE DATABASE marks_db OWNER marks_user;
\q
```

### 3. Configure database, JWT and smoke-test settings

For development, store secrets with `dotnet user-secrets`. They live outside the repository, so nothing secret is ever committed.

**Replace the password and port with your own values.**

```bash
dotnet user-secrets init --project src/MarksApi/MarksApi.csproj
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=marks_db;Username=marks_user;Password=YOUR_MARKS_USER_PASSWORD" --project src/MarksApi/MarksApi.csproj
dotnet user-secrets set "Jwt:Key" "replace-this-with-a-long-random-secret-key-of-at-least-32-characters" --project src/MarksApi/MarksApi.csproj
```

Use whatever port your PostgreSQL runs on in the `Port=` part of the connection string.

The smoke test (see below) also needs four more secrets:

```bash
dotnet user-secrets set "Smoke:TeacherUsername" "javeria56" --project src/MarksApi/MarksApi.csproj
dotnet user-secrets set "Smoke:TeacherPassword" "oiYwe32_09" --project src/MarksApi/MarksApi.csproj
dotnet user-secrets set "Smoke:StudentUsername" "saraahmed1" --project src/MarksApi/MarksApi.csproj
dotnet user-secrets set "Smoke:StudentPassword" "Adscxx234_" --project src/MarksApi/MarksApi.csproj
```

**On a server**, use environment variables instead of user-secrets:

Linux / macOS

```bash
export ConnectionStrings__Database="Host=localhost;Port=5432;Database=marks_db;Username=marks_user;Password=YOUR_MARKS_USER_PASSWORD"
export Jwt__Key="replace-this-with-a-long-random-secret-key-of-at-least-32-characters"
```

Windows (PowerShell)

```powershell
$env:ConnectionStrings__Database = "Host=localhost;Port=5432;Database=marks_db;Username=marks_user;Password=YOUR_MARKS_USER_PASSWORD"
$env:Jwt__Key = "replace-this-with-a-long-random-secret-key-of-at-least-32-characters"
```

The other settings (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`, PassMark, CORS, logging) are non-secret. `appsettings.Example.json` in the repository root lists every setting with fake values.

### 4. Install the EF Core tools and apply migrations

```bash
dotnet tool install --global dotnet-ef
```

If it is already installed, update it instead:

```bash
dotnet tool update --global dotnet-ef
```

Restore the packages first. On a fresh clone `dotnet ef` fails without this:

```bash
dotnet restore src/MarksApi/MarksApi.csproj
```

Then apply the migrations (this needs the connection string from step 3):

```bash
dotnet ef database update --project src/MarksApi/MarksApi.csproj
```

### 5. Build and run the API

```bash
dotnet build src/MarksApi/MarksApi.csproj
dotnet run --project src/MarksApi/MarksApi.csproj
```

The console prints the addresses the API listens on (`Now listening on: ...`). They come from `src/MarksApi/Properties/launchSettings.json`. In this project that is:

- HTTP: `http://localhost:5250`

If you add an HTTPS profile to `launchSettings.json`, trust the development certificate with `dotnet dev-certs https --trust`.

Seed data (demo accounts) is created automatically on startup.

## API documentation and health check

With the API running on `http://localhost:5250`:

- Interactive API reference: `http://localhost:5250/scalar/v1`
- OpenAPI document: `http://localhost:5250/openapi/v1.json`
- Health check: `http://localhost:5250/health` (verifies database connectivity)

If your console prints a different address, use that one.

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

### Health

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/health` | Public | Check API and database connectivity |

List endpoints accept `page` (default 1) and `pageSize` (default 10, maximum 50). Student and course lists also accept `search` and `sort`.

## Postman

Import these files into Postman:

- `postman/MarksApi.postman_collection.json`
- `postman/Local.postman_environment.json`

Then:

1. Select the **Local** environment.
2. Make sure the base URL variable matches your running API address.
3. Run the login request first. It saves the token to the `token` variable used by the other requests.
4. Run the whole collection with the Collection Runner.

The environment file in the repository has no token value. Do not commit a real token.

## Smoke test

The smoke test needs PostgreSQL running, the migrations applied, and the four `Smoke:` secrets from step 3 set. Without them it stops at login.

Stop any other running copy of the API first, because the smoke test starts its own instance. From the repository root:

```bash
dotnet run --project src/MarksApi/MarksApi.csproj -- --smoke-test
```

The smoke test:

- Runs only when `--smoke-test` is passed, never on normal startup
- Logs in as a teacher and as a student, using credentials read from configuration (no secrets in code)
- Makes a real call to every endpoint with a valid request: health, students, courses, enrollments and mark updates
- Calls the failing cases: no token (401), student token on a teacher endpoint (403), missing id (404), invalid input (400), and duplicate student, course and enrollment (409)
- Cleans up the data it creates: all of it is named `SMOKE-...`, and it deletes the enrollment, then the student, then the course, so a second run gives the same result
- Prints `PASS` or `FAIL` for each check, then `ALL CHECKS PASSED` and exits with code `0`, or `N CHECK(S) FAILED` and exits with code `1`

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
pg_dump --schema-only -h localhost -p 5432 -U marks_user -f db/schema.sql marks_db
```

Replace `5432` with the port your PostgreSQL runs on. The command fails on a non-default port without it, and it asks for the `marks_user` password.

## ER diagram

`docs/erd.png` shows the four tables and their relationships:

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

`appsettings.Example.json` in the repository root lists every setting with fake values only, including the PassMark and `Smoke` settings. Copy the shape from that file; never put real secrets in it.

## Project structure

```text
.
├── src/
│   └── MarksApi/
│       ├── Data/
│       ├── Dtos/
│       ├── Services/
│       ├── Migrations/
│       ├── SmokeTests/
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── Program.cs
│       └── MarksApi.csproj
├── db/
│   └── schema.sql
├── docs/
│   ├── erd.png
│   └── screenshots/
├── postman/
│   ├── MarksApi.postman_collection.json
│   └── Local.postman_environment.json
├── appsettings.Example.json
└── README.md
```