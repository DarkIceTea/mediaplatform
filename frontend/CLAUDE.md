# Frontend notes (frontend/)

Angular 22, standalone components only (no NgModules); each component has separate `.html/.scss/.spec.ts` files and uses `inject()`. Tests: Vitest (`npm test`; single file: `npx ng test --include <path>.spec.ts`). Formatting: Prettier.

## State: signals, not BehaviorSubject
Shared state lives in services as signals: `AuthService.currentUser`, `ChatService.messages` / `activeUser`. Components read them directly. Use `signal`/`computed` for new state instead of RxJS `BehaviorSubject`; keep RxJS for HTTP streams.

## Auth
- `interceptors/credentials.intersceptor.ts` (filename misspelled) is a functional `HttpInterceptorFn` that adds `withCredentials: true` so the HttpOnly `jwt` cookie is sent through the gateway (`http://localhost:5000`, hardcoded in services).
- Never read or store tokens in `localStorage`. The current `logout()` only clears local state and doesn't call `/api/auth/logout`, so the cookie survives.
- `app.config.ts` registers `provideHttpClient()` twice; the interceptor must be on the effective registration.

## Chat (SignalR)
`ChatService` builds the hub connection with `@microsoft/signalr`, `.withUrl(`${gatewayUrl}/hubs/chat`, { withCredentials: true })`. Without `withCredentials`, the cookie isn't sent and the hub returns 401.

## Optimistic UI for likes
`post-detail` and `video-watch` save `previousState`, update the signal immediately, then reconcile with the server's `{ isLiked, likesCount }`. On error they roll back with `set(previousState)`. Follow the same pattern for new toggle-style actions.
