# Prompt for OpenAI Codex

**Goal:** Generate a production‑grade, multi‑agent, .NET‑based newsroom platform on Azure that automates end‑to‑end digital newspaper operations with *minimal human intervention*. A human (Editor‑in‑Chief) interacts via a web front end to set priorities and approvals; autonomous agents handle the rest according to the newsroom spec below.

---

## Inputs
- `newsroom_spec`: A detailed description of how a modern digital newspaper office operates (editorial workflow, CMS usage, roles, publishing/distribution, analytics/monetization, tech stack, daily ops, challenges/innovations). Use the text supplied by the user.

Paste the value of `newsroom_spec` **verbatim** in a file `docs/newsroom_spec.md` and derive requirements from it.

---

## What to Build (High Level)
Create a monorepo with:
1. **Web Front End**: ASP.NET Core + Blazor Server app (`/apps/editor-portal`) where an Editor‑in‑Chief can:
   - Define coverage priorities, create/approve pitches, set embargoes.
   - Configure paywall rules, SEO preferences, distribution channels.
   - Monitor live runs (orchestrations), approve escalations, view analytics dashboards.
2. **Agent Services** (containerized .NET 8 microservices) running in Azure Container Apps (or AKS) with Dapr sidecars:
   - `PitchAgent` – ingests EIC goals, wires/feeds; proposes pitches.
   - `AssignmentAgent` – assigns resources, timelines, distribution plan.
   - `ReporterAgent` – drafts stories (LLM‑assisted), requests assets/transcripts.
   - `FactCheckAgent` – verifies claims, sources, citations; flags risk.
   - `CopyEditAgent` – style guide, clarity, headline/deck variants.
   - `SEOPackagingAgent` – keywords, schema.org, recirculation modules.
   - `VisualsAgent` – image selection/cropping/captions; simple charts via Datawrapper/Flourish API stubs.
   - `PublishAgent` – schedules/publishes to site/app, syndication feeds.
   - `DistributionAgent` – social posts, push alerts, newsletter slots.
   - `MonetizationAgent` – paywall decisions (propensity scoring), ad ops hooks.
   - `AnalyticsAgent` – dashboards, A/B tests, success metrics + feedback loop.
   - `ModerationAgent` – comments/community triage, UGC verification signals.
3. **Orchestration Layer**: Durable Functions (.NET 8, isolated worker) implementing story state machines and sagas.
4. **Shared Libraries**: contracts, SDK, auth, telemetry, feature flags.
5. **Infrastructure as Code**: Bicep or Terraform to provision Azure resources.
6. **CI/CD**: GitHub Actions workflows for build, test, security scans, deploy.

Deliver runnable code + IaC + docs.

---

## Azure Architecture (Provision via IaC)
- **Compute**: Azure Container Apps (ACA) with Dapr enabled (or AKS if chosen via flag `USE_AKS`). Durable Functions on a Consumption or Premium plan.
- **Messaging**: Azure Service Bus (topics/subscriptions) for async events; Dapr pub/sub.
- **Storage/DB**:
  - Azure Cosmos DB (PostgreSQL or NoSQL) for content metadata, runs, users.
  - Azure Blob Storage for assets; Azure Files for temp.
  - Azure Cache for Redis for caching/queues.
  - Azure Cognitive Search (indexing published content for site search).
- **AI**: Azure OpenAI (configurable model names) for LLM tasks; Azure AI Content Safety for moderation.
- **Identity**: Microsoft Entra ID (OAuth2/OpenID Connect). Roles: `EditorInChief`, `ManagingEditor`, `Producer`, `Observer`.
- **Observability**: Azure Application Insights; Log Analytics; dashboard workbook.
- **Networking/Security**: Private endpoints where possible; Key Vault for secrets; WAF (Front Door) + CDN.
- **Content Delivery**: Azure Front Door + CDN for the public site.

Provide a `infra/main.bicep` (or Terraform) with parameters and sensible defaults.

---

