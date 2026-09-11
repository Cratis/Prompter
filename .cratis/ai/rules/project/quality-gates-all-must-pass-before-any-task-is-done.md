---
applyTo: "**/*"
---

## Quality gates (all must pass before any task is "done")

```bash
dotnet build --configuration Release   # zero warnings, zero errors (warnings are errors in Release)
dotnet test --configuration Release
```
