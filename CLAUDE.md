# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Social media platform: blog posts + comments + likes, video upload/streaming, 1:1 real-time chat.
Layer-specific notes live in `backend/MediaPlatform/CLAUDE.md` and `frontend/CLAUDE.md`.

## Layout
- `backend/MediaPlatform/` — .NET microservices, `MediaPlatform.sln` (7 projects). `MediaPlatform.Api`, `.Core`, `.Infrastructure` are leftover monolith folders (only `bin/obj`, not in the sln) — ignore.
- `backend/MediaPlatform/k8s/` — Kustomize manifests (namespace `mediaplatform`); migration Jobs in `k8s/jobs/`.
- `frontend/` — Angular 22 SPA.
- `api-tests/` — Postman collection + local environment.
- `.github/workflows/ci-cd.yml` — build backend (Release) + frontend (production), validate `kubectl kustomize`, push images to `ghcr.io/darkicetea/mediaplatform-<service>:latest`.

## Architecture & service map
Local orchestration: .NET Aspire. `MediaPlatform.AppHost/AppHost.cs` starts every service plus infra: one Postgres container (`identitydb`, `blogdb`, `mediadb`, `chatdb`), Redis, RabbitMQ (`messaging`), MinIO (9000/9001). `MediaPlatform.ServiceDefaults` provides OpenTelemetry, health checks, resilience and service discovery (`AddServiceDefaults()` / `MapDefaultEndpoints()`).

| Service | Routes (via gateway) | Store |
|---|---|---|
| `Gateway` (YARP, `http://localhost:5000`) | routes/clusters + rate-limit policies in `Gateway/appsettings.json` | — |
| `Identity.Api` | `/api/auth/{register,login,logout,me,users}` | `identitydb` |
| `Content.Api` | `/api/posts`, `/api/comments` | `blogdb` + Redis cache |
| `Media.Api` | `/api/videos`, `/api/media/upload-image` | `mediadb` + MinIO + MassTransit |
| `Chat.Api` | `/api/chats/*`, SignalR `/hubs/chat` | `chatdb` + Redis backplane |

CORS lives only in the gateway: `http://localhost:4200` with credentials. The frontend hardcodes `http://localhost:5000` as the gateway URL.

## Cross-cutting conventions
- **Auth = HttpOnly JWT cookie `jwt`** (set by Identity.Api). The frontend sends `withCredentials: true` via `credentialsInterceptor`; every backend service reads the cookie in `JwtBearerEvents.OnMessageReceived` (each service has its own copy of this `AddJwtBearer` block). **Never store tokens in `localStorage`.** Claims: NameIdentifier (user Guid), Name, Email, `displayName`.
- **JWT settings are injected centrally** by AppHost as env vars `JwtSettings__Secret`, `JwtSettings__Issuer`, `JwtSettings__Audience` (in k8s, through each service's `secretGenerator`, which reads a gitignored `.<service>.secrets` file).
- **Inter-service calls use Aspire service discovery names** (`http://identity-api`, etc.), both in gateway destinations and in `HttpClient` base addresses (Chat.Api → Identity.Api).
- **Database per service**, no cross-service foreign keys: author data (`AuthorId`, `AuthorName`, `AuthorDisplayName`) is copied onto rows from JWT claims.
- **New public endpoint/service**: add a gateway route + cluster (+ `RateLimiterPolicy`) and `.WithReference(...)` in AppHost; for a new service, also add a Dockerfile, a k8s folder + kustomization entry, a migration Job and a CI matrix entry.

## Critical constraints & version pins
- All projects target `net10.0`; keep `Microsoft.AspNetCore.Authentication.JwtBearer` on `10.*`.
- **MassTransit packages stay on `8.*`.** v9+ requires a commercial license key.
- **Production/k8s migrations run only through the k8s `Job`s** (`k8s/jobs/*-migration-job.yaml`), which start the service image with `--migrate`. Startup auto-migration is Development-only; never enable it for multi-replica production pods.
- Angular needs **Node 22+**. Never use `node:20-alpine` in Dockerfiles (`frontend/dockerfile` currently uses `node:alpine`).
- k8s Jobs are immutable: `kubectl delete -f` them before re-applying.

## Key commands
```bash
# Local dev (everything via Aspire)
dotnet run --project backend/MediaPlatform/MediaPlatform.AppHost
cd frontend && npm start                          # http://localhost:4200

# Build / test
dotnet build backend/MediaPlatform/MediaPlatform.sln   # no backend test projects exist
cd frontend && npm test                          # Vitest via ng test

# Kubernetes
kubectl apply -k backend/MediaPlatform/k8s       # all workloads (does NOT include jobs/)
kubectl apply -f backend/MediaPlatform/k8s/jobs/ # run migrations (delete old Jobs first)
```

## Known gotchas
- Hardcoded secrets: the JWT secret in `AppHost.cs`, and the MinIO credentials (`miniouser`/`miniopassword`) in `MinioStorageService.cs`.
- CI references `frontend/Dockerfile`, but the file is `frontend/dockerfile` (case-sensitive on Linux runners).
- `k8s/frontend.yaml` uses `imagePullPolicy: IfNotPresent` with a `:latest` tag, so new frontend images won't be pulled.
- `/api/auth/users` is unauthenticated (Chat.Api calls it server-to-server).
