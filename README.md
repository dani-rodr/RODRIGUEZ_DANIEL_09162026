# File Processing API

An ASP.NET Core API that accepts JSON files, stores the parsed records in MongoDB, and returns reports or filtered records.

## Run with Docker

Docker Desktop is the easiest way to run the API and MongoDB together.

On macOS or Linux:

```bash
bash setup.sh
```

On Windows, run `setup.bat` from the repository folder. Both scripts create `.env` from `.env.example` when it does not exist, then build and start the stack in the background. Existing `.env` settings are not overwritten.

The default API key and MongoDB credentials are for local development. Change them in `.env` when needed. The API is available at `http://localhost:5274`; open `http://localhost:5274/` for the browser page or `http://localhost:5274/swagger` for the API.

If port `5274` is already in use, change `API_PORT` in `.env` and run the setup script again. MongoDB is only available inside the Compose network. Its data is kept in the `mongo-data` volume.

The equivalent Compose command is:

```bash
docker compose up --build
```

Stop the stack with:

```bash
docker compose down
```

## Build and test

The solution uses .NET 10.

```bash
dotnet restore FileProcessing.slnx
dotnet build FileProcessing.slnx --configuration Release --no-restore
dotnet test FileProcessing.slnx --configuration Release --no-build --no-restore
```

## Run locally

The Development settings expect MongoDB at `localhost:27017` with the following local credentials:

```text
Username: admin
Password: password
Database: file-processing
API key: local-development-key
```

For example, start MongoDB with Docker:

```bash
docker run --name file-processing-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=password \
  -d mongo:8.0
```

Then run the API:

```bash
dotnet run --project src/FileProcessing.Api/FileProcessing.Api.csproj --launch-profile http
```

The local browser page is at `http://localhost:5232/`. Swagger is at `http://localhost:5232/swagger`.

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

`samples/items.json` contains a larger example. `samples/items-small.json` and `samples/items-special.json` contain smaller variants. `samples/items-invalid.json` is expected to be rejected. The upload stores the parsed records and file metadata. The original file is not stored.

## Endpoints

All `/api/files` endpoints require the `X-API-Key` header. `/health` is public.

| Method | Path | Description |
| --- | --- | --- |
| GET | `/health` | Check that the API is running |
| POST | `/api/files/upload` | Upload and store one JSON file |
| GET | `/api/files/report` | List uploaded files and record counts |
| GET | `/api/files/{id}/records` | Return records for one file, optionally filtered |

Use the `X-API-Key` field shown in Swagger, or add the header to a request. The Docker default port is used below; use `5232` for a local `dotnet run` process.

```bash
curl -H "X-API-Key: local-development-key" http://localhost:5274/api/files/report
```

## Browser workflow

Open the root page, enter the API key, and choose a JSON file. The page previews the records before upload. After uploading, load the report, select a file, and apply filters.

## API workflow

Upload a file and copy the `id` from the response:

```bash
curl -X POST \
  -H "X-API-Key: local-development-key" \
  -F "file=@samples/items.json" \
  http://localhost:5274/api/files/upload
```

Use that ID to retrieve filtered records:

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

Filters are optional and are combined with AND. If a comparison is omitted, the defaults are `Equal` for `active` and `value`, and `Contains` for `name`.

Available comparisons:

- `activeComparison`: `Equal`, `NotEqual`
- `nameComparison`: `Equal`, `Contains`, `StartsWith`
- `valueComparison`: `Equal`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`

Duplicate filenames are allowed. Each upload gets its own generated ID.
