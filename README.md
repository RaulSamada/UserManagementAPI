# User Management API

`UserManagementAPI` is an ASP.NET Core Minimal API for TechHive Solutions. It provides the complete CRUD workflow needed to manage user records through HTTP.

## Features

- List all users and retrieve one user by ID.
- Create, update, and delete users.
- Validate required names and email addresses.
- Prevent duplicate email addresses, using case-insensitive comparison.
- Return standard HTTP status codes and Problem Details error responses.
- Catch malformed requests and unexpected exceptions at a centralized boundary.
- Protect every `/api` route with bearer-token authentication.
- Audit HTTP methods, paths, final status codes, elapsed time, and trace IDs.
- Serve user-list reads from a cached snapshot without repeated sorting or lock contention.
- Publish an OpenAPI document in the Development environment.
- Store data safely in memory for the lifetime of the application.

## Prerequisites

- .NET SDK 10
- Postman

## Run the API

No authentication token is stored in the repository. Define one locally before building and running the API:

```bash
export ApiAuthentication__Token="choose-a-local-development-token"
dotnet build UserManagementAPI/UserManagementAPI.csproj
dotnet run --project UserManagementAPI/UserManagementAPI.csproj
```

The HTTP launch profile listens at `http://localhost:5027`. The OpenAPI document is available at `http://localhost:5027/openapi/v1.json` while the application runs in Development.

> User data is intentionally stored in memory in this implementation and is cleared whenever the application stops.

## Authentication configuration

Every route under `/api` requires this header:

```http
Authorization: Bearer <token>
```

The root endpoint and the Development OpenAPI document remain public. Protected routes use the token supplied through `ApiAuthentication__Token`.

For production, supply the token through a secure secret provider and rotate it according to the deployment's security policy. The application validates this setting during startup and refuses to start with an empty token.

## Middleware pipeline

The custom middleware is registered in this order:

1. Exception handling converts malformed requests and unhandled exceptions into standardized Problem Details responses.
2. Token authentication rejects missing or invalid bearer tokens with `401 Unauthorized`.
3. Request logging records completed authorized and public requests.

Authentication failures and exception responses short-circuit the downstream pipeline, so those middleware components use the same audit logger directly. This preserves the required order while ensuring every completed request produces one accurate audit entry. Audit entries contain the HTTP method, request path, final status, elapsed time, and trace ID. Authorization headers, tokens, query strings, and request or response bodies are not logged.

## API endpoints

| Method | Route | Success | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/users` | `200 OK` | List all users. |
| `GET` | `/api/users/{id}` | `200 OK` | Retrieve one user. |
| `POST` | `/api/users` | `201 Created` | Create a user. |
| `PUT` | `/api/users/{id}` | `200 OK` | Replace a user's editable data. |
| `DELETE` | `/api/users/{id}` | `204 No Content` | Delete a user. |

Requests to `POST` and `PUT` use this JSON shape:

```json
{
  "name": "Ada Lovelace",
  "email": "ada.lovelace@techhive.example"
}
```

The API returns `400 Bad Request` for invalid input, `401 Unauthorized` for a missing or invalid token, `404 Not Found` for an unknown user ID, and `409 Conflict` when an email address is already assigned to another user. Unexpected failures return a sanitized `500 Internal Server Error` response.

In Development only, `/api/testing/unhandled-error` intentionally throws an exception so the error middleware can be verified. It requires a valid token, is excluded from OpenAPI, and is not available in production.

## Test with Postman

1. Start the API.
2. In Postman, select **Import** and choose [`postman/UserManagementAPI.postman_collection.json`](postman/UserManagementAPI.postman_collection.json).
3. Open the imported **User Management API - Middleware and Security** collection.
4. Set the collection variable `apiToken` to the same value used for `ApiAuthentication__Token`.
5. Select **Run collection** and keep the requests in their saved order.

The collection defaults to `http://localhost:5027` and inherits bearer authentication from its `apiToken` variable. It verifies missing, invalid, and valid tokens; generates a unique email address; captures the created user ID; runs the full CRUD lifecycle; exercises invalid and malformed input; verifies the sanitized `500` response; checks missing-user responses; and deletes its test user.

The saved collection was also executed with Newman, Postman's command-line collection runner. All 18 requests and 33 assertions passed with zero failures. The application console was inspected separately to confirm accurate audit entries for successful, rejected, and failed requests.
