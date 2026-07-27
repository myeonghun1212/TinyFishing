# Unity CLI and Pipeline command reference

## Contents

- CLI authority and scope
- Editor and project lifecycle
- Output and automation
- Unity Pipeline setup and discovery
- Hub CLI migration
- Source documentation

## CLI authority and scope

Unity CLI is experimental. Always run `unity --help`, `unity <command> --help`, and `unity <command> <subcommand> --help` against the installed version. The installed help may contain commands and flags absent from online documentation.

Unity CLI manages Editor installations, modules, project registration, project opening, authentication, and CLI upgrades. Unity Pipeline adds a package to a Unity 6+ project and exposes a running Editor through a local HTTP API and CLI-discovered commands.

## Editor and project lifecycle

| Task | Command |
|---|---|
| Show CLI version/help | `unity --version`, `unity --help` |
| Upgrade CLI | `unity upgrade` |
| Install an Editor | `unity install [version]` |
| Install with modules | `unity install <version> -m <module-id...> [--cm]` |
| List module choices | `unity install-modules -e <version> -l` |
| Add modules | `unity install-modules -e <version> -m <module-id...>` |
| List installed Editors | `unity editors -i --format json` |
| List releases | `unity editors -r --format json` |
| Register local installs | `unity editors add "<path>"` |
| Show/set default Editor | `unity editors default [version]` |
| Show/change install path | `unity install-path [options]` |
| Open project | `unity open "<project-path>"` |
| Open project shorthand | `unity "<project-path>"` |
| Manage Hub project registry | `unity projects [subcommand] [options]` |
| Show auth status | `unity auth status` |
| Sign in/out | `unity auth login`, `unity auth logout` |
| Uninstall an Editor | `unity uninstall <version>` |

Version aliases include `latest`, `lts`, `default`, and major/minor streams such as `6` or `6.5`. Use an explicit version in CI. A changeset can be supplied with `-c` when a version is not in the release list. Module IDs are version/platform dependent; list them rather than guessing.

Only Editors installed through Hub or Unity CLI can receive modules through `install-modules`.

## Output and automation

- Interactive output defaults to human-readable formatting.
- Piped output defaults to TSV.
- Prefer `--format json` or the compatibility alias `--json` for scripts.
- Data goes to stdout. Errors and diagnostics go to stderr.
- JSON errors are emitted as `{"error":"..."}` on stderr.
- Do not parse progress rendering.
- Exit codes: `0` success, `1` general error, `130` cancellation.

Capture stdout, stderr, and exit code separately in robust automation. Do not infer success from output text alone.

## Unity Pipeline setup and discovery

Prerequisites:

- Unity Editor 6.0 or later
- Unity CLI installed and available on PATH
- Project open in the Unity Editor
- Authenticated Unity account

Setup:

```powershell
Set-Location "<project-path>"
unity auth login
unity pipeline install
```

Wait for package resolution and project recompilation in the Editor. Verify:

```powershell
unity pipeline list
```

Discover commands for the Editor associated with the current directory:

```powershell
unity command
```

Disambiguate multiple running Editors:

```powershell
unity command --project-path="<project-path>"
```

The discovered command list/schema is the source of truth for operations such as builds, tests, development workflows, and custom commands. Inspect the installed help and discovery output to determine the exact invocation syntax. Do not encode assumed build/test command names in automation.

Pipeline communicates with the running Editor through a local HTTP API. A failure to discover commands can indicate a closed Editor, wrong project selection, missing package, compilation failure, or version mismatch.

## Hub CLI migration

Legacy Hub CLI syntax differs:

- Replace `"Unity Hub.exe" -- --headless <command>` with `unity <command>`.
- Replace `install -v <version>` with `install <version>`.
- Replace `install-modules --version/-v` with `--editor-version/-e`.
- Replace `editors --add` with `editors add`.
- Expect piped stdout to be TSV, errors on stderr, and cancellation exit code `130`.
- Removed flags include `--headless`, `--silent`, `--errors`, and `--logLevel`.

## Source documentation

- Unity CLI reference: https://docs.unity.com/en-us/unity-cli/unity-cli-reference
- Use the Unity CLI: https://docs.unity.com/en-us/unity-cli/use-unity-cli
- Unity Pipeline package: https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package
- Pipeline package manual: https://docs.unity3d.com/Packages/com.unity.pipeline@latest
