## 2025-03-12 - Optimized Database Access Patterns
**Learning:** Initial implementation used `FindAsync` for existence checks and counts, which materializes whole lists into memory before checking `Count()` or `Any()` in C#. This is an O(n) operation in memory compared to O(1) in SQL. Also, N+1 role fetching in Login was identified.
**Action:** Implemented `AnyAsync`, `CountAsync`, and `FirstOrDefaultAsync` in the Generic Repository to push these operations to the database (SQL `EXISTS`, `COUNT(*)`, `LIMIT 1`). Batched role fetching in `AuthService` to eliminate the N+1 query pattern.
