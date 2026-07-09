# ZPP Coding Agent Context (Mono‑repo)

Prefer to use nuget libraries over implementing yourself.  



## Non-negotiables (style + behavior)

### Tabs only
**Use tabs for indentation** in C# and other code. Do not introduce space-based indentation in existing files. Keep diffs whitespace-minimal.

### Exceptions + error propagation
- **Never swallow exceptions.**
- Prefer: **throw early**, then **log at the top-most boundary** (entry-point / consumer boundary / request handler boundary).

### Time + strings
- Always use `DateTimeOffset` (prefer `DateTimeOffset.UtcNow` unless local time is explicitly needed).
- Default to **case-insensitive** string comparisons/searches (`StringComparison.OrdinalIgnoreCase`) and prefer ZPP “safe” extensions where available.



## Constraints
- Keep code lean and production-appropriate
- Do not add unrelated features
- Add comments only where useful
- Make the app compile
- Always use tabs over spaces
- Preserve existing behavior where possible


## Change etiquette for agents

- Preserve project-local patterns and naming conventions.
- Prefer minimal diffs; avoid broad refactors unless asked.
- Put feature code near its DTO/consumer (vertical slice locality).


