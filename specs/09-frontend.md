# 09 — Frontend (React + Tailwind)

- **Status:** Done
- **Score:** required (Frontend & UX 10%)

## Context

The details page was a copy of the create form, Tailwind was not wired (`index.css`
was a fixed precompiled CSS — new classes did not work), the API URL was hard-coded
and the Dockerfile used Node 18, which is incompatible with Vite 7.

## Requirements

- List orders in a responsive table — challenge §2.
- Create orders through a form — challenge §2.
- View order details — challenge §2.
- Visual feedback for status changes — challenge §2.
- React + TailwindCSS — challenge *Required technologies*.

## Acceptance criteria

1. **List**: on screens ≥ 768 px, a table with customer, product, amount (USD), status, creation date and a details link; on smaller screens, stacked cards with no horizontal scrolling.
2. **List**: loading (skeleton), empty (with a "Create the first order" button) and error (with "Try again") states.
3. **Create**: client-side validation (required, up to 100 characters, amount > 0), per-field errors from the API's `400`, the button disabled while submitting, and a redirect to the details page on success.
4. **Details**: order data, a Pending → Processing → Completed progress bar and the status history timeline (spec 07); a friendly 404 for unknown ids.
5. **Status feedback**: a badge colored by status, animated while `Processing`; a brief highlight of the row/card when the status changes; a toast "Order X — Completed".
6. **Real time**: "Live / Polling" indicator in the header (spec 06).
7. **Ask**: AI module page (spec 10).
8. Works the same with `npm run dev` (Vite proxy) and in Docker (nginx), with no hard-coded API URL.
9. `npm run lint` and `npm run build` with no errors.

## Design

```
frontend/src/
  main.jsx, App.jsx           # routes: /, /orders/new, /orders/:id, /ask, *
  index.css                   # @import "tailwindcss"; + @theme with brand colors
  services/api.js             # BASE = "/api"; ProblemDetails errors
  realtime/RealtimeProvider.jsx, context.js, useOrderUpdates.js
  components/StatusBadge.jsx, StatusProgress.jsx, StatusTimeline.jsx, ConnectionIndicator.jsx, Feedback.jsx, toasts/
  pages/OrdersList.jsx, CreateOrder.jsx, OrderDetails.jsx, AskOrders.jsx, NotFound.jsx
  utils/format.js             # en-US currency (USD) and dates
```

- Tailwind v4 through `@tailwindcss/vite`; removed `postcss.config.mjs`, `input.css`, `App.css`, the legacy `.eslintrc.json`, `assets/react.svg` and unused dependencies.
- `vite.config.js`: proxy `/api` → `http://localhost:5000` (prefix stripped) and `/hubs` → `http://localhost:5000` with `ws: true`.
- `nginx.conf`: `location /api/ { proxy_pass http://api:8080/; }`, `location /hubs/` with `Upgrade`/`Connection`, `try_files … /index.html`.
- Dockerfile: `node:22-alpine` to build, `nginx:1.27-alpine` at runtime.
- Accessibility: associated labels, visible focus, AA contrast, `aria-live` on toasts.
- `index.html` with `lang="en"` and the "Order Management" title.

## Tasks

- [x] 9.1 Wire Tailwind v4 and clean up template leftovers and unused dependencies.
- [x] 9.2 `services/api.js` with the `/api` base and `ProblemDetails` handling.
- [x] 9.3 Responsive list (table + cards) with loading/empty/error states.
- [x] 9.4 Rewrite the details page with progress and timeline.
- [x] 9.5 Create form with validation and per-field errors.
- [x] 9.6 Badge, change highlight, toasts, connection indicator, 404 page.
- [x] 9.7 Node 22 Dockerfile and `nginx.conf` proxying `/api/` and `/hubs/`.
- [x] 9.8 Proxy in `vite.config.js` for development.

## Verification

- `npm ci && npm run lint && npm run build`.
- Manual check in Compose (`http://localhost:3000`): create, list, view details, follow the status change; DevTools mobile mode (375 px) for criterion 1.

## Implementation notes

- ESLint gives Node globals to `*.config.js`.
- Verified: `npm run lint` and `npm run build` clean; SPA and `/api` proxy returning 200 through nginx.
- **Pending manual browser check:** visual criteria 1–7 (responsive layout, highlight, toasts).
