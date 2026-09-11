---
applyTo: "**/*"
---

## Conventions

- Every `.cs` file starts with the two-line Cratis copyright header;
  file-scoped namespaces; `var`; primary constructors; records for data; no
  `Service`/`Manager`/`Handler`/`Exception` postfixes; custom exception types
  only; `[LoggerMessage]` logging in `*Logging.cs` partials; concepts via
  `ConceptAs<T>`.
- Specs use Cratis.Specifications BDD style: `for_<Type>/when_<behavior>/and_<condition>.cs`,
  `Establish`/`Because`/`[Fact] void should_*`. The word `when` appears only
  in `when_` folder names.
- American English everywhere.
