# AGENTS.md

Guidance for AI agents working in this repository.

## Repository overview

- .NET 8 solution: `AutoSourcing.sln`
- Source projects live under `src/` (`AutoSourcing.Core`, `AutoSourcing.Data`, `AutoSourcing.Services`, `AutoSourcing.API`).
- Test project: `tests/AutoSourcing.Tests/AutoSourcing.Tests.csproj`.
- Default branch: `master`.

## Git workflow

- Branch from `master` before starting work. Use a descriptive branch name; existing branches follow the `edit/<short-description>` convention.
- Do your work on the feature branch, then open a pull request targeting `master`.
- Never push directly to `master`; changes land via PR.
- Do not rebase or force-push shared branches.
- Only commit, amend, push, or open PRs when explicitly asked by the user.

## Build and test

Before committing or requesting a PR, verify the change locally:

```pwsh
dotnet build AutoSourcing.sln
dotnet test AutoSourcing.slin
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
