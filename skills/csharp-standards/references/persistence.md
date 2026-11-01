# Persistence

Follow existing storage patterns; do not assume EF or introduce a new mechanism
without need. Use `int`, not `Guid`, for primary/foreign keys in these flows.
Commit EF migrations only when the repository actually needs them.

For simple persistence, one insertion test asserting all fields is sufficient;
do not multiply tests across fields, optional values, or enums. Database-backed
tests use the real provider and existing fixtures, never SQLite or EF InMemory
as substitutes.
