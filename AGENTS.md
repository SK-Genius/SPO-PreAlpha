# AGENTS.md

## Purpose
Concise repository rules for coding agents working on this project.

## Code Style
- **Indentation:** Use **tabs** (`\t`) only — never spaces.
- **Line endings:** Use **Windows CRLF** (`\r\n`) for all text files.
- Do not reformat existing files to spaces or LF. Preserve current tab/CRLF formatting.
- Keep diffs minimal: only change lines that are necessary for the task.

## Tests
- **Authoritative failing tests file:** `SPO_CS/SPO_CS/FailedTests.txt`
  - Treat this file as the source of truth for the latest failing tests.
  - the test are runnin from the folder SPO_CS with the command: dotnet run --no-restore -- -o3 -p > FailingTests.txt
  - it is expected the the agent can not run the test by it's own because he is missing .net9.0 for now.

## Operational Notes
- Prefer small, self-contained changes.
- Ask for human action if a task requires unavailable tooling (e.g., running tests).
