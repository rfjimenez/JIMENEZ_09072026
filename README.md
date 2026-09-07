# File Processing Service

A secure, RESTful web service built with ASP.NET Core (.NET 8) that accepts CSV file uploads,
calculates an aggregate (average and sum) over a chosen numeric column, and tracks every file processed for basic reporting.
The upload and reporting endpoints are protected by an API key.

---

## Contents

- [Features](#features)
- [Quick start](#quick-start)
- [Running with Docker](#running-with-docker)
- [Running without Docker (validated workaround)](#running-without-docker-validated-workaround)
- [Configuration](#configuration)
- [API reference](#api-reference)
- [File tracking and reporting](#file-tracking-and-reporting)
- [Testing the endpoints](#testing-the-endpoints)
- [Project layout](#project-layout)
- [Design notes](#design-notes)

---

## Features

- **ASP.NET Core 8 Web API** exposing a file-upload endpoint and a reporting endpoint.
- **API key authentication** enforced by middleware on every request except the health probe and Swagger UI.
The key is compared in fixed time and is never logged.
- **CSV processing** using CsvHelper: computes the average and sum of a named column, streams the file, and skips
(rather than fails on) unparseable rows, returning per-row warnings.
- **File tracking and reporting**: an in-memory tracker records every upload attempt (success or failure and exposes)
cumulative counters plus the most recent records.
- **Fail-fast startup** the service refuses to start if no API key is configured, so it never runs with authentication that cannot succeed.
- **Containerized** with a multi-stage Dockerfile (non-root runtime user) and a docker-compose convenience wrapper.
- Structured logging and typed, machine-readble error codes throughout.

---

## Quick start

The service requires an API key to be supplied at runtime (there is no default).
Pick one of the two paths below:

- **Docker** - see [Running with Docker](#running-with-docker).
- **Native .NET (no Docker required)** - see [Running without Docker](#running-without-docker-validated-workaround). This
path has been fully validated on the my machine.

Once running, the base URL is:
- Docker / Production: `http://localhost:8080`
- Native Development profile: `http://localhost:5062`

---

## Running with Docker

> **Please read - Docker testing caveat.**
> Due to time constraints, the container was **not tested thoroughly** by the
> author. The Docker image builds from the *same published application* that was 
> validated natively (see below), and the Dockerfile has been reviewed for
> correctness, but end-to-end container execution has not been exercised. **If
> the container does not build or start in your environment, use the
> [native .NET workaround](#running-without-docker-validated-workaround), which
> runs the identical application and has been fully validated.**

### Option A: docker compose (recommended)

```bash
# 1. Provide an API key (the .env file is git-ignored, so the key is never committed)
cp .env.example .env
#	then edit .env and set ApiKey__Value to any non-empty value
# 2. Build and run
docker compose up --build
```

The service listens on `http://localhost:8080`.

### Option B: docker build / run

```bash
docker build -t fileprocessingservice .

docker run --rm -p 8080:8080 \
  -e ApiKey__Value=api-key-for-demo \
  fileprocessingservice
```

Verify it is up (the health endpoint needs no API key):

```bash
curl http://localhost:8080/health
# {"status":"healthy"}
```

---

## Running without Docker (validated workaround)

This is the recommended fallback if Docker is unavailable or does not work in
your environment. It runs the **exact same application** the container would run
and was validated end-to-end (build, auth, CSV aggregation, and reporting all confirmed working).

**Prerequisites:** the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
(or newer - the .NET 9 SDK also builds and runs this net8.0 project).

```powershell
# From the repository root.

# 1. Restore and build
dotnet build -c Release

# 2. Run the service exactly as the container does:
#	Production environment, plain HTTP on port 8080, API key via environment variable.
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS		= "http://localhost:8080"
$env:ApiKey__Value			= "api-key-for-demo"
dotnet run -c Release --no-launch-profile
```

On Linux/macOS (bash) the run step is:

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:8080
export ApiKey__Value=api-key-for-demo
dotnet run -c Release --no-launch-profile
```

The service is now on `http://localhost:8080` - identical URL and behavior to the Docker container.
Jump to [Testing the endpoints](#testing-the-endpoints).

### Alternative: Development mode with Swagger UI

To explore the API interactively, run the default (Development) profile. Supply the key via user secrets so it is not hard-coded:

```powershell
dotnet user-secrets set "ApiKey:Value" "api-key-for-demo"
dotnet run
```

Then open `http://localhost:5062/swagger` in a browser. Swagger and `/health` are exempty from the API key check;
the upload and report calls still require the `X-Api-Key` header.

---

## Configuration

All settings can be supplied via `appsettings.json`, user secrets, or environment
variables. In environment variables, the configuration separator `:` becomes a double underscore `__`.

| Setting | Env variable | Default | Purpose |
|---|---|---|---|
| `ApiKey:Value` | `ApiKey__Value` | *(none - required)* | The accepted API key. The app fails to start if unset. |
| `ApiKey:HeaderName` | `ApiKey__HeaderName` | `X-Api-Key` | Request header the key is read from. |
| `ASPNETCORE_ENVIRONMENT` | `ASPNETCORE_ENVIRONMENT` | `Production` | `Development` enables Swagger and HTTPS redirection |
| `ASPNETCORE_URLS` | `ASPNETCORE_URLS` | `http://+:8080` (image) | Address/port Kestrel binds to. |

> A single underscore (`ApiKey_Value`) will **not** bind - the double underscore
> is required.

---

## API reference
Base URL below is `http://localhost:8080`. All endpoints except `/health` and `/swagger` require the `X-Api-Key` header.

### `GET /health`

Unauthenticated liveness probe.

```
200 OK
{ "status" : "healthy" }
```

### `POST /api/files/upload`

Uploads a CSV file and returns the average and sume of one numeric column.

- **Auth:** `X-Api-Key` header required.
- **Body:** `multipart/form-data` with the file under the key `file`.
- **Query:** `column` (optional, default `Amount`) - the header name to aggregate.
- **Limits** 10 MB maximum; `.csv` extension required.

Successful response (`200 OK`):

```json
{
  "succeeded": true,
  "errorCode": "None",
  "errorMessage": null,
  "fileName": "transactions.csv",
  "column": "Amount",
  "rowsProcessed": 5,
  "rowsSkipped": 0,
  "average": 618.50,
  "sum": 3092.50
  "durationMs": 16,
  "warnings": []
}
```

Rows whose target column is empty or non-numeric are skipped, counted in
`rowsSkipped`, and described in `warnings` rather than failing the whole file.

### `GET /api/reports`

Returns cumulative processing statistics and recent records.

-**Auth:** `X-Api-Key` header required.
-**Query:** `recent` (optional, default `20`, max `100`) = how many individual
records to include, newest first.

See [File tracking and reporting](#file-tracking-and-reporting) for the response shape.

### Error responses

Failures return a consistent JSON body with a stable, machine-readable `code`:

```json
{
  "code": "ColumnNotFound",
  "message": "Column 'Total' was not found. Available columns: Id, Description, Amount.",
  "fileName": "transactions.csv",
  "timestampUtc": "2026-09-07T13:31:54.12Z"
}
```

## File tracking and reporting

Every upload attempt - whether it succeeds, is rejected before parsing, or fails
during processing - is recorded by an in-memory tracker
(`InMemoryProcessingTracker`). This backs the `GET /api/reports` endpoint.

**What is tracked per file** (`recentFiles[]`): a unique id, file name, size in
bytes, processing timestamp (UTC), duration in milliseconds, success flag, rows
processed, rows skipped, and the error code/message when it failed.

**What is aggregated** (top-level counters): total attempts, successes, failures,
total rows processed and skipped, total bytes received, average processing
duration, tracking start time, and a breakdown of failures grouped by cause.

Example `GET /api/reports` response:

```json
{
  "totalFiles": 1,
  "successfulFiles": 1,
  "failedFiles": 0,
  "totalRowsProcessed": 5,
  "totalRowsSkipped": 0,
  "totalBytesReceived": 162,
  "averageDurationMs": 16,
  "trackingSinceUtc": "2026-09-07T13:31:54.30Z",
  "failuresByCode": {},
  "recentFiles": [
    {
      "id": "fb9acc16-e395-421d-b06b-95c6fc0bcc9f",
      "fileName": "transactions.csv",
      "sizeBytes": 162,
      "processedAtUtc": "2026-09-07T13:31:54.56Z",
      "durationMs": 16,
      "succeeded": true,
      "rowsProcessed": 5,
      "rowsSkipped": 0,
      "errorMessage": null,
      "errorCode": "None"
    }
  ]
}
```

**Note:** counters are cumulative for the life of the process and the per-file
history is capped (newest 100 retained), so a long-running service cannot grow
without bound. State is in-memory and resets on restart; swapping in a
database-backed tracker would only require registering a different
`IProcessingTracker` implementation.

---

## Testing the endpoints

A sample file is provided at `sample-data/transactions.csv` (five rows; the
average of the `Amount` column is `618.50`). With the service running on
`http://localhost:8080` and the key `api-key-for-demo`:

```bash
# Health (no key needed)
curl http://localhost:8080/health

# Upload without a key -> 401 Unauthorized
curl -i -X POST "http://localhost:8080/api/files/upload?column=Amount" \
  -F "file=@sample-data/transactions.csv"

# Upload with a wrong key -> 403 Forbidden
curl -i -X POST "http://localhost:8080/api/files/upload?column=Amount" \
  -H "X-Api-Key: wrong-key" \
  -F "file=@sample-data/transactions.csv"

# Upload with a correct key -> 200 OK, average 618.50
curl -i -X POST "http://localhost:8080/api/files/upload?column=Amount" \
  -H "X-Api-Key: api-key-for-demo" \
  -F "file=@sample-data/transactions.csv"

# Report
curl -H "X-Api-Key: api-key-for-demo" http://localhost:8080/api/reports
```

On Windows Powershell, use `curl.exe` (not the `curl` alias) so the `-F` and
`-H` flags are passed through to the real curl binary.

These exact calls were run against the natively hosted service and returned the
expected 200/401/403 results and the correct `618.50` average.

---

## Project layout

```
FileProcessingService/
  Controllers/
    FilesController.cs      # POST  /api/files/upload
    ReportsController.cs    # GET   /api/reports
  Security/
    ApiKeyMiddleware.cs     # API key enforcement
  Services/
    ICsvProcessor.cs
    CsvProcessor.cs         # CSV average/sum aggregation
    IProcessingTracker.cs
    InMemoryProcessingTracker.cs
  Models/                   # DTOs, error codes, result/report types
    ErrorResponse.cs
    FileProcessingRecord.cs
    ProcessingErrorCode.cs
    ProcessingReport.cs
    ProcessingResult.cs
  sample-data/
    transactions.csv        # sample upload
  Program.cs                # DI, middleware pipeline, fail-fast key check
  Dockerfile                # multi-stage build
  docker-compose.yml        # convenience wrapper
  .env.example              # template for the API key (copy to .env)
```

---

Design notes
- **Fail fast on misconfiguration.** Startup aborts if no API key is set, rather
  than accepting requests that could never authenticate.
- **Timing-safe key comparison.** The middleware compares keys with
  `CryptographicOperations.FixedTimeEquals` so response latency cannot be used to guess the key
  The supplied key is never written to the logs.
- **Resilient CSV parsing.** Malformed rows are skipped and reported instead of
  failing the whole upload; the warning list is capped to bound the response size.
- **Track everything.** Both accepted and rejected uploads are recorded so the
  report reflects real traffic, including the distribution of failure causes.
- **Container hardening.** The runtime image contains only the published output
  (no SDK or source) and runs as the non-root `app` user; TLS is expected to be
  terminated upstream, so the app serves plain HTTP inside the container and only
  enables HTTPS redirection in Development.