# 07 — Order status history

- **Status:** Done
- **Score:** bonus **Order status history (+3)**

## Context

The `OrderStatusHistory` entity and table existed in the migration, but `OrderContext`
did not expose the `DbSet`, the API did not return the history and the details page
did not exist.

## Requirements

- Order status history — challenge §5.
- View order details — challenge §2.

## Acceptance criteria

1. **Given** a created order, **then** the history has `Pending` with the creation date.
2. **Given** a completed order, **then** the history has exactly `Pending`, `Processing`, `Completed`, in chronological order.
3. **When** `GET /orders/{id}`, **then** the response includes `statusHistory: [{ status, changedAt }]` ordered by date.
4. **Given** the details page, **then** a timeline shows each status, the local date/time and the time elapsed between steps.
5. **Given** the details page open, **when** the status changes, **then** the new entry appears in the timeline without reloading (spec 06).

## Design

- Recording is centralized in `Order.TransitionTo` (spec 01): the status cannot change without a history entry.
- Returned in `OrderResponse` (spec 02).
- `StatusTimeline` component in the frontend (spec 09).
- The history also feeds the "average processing time" metric of the AI module (spec 10).

## Tasks

- [x] 7.1 History recorded on every transition (depends on 1.3).
- [x] 7.2 `statusHistory` in `OrderResponse` and `GET /orders/{id}` (depends on 2.1).
- [x] 7.3 `StatusTimeline` component on the details page (depends on 9.4).

## Verification

- Criteria 1–2: domain and worker handler tests (spec 12).
- Criterion 3: API integration test (spec 12).
- Criteria 4–5: manual check in the frontend with Compose running.

## Implementation notes

- Verified: criteria 1–3 by automated tests and in Compose (history with Pending, Processing, Completed).
- **Pending manual browser check:** criteria 4–5 (timeline and update without reload).
