# SimlifiezYaml

Enterprise-ready Azure DevOps Pipeline Builder for cloud, on-prem, and hybrid environments.

## Features

- **Variable groups** — pipeline, stage, and environment scope
- **Azure Key Vault** — `AzureKeyVault@2` pre-job integration
- **Environment approvals** — `environment:` references with UI guidance
- **Artifacts** — pipeline, build, zip, Docker, NuGet publish/download
- **Rollback** — IIS, Windows Service, file share, App Service slots, Docker
- **Health checks** — HTTP, IIS app pool, Windows service, port, custom PowerShell
- **Notifications** — Teams, email placeholder, custom webhook (via variables only)
- **IaC** — Terraform, Bicep, ARM, PowerShell
- **Deployment strategies** — standard, rolling, blue-green, canary, slot swap
- **Governance validation** — approvals, secrets, health checks, naming, forbidden tasks
- **Repo scanning** — detect stack and suggest templates
- **Pipeline explainability** — human-readable task descriptions
- **Agent diagnostics** — self-hosted agent PowerShell checks
- **Secrets governance** — plaintext secret detection
- **Template marketplace** — internal catalog by category

## Solution structure

```
src/SimlifiezYaml.Core/     Models, services, modular stage generators
src/SimlifiezYaml.Web/       Blazor Server 15-step wizard UI
tests/SimlifiezYaml.Core.Tests/
```

## Run

```bash
dotnet build
dotnet test
dotnet run --project src/SimlifiezYaml.Web
```

Open `https://localhost:5xxx` and use the wizard to generate `azure-pipelines.yml`.

## Architecture

Modular generators (not a monolithic YAML builder):

- `BuildStageGenerator`, `TestStageGenerator`, `ArtifactStageGenerator`, `DeploymentStageGenerator`
- `RollbackStepGenerator`, `HealthCheckStepGenerator`, `NotificationStepGenerator`
- `GovernanceValidator`

Pipeline flow: **Build → Test → Artifact → Deploy (test/preprod/prod) → Health Check → Rollback (on failure) → Notify**
