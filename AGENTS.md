# Repository Guidelines

## Project Structure & Module Organization
`KrasCore` is a Unity package split by assembly and execution context:
- `Runtime/`: runtime utilities, data types, extensions, rendering helpers, and ECS-related code.
- `Editor/`: editor-only tools, drawers, menu items, and toolbar integrations.
- `Runtime/*.asmdef` and `Editor/*.asmdef`: assembly boundaries (`KrasCore`, `KrasCore.Editor`, plus `ArtificeToolkit` variants).
- `.meta` files are part of source control and must be kept in sync with any added/moved/deleted assets.

Prefer placing new code in an existing feature folder (`Extensions`, `Utils`, `Data`, `Tools`) before creating new top-level folders.

## Build, Test, and Development Commands
This repo is a Unity package (not a standalone app), so commands run from a host Unity project:
- `"<UnityExe>" -batchmode -projectPath "<HostProject>" -quit -logFile -`
  - Imports/compiles package code and exits (quick CI smoke check).
- `"<UnityExe>" -batchmode -projectPath "<HostProject>" -runTests -testPlatform EditMode -testResults TestResults/EditMode.xml -quit`
  - Runs EditMode tests.
- `"<UnityExe>" -batchmode -projectPath "<HostProject>" -runTests -testPlatform PlayMode -testResults TestResults/PlayMode.xml -quit`
  - Runs PlayMode tests.

`package.json` is UPM metadata; there are no npm scripts in this repository.

## Coding Style & Naming Conventions
- Language: C# (Unity). Use 4-space indentation and Allman braces, consistent with current files.
- Use `PascalCase` for types, methods, and filenames.
- Use `var` for variable declarations whenever possible; use explicit types only where `var` is not possible.
- Keep helper naming consistent: `*Extensions`, `*Utils`, `*SystemGroup`, etc.
- Runtime namespaces should stay under `KrasCore`; editor code under `KrasCore.Editor`.
- Keep editor-only APIs inside `Editor/` assemblies; do not leak `UnityEditor` references into runtime assemblies.

## Testing Guidelines
There is currently no committed `Tests/` directory. For new coverage:
- Add `Tests/Editor` and `Tests/Runtime` (or `Tests/PlayMode`) with dedicated test asmdefs.
- Name files `FeatureNameTests.cs`.
- Prefer behavior-focused test names: `MethodName_State_ExpectedResult`.
- Run both EditMode and PlayMode tests before opening a PR when relevant.

## Commit & Pull Request Guidelines
Local git history for this submodule is not available in this checkout, so use clear, scoped commit messages:
- `runtime: add NativeArray helper extensions`
- `editor: fix missing script scanner null handling`

PRs should include:
- What changed and why.
- Affected folders/asmdefs.
- Validation steps (test commands or manual Unity checks).
- Screenshots/GIFs for editor UI changes.
