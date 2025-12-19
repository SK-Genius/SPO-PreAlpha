[![codecov](https://codecov.io/github/SK-Genius/SPO-PreAlpha/graph/badge.svg?token=4YUKMtzu8y)](https://codecov.io/github/SK-Genius/SPO-PreAlpha)
[![CodeFactor](https://www.codefactor.io/repository/github/sk-genius/spo-prealpha/badge)](https://www.codefactor.io/repository/github/sk-genius/spo-prealpha)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=SK-Genius_SPO-PreAlpha&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=SK-Genius_SPO-PreAlpha)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/SK-Genius/SPO-PreAlpha)

# SPO (PreAlpha)
SPO is a programming language focused on security, safety, robustness, and performance.

## Why SPO?
Modern software runs critical infrastructure, yet many languages still allow easy-to-miss failures (nulls, unchecked bounds).
SPO aims to make secure and robust programs the default without drowning developers in complexity - by making effects explicit, keeping dataflow analyzable, and enforcing safety through the type system.

## Goals
- Memory safety and type safety by construction.
- Avoid common runtime crashes (null pointer, out of range).
- No unmanaged side effects: mutable state is explicit and capability-based; no global mutable data.
- Make static dataflow analysis straightforward.
- Seamless mix of functional, imperative, and parallel styles.

## How security and safety are intended to be realized
- Explicit `tMaybe...` and `tResult...` types instead of implicit nulls or exceptions.
- Refinement types to make bounds and validity checks explicit and move checks from callee to caller (also for performance).
- Capabilities via mutable data/handles to control effects and prevent hidden side effects.
- Capability model example (conceptual): code must declare needed capabilities (e.g., FileRead, FileWrite, Net); the host grants them or supplies mocks/proxies. Without a capability, code cannot touch that resource.

## Example

```spo
§IMPORT {
  ...+...: §DEF ...+... € [[§INT, §INT] => §INT]
  ...-...: §DEF ...-... € [[§INT, §INT] => §INT]
}

§RECURSIVE {
  §DEF Fib... = (
    §DEF a € §INT
  ) => §IF a §MATCH {
    0 : 0
    1 : 1
    §DEF N : (.Fib (N .- 2)) .+ (.Fib (N .- 1))
  }
}

§EXPORT .Fib 6
```

## Planned killer app
A plugin-driven, secure, privacy-first social media infrastructure without servers (peer-to-peer). Untrusted extensions run under explicit capabilities. Data is hosted by users by default, unless explicitly delegated to a third-party server.

## Roadmap
- **Pre-Alpha -> Alpha:** make the compiler self-hosting (rewrite in SPO).
- **Alpha -> Beta:** complete memory management (statically optimized reference counting) and missing core features (refinement types, parallel execution, debugger).
- **Beta -> Release:** add real CPU backends (LLVM) and expand tooling + standard libraries.
