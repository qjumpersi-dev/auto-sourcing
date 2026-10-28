# AGENTS.md

Guidance for AI agents working in this repository.

## Repository overview

- .NET 8 solution: `AutoSourcing.sln`
- Source projects live under `src/` (`AutoSourcing.Core`, `AutoSourcing.Data`, `AutoSourcing.Services`, `AutoSourcing.API`).
- Test project: `tests/AutoSourcing.Tests/AutoSourcing.Tests.csproj`.
- Default branch: `master`.

## Git workflow

- Branch from the live branch before starting work. As of October 2026 the live branch (the one the droplet deploys) is `edit/outreach-ui-polish`; `master` is weeks behind, so do NOT branch from `master` unless it has caught up. Verify which branch is live before starting.
- Use a descriptive branch name; existing branches follow the `edit/<short-description>` convention.
- Do your work on the feature branch, then open a pull request targeting the live branch.
- Never push directly to `master` or the live branch; changes land via PR.
- Do not rebase or force-push shared branches.
- Only commit, amend, push, or open PRs when explicitly asked by the user.

## Commit and PR instructions (always required)

Whenever a commit, push, or PR is involved (done by the agent or by the user), always tell the user:
- The exact directory to run each command from (usually the repo root, otherwise the specific subdirectory).
- The exact commands to type, one step at a time, including staging of specific files.
- Stage only the files belonging to the change; never stage unrelated untracked folders.

If the agent runs git itself, still print the equivalent commands so the user can reproduce them in another window.

## Telling the user where to act (always required)

Every instruction the user must carry out has to state exactly **where** to do it. Never say only "deploy", "run this", "restart it" or "rebuild" without the location.

Always use an explicit environment label, for example:
- **🌐 DROPLET (SSH)** — the production server (repo at `/root/auto-sourcing`).
- **🖥️ YOUR PC → PowerShell** — local machine, with the exact working directory.
- **🖥️ YOUR PC → File Explorer / editor**.
- **🌐 BROWSER** — name the site/URL, and say whether to hard-refresh (Ctrl+Shift+R).
- **Twilio Console / Azure Portal / Teams admin center / Scotty / GitHub** — name the console and the screen/tab.

Also always give the exact working directory for any command, and name the affected container or service (e.g. `--build web` versus `--build api web`).

When a command must run in a specific directory, put the explicit `cd <path>` as the **first line of the command block itself** — do not only mention the directory in prose. The user is not a developer: every step must be copy-pasteable exactly as written, with no assumptions about where they already are.

## Build and test

Before committing or requesting a PR, verify the change locally:

```pwsh
dotnet build AutoSourcing.sln
dotnet test AutoSourcing.sln
```

Both must pass. If a build or test fails, fix the issue before committing; do not commit broken code. If the user has a different preferred command, prefer it and update this file.

## Secrets

- Never commit secrets, credentials, API keys, tokens, or connection strings to the repository.
- Do not log or expose secrets in code, output, or error messages.
- If a secret is already in the repo or a file looks like a secret, flag it to the user and do not stage it.
- Load configuration (API keys, connection strings) from environment variables or user secrets, not from committed files.

## Code style

- Match the existing conventions in the file and surrounding project.
- Do not add comments unless explicitly asked.
- Follow security best practices; never introduce code that exposes or logs secrets.
