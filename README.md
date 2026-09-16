# File Processing API

An ASP.NET Core REST API with a small browser UI. Upload JSON files, store their parsed records in MongoDB, view processing reports, filter records, and delete stored files.

## Quick start

Install Git and Docker Desktop first. Docker Desktop must be running.

### Linux or macOS

Copy and run:

```bash
git clone https://github.com/dani-rodr/RODRIGUEZ_DANIEL_09162026.git
cd RODRIGUEZ_DANIEL_09162026
bash setup.sh
```

### Windows Command Prompt or PowerShell

Copy and run:

```bat
git clone https://github.com/dani-rodr/RODRIGUEZ_DANIEL_09162026.git
cd RODRIGUEZ_DANIEL_09162026
.\setup.bat
```

The setup script creates `.env` from `.env.example` when needed, builds the API image, and starts the API and MongoDB in the background. Existing `.env` settings are not overwritten.

After setup:

- Browser UI: http://localhost:5274/
- Swagger API: http://localhost:5274/swagger
- Health check: http://localhost:5274/health

Enter this default API key in the browser UI or use it in the API examples:

```text
local-development-key
```

The default key and MongoDB credentials are for local evaluation only. Change them in `.env` before running the setup script again. If port `5274` is already in use, change `API_PORT` in `.env`.

## Browser UI

Open http://localhost:5274/ and:

1. Enter `local-development-key`.
2. Choose one of the JSON files in `samples/`.
3. Click **Upload**.
4. Click **Load files** to view stored-file metadata.
5. Click **Select** to load records and apply Active, Name, or Value filters.
6. Click **Delete** beside a file and confirm to remove it and its records.

The UI is a simple client of the REST API. It does not access MongoDB directly. API validation and error messages are displayed in the page.

## API key

The API validates the `X-API-Key` request header with middleware.

- `/`, `/swagger`, and `/health` are public.
- Every `/api/files/*` endpoint requires `X-API-Key`.
- The Docker key is configured by `API_KEY` in `.env`.
- Local `dotnet run` uses the Development value `local-development-key`.

Example:

```bash
curl -H "X-API-Key: local-development-key" http://localhost:5274/api/files/report
```

## JSON format

Uploads must have a `.json` extension and contain an array of records with this shape:

```json
[
  {
    "id": 1,
    "name": "Item A",
    "active": true,
    "value": 75
  }
]
```

The following sample files are included in the repository:

- [`items.json`](samples/items.json): larger valid sample with 10 records
- [`items-small.json`](samples/items-small.json): small valid sample with 3 records
- [`items-special.json`](samples/items-special.json): valid sample with varied names, values, and active states
- [`items-invalid.json`](samples/items-invalid.json): intentionally malformed sample for testing API validation; it should be rejected

The three valid samples can be uploaded through the browser UI or the upload endpoint. The upload stores parsed records and file metadata; the original file is not stored.

## Endpoints

| Method | Path | Auth | Description |
| --- | --- | --- | --- |
| GET | `/` | Public | Browser UI |
| GET | `/swagger` | Public | Interactive API documentation |
| GET | `/health` | Public | Check that the API is running |
| POST | `/api/files/upload` | API key | Upload and store one JSON file |
| GET | `/api/files/report` | API key | List uploaded files and record counts |
| GET | `/api/files/{id}/records` | API key | Return records for one file, optionally filtered |
| DELETE | `/api/files/{id}` | API key | Delete one stored file and its records |

## API workflow

Run these commands from the repository folder. Upload a file and copy the `id` from the response:

```bash
curl -X POST \
  -H "X-API-Key: local-development-key" \
  -F "file=@samples/items.json" \
  http://localhost:5274/api/files/upload
```

List processed files:

```bash
curl \
  -H "X-API-Key: local-development-key" \
  http://localhost:5274/api/files/report
```

Retrieve filtered records using the returned ID:

```bash
curl -G \
  -H "X-API-Key: local-development-key" \
  --data-urlencode "active=true" \
  --data-urlencode "name=item" \
  --data-urlencode "nameComparison=Contains" \
  --data-urlencode "value=100" \
  --data-urlencode "valueComparison=GreaterThanOrEqual" \
  http://localhost:5274/api/files/FILE_ID/records
```

Delete a stored file by ID. A successful delete returns `204 No Content`:

```bash
curl -X DELETE \
  -H "X-API-Key: local-development-key" \
  http://localhost:5274/api/files/FILE_ID
```

Filters are optional and are combined with AND. If a comparison is omitted, the defaults are `Equal` for `active` and `value`, and `Contains` for `name`.

Available comparisons:

- `activeComparison`: `Equal`, `NotEqual`
- `nameComparison`: `Equal`, `Contains`, `StartsWith`
- `valueComparison`: `Equal`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`

Duplicate filenames are allowed. Each upload gets its own generated ID.

## Local development

The Development settings expect MongoDB at `localhost:27017`:

```text
Username: admin
Password: password
Database: file-processing
API key: local-development-key
```

Start MongoDB with Docker:

```bash
docker run --name file-processing-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=password \
  -d mongo:8.0
```

Run the API:

```bash
dotnet run --project src/FileProcessing.Api/FileProcessing.Api.csproj --launch-profile http
```

The local browser UI is at http://localhost:5232/. Swagger is at http://localhost:5232/swagger.

## Build and test

The solution uses .NET 10:

```bash
dotnet restore FileProcessing.slnx
dotnet build FileProcessing.slnx --configuration Release --no-restore
dotnet test FileProcessing.slnx --configuration Release --no-build --no-restore
```

## Stop and reset

Stop the containers while keeping MongoDB data:

```bash
docker compose down
```

The `mongo-data` named volume intentionally survives container deletion, so uploaded data remains after restarting the stack. To remove the containers and all MongoDB data:

```bash
docker compose down -v
```