## Domain Model & Contracts
Create a shared project `/libs/Newsroom.Contracts` with C# records:
```csharp
public enum StoryStage { Pitched, Assigned, Reporting, Drafting, FactChecking, CopyEdit, Packaging, ReadyToPublish, Scheduled, Published, Distributed, Archived, Rework }

public record StoryId(Guid Value);
public record AssetId(Guid Value);

public record StoryPitch(
  StoryId StoryId,
  string Slug,
  string HeadlineIdea,
  string Angle,
  string Beat,
  string[] Keywords,
  DateTimeOffset? EmbargoUntil,
  string[] Sources,
  string? Rationale,
  int Priority);

public record Assignment(
  StoryId StoryId,
  string Desk,
  string Assignee,
  DateTimeOffset Due,
  string[] RequiredAssets);

public record Draft(
  StoryId StoryId,
  string Hed,
  string Dek,
  string BodyMarkdown,
  string[] Tags,
  string[] Links,
  bool RequiresLegalReview);

public record FactCheckRequest(StoryId StoryId, string[] Claims, string DraftText);
public record FactCheckResult(StoryId StoryId, bool Pass, string[] Flags, string ReportMarkdown);
public record CopyEditRequest(StoryId StoryId, string Text);
public record CopyEditResult(StoryId StoryId, string Text, string[] HeadlineVariants);
public record PackagingRequest(StoryId StoryId, string Text, string[] Keywords);
public record PackagingResult(StoryId StoryId, string SeoTitle, string SeoDescription, object SchemaOrgJsonLd, string FeaturedImageUrl);
public record PublishRequest(StoryId StoryId, DateTimeOffset? ScheduleAt);
public record DistributionPlan(StoryId StoryId, string[] Channels, string[][] Posts);

public record MessageEnvelope(
  Guid Id,
  string Type,
  string Role,
  string CorrelationId,
  int Priority,
  DateTimeOffset CreatedAt,
  object Payload);
```
Add JSON schema equivalents under `/libs/Newsroom.Contracts/schemas` and validators with FluentValidation.

---

## Messaging Topology (Service Bus + Dapr)
Topics: `pitches`, `assignments`, `drafts`, `factcheck`, `copyedit`, `package`, `publish`, `distribute`, `analytics`, `moderation`, `errors`.
- Each agent subscribes to the relevant topic and publishes next‑stage events.
- Use dead‑letter queues; poison handling to `errors` with correlation IDs.
- Include message reordering & de‑duplication patterns.

---

## Orchestration (Durable Functions)
Create orchestrations:
- `StoryOrchestrator` (per story):
  1. Receive `StoryPitch` (manual or from `PitchAgent`).
  2. Call Assignment activity; wait for `Assignment` event.
  3. Fan‑out to `ReporterAgent` → `FactCheckAgent` → `CopyEditAgent` → `SEOPackagingAgent`.
  4. Await `EditorInChief` approval only when required by policy (high risk/legal flags);
     otherwise auto‑advance.
  5. Trigger `PublishAgent` → `DistributionAgent`.
  6. Emit telemetry to `AnalyticsAgent` and feedback loops (A/B headline updates).
- Support compensating actions (unpublish/retract), timeouts, retries, and human escalation signals.

---

## Agents (Patterns & Base Class)
Create `/libs/Newsroom.Agents` with an `AgentBase` providing:
- Service Bus/Dapr pub‑sub helpers
- Azure OpenAI client wrapper with retry/backoff and prompt templates
- Structured logging + OpenTelemetry spans
- Policy hooks (rate limits, content safety)

Each agent project (`/services/<AgentName>`) implements a handler like:
```csharp
public sealed class FactCheckAgent : AgentBase {
  protected override async Task HandleAsync(MessageEnvelope env, CancellationToken ct) {
    var req = env.Payload as FactCheckRequest ?? throw new();
    // 1) Extract claims with LLM, 2) verify via web search/bing connector stubs, 3) produce report
    var result = await VerifyAsync(req, ct);
    await PublishAsync("factcheck.results", new FactCheckResult(req.StoryId, result.Pass, result.Flags, result.ReportMarkdown));
  }
}
```

