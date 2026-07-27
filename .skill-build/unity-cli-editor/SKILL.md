---
name: unity-cli-editor
description: Operate Unity installations, projects, and running Unity 6+ Editor instances with the experimental Unity CLI and Unity Pipeline package. Use for installing or selecting Editors and modules, opening projects, authenticating, installing or diagnosing the Pipeline package, discovering Editor commands, executing Pipeline commands against a specific project, automating Editor builds or tests, and safely troubleshooting unity-cli or unity-pipeline workflows.
---

# Unity CLI Editor

Use `unity` for Editor lifecycle and project launch tasks. Use `unity pipeline` to install the in-project integration, then `unity command` to discover and invoke commands exposed by a running Editor.

## Operating workflow

1. Locate the Unity project. Confirm it contains `Assets/`, `Packages/`, and `ProjectSettings/`.
2. Read `ProjectSettings/ProjectVersion.txt` to determine the required Editor version.
3. Run read-only discovery before mutation:

   ```powershell
   unity --version
   unity --help
   unity editors -i --format json
   unity auth status
   ```

4. Inspect command-specific help immediately before using version-sensitive flags:

   ```powershell
   unity <command> --help
   unity pipeline --help
   unity command --help
   ```

5. Ensure the correct Editor is installed, add only required modules, and open the project.
6. For remote Editor operation, authenticate, install Pipeline into the project, wait for Unity compilation to finish, and verify installation.
7. Run `unity command` to discover commands from the running Editor. Treat its output as authoritative; never invent command names, parameters, or schemas.
8. When multiple Editors are open, always pass the explicit project path supported by the installed CLI, such as `--project-path=<path>`.
9. Execute the smallest relevant command, inspect its result and Editor state, then report what changed.

## Safety and execution rules

- Prefer inspection before changes. Do not install, upgrade, uninstall, authenticate, open GUI flows, or execute state-changing Editor commands unless the request authorizes it.
- Ask before uninstalling an Editor or applying broad project changes.
- Preserve the project’s declared Editor version unless the user explicitly requests an upgrade.
- Do not edit `Packages/manifest.json` to simulate Pipeline installation; use `unity pipeline install`.
- Expect Pipeline installation to trigger package resolution and script recompilation. Wait for compilation before discovery.
- Do not assume an Editor is ready merely because its process exists. Confirm Pipeline discovery succeeds.
- Quote Windows paths. Resolve the project path before targeting a running Editor.
- Use `--format json` for automation when supported. Parse stdout as data and stderr as diagnostics; check exit codes.
- Do not parse interactive progress bars. Exit code `0` means success, `1` general failure, and `130` cancellation.
- In non-interactive environments, provide explicit versions and paths; do not rely on prompts.
- Never expose credentials or session data. `unity auth status` is sufficient for login checks.
- Since Unity CLI is experimental and Pipeline is beta, prefer installed `--help` and live command discovery over remembered syntax.

## Common sequences

Install and open the project:

```powershell
unity install <version>
unity install-modules -e <version> -m <module-id>
unity open "<project-path>"
```

Prepare Pipeline after the Editor has opened the project:

```powershell
unity auth status
unity auth login
unity pipeline install
unity pipeline list
unity command --project-path="<project-path>"
```

Run Pipeline operations only after reading the discovered command list and the relevant help/schema. When a requested operation is absent, explain that the running Editor/package does not expose it and avoid substituting an unverified command.

## Diagnostics

- If `unity` is not found, verify installation and PATH, then reopen the terminal.
- If the required Editor is absent, compare `ProjectVersion.txt` with `unity editors -i --format json`.
- If Pipeline is missing, open the project, authenticate, run `unity pipeline install`, wait for recompilation, then run `unity pipeline list`.
- If discovery finds no Editor, verify that the target project is open and use an explicit project path.
- If compilation fails, inspect Unity Console output and project/package errors before retrying.
- If a flag fails, run help for that exact command; do not retry with guessed syntax.
- If automation fails, retain stdout, stderr, exit code, CLI version, Editor version, and target project path in the report.

## Detailed reference

Read [references/commands.md](references/commands.md) when selecting CLI commands, scripting output, migrating old Hub CLI invocations, or setting up Pipeline.
