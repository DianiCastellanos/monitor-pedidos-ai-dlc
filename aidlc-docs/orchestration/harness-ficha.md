# Harness Ficha — MonitorPedidos AI

## Identity
- **Harness**: Claude Code (CLI + VSCode Extension)
- **Model**: claude-sonnet-4-6 (Claude Sonnet 4.6)
- **Provider**: Anthropic
- **Version**: Claude Code (bundled in VSCode Extension)
- **Platform**: Windows 11 (PowerShell 5.1)

## Capabilities
| Capability | Supported | Notes |
|------------|-----------|-------|
| Read/write files | Yes | Via Claude Code native tools (Read, Write, Edit) |
| Search code | Yes | Glob + Grep tools |
| Execute commands | Yes | Bash + PowerShell tools |
| Web search | Yes | Via WebSearch tool |
| Web fetch | Yes | Via WebFetch tool |
| Run Playwright tests | Yes | npx playwright test |
| Build .NET solutions | Yes | dotnet build |
| Run unit tests | Yes | dotnet test |
| Git operations | Yes | git commands via Bash/PowerShell |
| MCP client | Yes | GitHub MCP, Linear MCP (deferred) |
| Create documentation | Yes | Markdown files |
| Spawn subagents | Yes | Agent tool con subagent_type |

## Workflow
- **Framework**: AI-DLC (Adaptive Software Development Lifecycle)
- **Phases executed**: Inception (6 stages) → Construction (U1-U7 + 13 ITs) → Validate → Operations
- **Total artifacts**: 200+ files en aidlc-docs/
- **Total code**: Solución C# Blazor Server .NET 8 + suite Playwright TypeScript

## Accuracy & Reliability
- **Verificación**: `dotnet build` (0 errores), `dotnet test` (50/50), `npx playwright test` (27/27)
- **Code review**: Manual por Diana Castellanos (owner)
- **Red-teaming**: 6/6 escenarios PRD Segmento 11 validados con Playwright

## History
- **Fecha inicio**: 2026-05-20
- **MVP completado**: 2026-05-28
- **OPERATIONS activo**: 2026-06-01
- **Total sesiones**: 15+ (Estación 1 hasta Operations)
- **Logros clave**: 27 tests Playwright, 6 RT PRD validados, NOC mode, Brand Monitor Salesforce real, graceful degradation, IT6 UI nav