### Prompt Templates (store in `/prompts/*`)
- `reporter_agent.md` (drafting from notes + newsroom style)
- `factcheck_agent.md` (claim extraction, citation requirements)
- `copyedit_agent.md` (house style, AP, inclusive language, clarity)
- `seo_agent.md` (search intent, schema.org, internal links)
- `distribution_agent.md` (platform‑native copy, tone, limits)


---

## Editor Portal (Blazor Server)
Features:
- **Dashboard**: active stories, stage progress, SLA timers, flags.
- **Pitch Board**: create/import pitches; accept/decline; priorities.
- **Policy Center**: set paywall meter, risk thresholds requiring human review, tone/style toggles, SEO rules, channels.
- **Live Runs**: visualize a story’s orchestration (swimlane), step logs, retry/override controls.
- **Analytics**: real‑time (Chartbeat/GA4 stub) + historical (engagement, conversions). Include headline A/B control.
- **Identity & RBAC**: Entra ID; show/hide controls by role.

---

## CMS & Publishing Abstraction
Create `/libs/Newsroom.Cms` with an interface and two adapters:
```csharp
public interface ICmsPublisher {
  Task<string> CreateOrUpdateAsync(Draft draft, PackagingResult pkg, CancellationToken ct);
  Task ScheduleAsync(string cmsId, DateTimeOffset when, CancellationToken ct);
  Task PublishNowAsync(string cmsId, CancellationToken ct);
}
```
- Implement `WordPressVipPublisher` (REST) and `ArcXpPublisher` (GraphQL) **stubs** with clear extension points.
- Provide `SyndicationService` for RSS/Atom and Apple/Google News feeds.

---

## Data & Search
- Cosmos DB containers: `stories`, `runs`, `users`, `assets`, `policies`.
- Cognitive Search index: `published_content` with fields for SEO, tags, embeddings.
- Include EF Core migrations for Postgres variant (if selected by `DB_ENGINE=pg`).

---

## Monetization & Paywall
- Implement `MonetizationAgent` rules engine (NRules or simple rules):
  - Metered vs. hard paywall; topic exceptions; trial offers.
  - Churn/propensity model stub (scored via Azure OpenAI embeddings or placeholder).
- Ad ops hooks: Google Ad Manager and Prebid **interfaces**.

---

## Content Safety & Compliance
- Integrate Azure AI Content Safety for toxicity/violence/sexual content analysis in `ReporterAgent` and `ModerationAgent`.
- Log decisions and allow human overrides; retain audit trail in `runs`.

---

## Site Search & Recirculation
- Build `RecirculationService` to compute related stories using embeddings + tags.
- Expose GraphQL API (`/apis/read`) for front‑end consumption and external partners.

---

## Automation & Schedules
- Timer triggers: morning pitch harvest; evergreen resurfacing; newsletter assembly; A/B headline winner selection.

---

## Testing & Quality Gates
- Unit/integration tests with xUnit; use Azurite & Service Bus emulator in CI.
- Contract tests for message schemas.
- Load tests (k6) scripts in `/tests/load`.
- Linting (dotnet format), SAST (CodeQL), dependency scan (OSV‑Scanner).

---

## CI/CD (GitHub Actions)
- `build.yml`: restore, build, test, publish artifacts, containerize.
- `deploy_infra.yml`: deploy Bicep/Terraform to Azure.
- `deploy_apps.yml`: push images to ACR, deploy to ACA/AKS; run smoke tests.

---

## Configuration & Secrets
- App settings via `appsettings.json` + Key Vault references.
- Environment variables for model names, endpoints, keys.
- Feature flags (Azure App Configuration) to toggle agents on/off.

---

