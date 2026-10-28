# SPO: AI Agent Entry Point

**Read this file first, then only the topic sheet you need.** Paths and statements refer to this checkout; verify details in the linked code.

## Documentation Goal and Style

- Help AI agents enter a topic quickly: understand its purpose, responsibilities, and invariants, then find the relevant code/tests with minimal context consumption.
- Write concise English. Prefer concrete terms, short bullets, tables for comparisons, and Mermaid for relationships or flows. Use brief prose when it explains a connection more clearly.
- Keep this README as the entry point; keep topic sheets focused and independently readable. Load additional detail through links as needed.
- Summarize concepts and design reasons; include consequences that matter when changing code. Preserve exact identifiers and paths for targeted searches.
- Link to source and executable fixtures for details/examples. Avoid copying implementations, full syntax references, or explanations already covered in another sheet.
- Keep current behavior, intended goals, and known limitations explicit. Maintain the relevant sheet when they change; accuracy takes priority over brevity.

## Project Goal

- SPO is a programming language for **zero-trust code**: untrusted code receives explicit, limited access rights.
- Goals: memory/type safety, traceable dataflow, visible effects, robustness, and performance.
- Functional and imperative styles; side effects through method receivers/capabilities.
- Pre-alpha: C# compiler and interpreter/VM, SPO libraries, terminal/GUI demos, VS Code tools.
- Planned: self-hosting, complete memory management, refinement types, parallelism, debugger, native backends. These are goals, not established guarantees.
- Long-term use case: secure, decentralized, privacy-first social infrastructure with untrusted plugins.

## Load Context by Task

| Task | Read |
| --- | --- |
| Find a compiler phase/file; understand responsibilities | [Architecture](01_Architecture.md) |
| Type inference, scopes, mutables, captures, zero trust, interface rights | [Typechecker and capabilities](02_Typechecker_Capabilities.md) |
| Write SPO; understand SIG, module graphs, runtime/app contracts | [SPO, modules, and runtime](03_SPO_Modules_Runtime.md) |
| Prepare changes, select tests, understand fixtures | [Workflow and tests](04_Workflow_Tests.md) |

## Essential Rules

- [AGENTS.md](../AGENTS.md) is binding; answer questions first, implement explicit implementation requests.
- Small implementations, local scopes, minimal state, immutable data, extension operations; avoid OOP layers and compatibility wrappers.
- `SPO_CS/src/` implements the language. `SPO/` contains apps, libraries, launcher, and native hosts.
- A closure's `ENV` is transitively immutable; mutable ordinary arguments are **opaque**. Access comes through `OBJ` or fresh local cells.
- Type compatibility, access rights, and runtime lifetime are separate checks.
- Handwritten IL also requires independent validation. Opcode generation assumes valid IL and checked import types.
- Run language tests from `SPO_CS/`; positive SPO E2E tests may update `.ILT` files.
- The C# host/runtime is part of the trusted computing base. This zero-trust model does not sandbox arbitrary native code.

## Sources and Freshness

- Language overview: [index.wiki](../index.wiki); syntax: [SPO](../doc/CheatSheet_SPO.wiki), [IL](../doc/CheatSheet_IL.wiki).
- Details: [MutableAccess.md](../SPO_CS/MutableAccess.md), [SIG.md](../SPO_CS/SIG.md), [API-Design.md](../SPO/API-Design.md).
- Older texts mention `Functions_SPO`/`Modules_SPO`; current directories are **`SPO_Functions`/`SPO_Modules`**, with corresponding ILT and error groups.
- Historical failure counts, migration reports, and “parser-visible” syntax do not establish current support. Check the concrete code, fixture, and validation result.
- Update these sheets when semantics, phase boundaries, or paths change; do not append test-count snapshots or change logs.