## Deliverables
- Running monorepo with solution files and projects:
```
/README.md
/docs/newsroom_spec.md
/infra/ (bicep or terraform)
/apps/editor-portal/ (Blazor Server)
/services/PitchAgent/
/services/AssignmentAgent/
/services/ReporterAgent/
/services/FactCheckAgent/
/services/CopyEditAgent/
/services/SEOPackagingAgent/
/services/VisualsAgent/
/services/PublishAgent/
/services/DistributionAgent/
/services/MonetizationAgent/
/services/AnalyticsAgent/
/services/ModerationAgent/
/libs/Newsroom.Contracts/
/libs/Newsroom.Agents/
/libs/Newsroom.Cms/
/apis/orchestrations/ (Durable Functions)
/.github/workflows/
/tests/
```

Include starter code (controllers, handlers, DI, configuration), Dockerfiles, and Helm/Bicep manifests.

---

## Acceptance Criteria
1. **Local dev**: `docker compose up` starts Service Bus emulator, Azurite, all agents, and the Blazor portal.
2. **Cloud deploy**: One‑click `infra` + `deploy_apps` succeeds on Azure; portal reachable behind Azure Front Door URL.
3. **Happy path demo**: From the portal, create a pitch → system auto‑assigns → generates draft → fact‑checks → copy‑edits → packages → schedules → publishes (mock CMS) → distributes to mock channels → analytics recorded.
4. **Human‑in‑the‑loop**: Risky claims trigger approval hold visible in portal; EIC can override.
5. **Observability**: Application Insights shows traces for every stage; dashboard workbook renders KPIs.
6. **Docs**: `README.md` with setup, `.env.sample`, and runbooks for ops.

---

## Coding Conventions
- .NET 8, C# 12, nullable enabled, async/await, DI with `Microsoft.Extensions.*`.
- Minimal APIs for services; health checks; OpenAPI/Swagger.
- Strict telemetry: request IDs, correlation IDs, story IDs propagate via headers.
- Resiliency: Polly policies (retry, circuit breaker, timeout) for all I/O.

---

## Starter Scaffolding Examples
**Minimal API service template** (`Program.cs`):
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
// add ServiceBus, Dapr, Contracts, AgentBase, KeyVault, AppConfig, AppInsights...
var app = builder.Build();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapOpenApi();
app.Run();
```

**Durable Functions Orchestrator** (`StoryOrchestrator.cs`):
```csharp
[Function("StoryOrchestrator")]
public static async Task Run([OrchestrationTrigger] TaskOrchestrationContext ctx) {
  var input = ctx.GetInput<StoryPitch>();
  var assignment = await ctx.CallActivityAsync<Assignment>("AssignActivity", input);
  var draft = await ctx.CallActivityAsync<Draft>("ReporterActivity", assignment);
  var fc = await ctx.CallActivityAsync<FactCheckResult>("FactCheckActivity", draft);
  if (!fc.Pass) await ctx.WaitForExternalEvent("EICApproval");
  var copy = await ctx.CallActivityAsync<CopyEditResult>("CopyEditActivity", draft);
  var pkg = await ctx.CallActivityAsync<PackagingResult>("PackagingActivity", copy);
  await ctx.CallActivityAsync("PublishActivity", pkg);
}
```

---

## How to Use This Prompt
1. Insert `newsroom_spec` into `docs/newsroom_spec.md`.
2. Generate the full repository with the structure and code described.
3. Produce IaC templates, GitHub Actions, and seeded configuration files.
4. Output clear RUN instructions for local and Azure.

**Non‑goals:** Building real integrations to CMS/ad servers/social networks—use clean interface abstractions and mocked providers with extension points and sample env vars.

---

## Final Instruction to Codex
> Build the entire monorepo, code, IaC, and documentation as specified above, using `newsroom_spec` to drive defaults and prompts for the agents. Optimize for minimal human intervention with safe human overrides. Ensure the system compiles and runs locally via Docker Compose and deploys to Azure via the provided GitHub Actions and IaC.

