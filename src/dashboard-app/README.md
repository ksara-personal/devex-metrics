# DevExMetrics · Flow Dashboard

A React dashboard for the DevExMetrics service — PR flow decomposition,
cycle/lead time trends, PR size mix, review-source breakdown, Copilot
review statistics, and author/reviewer leaderboards, sliceable by
value stream, team, and sprint.

## Run it

```bash
npm install
npm run dev
```

Then open the URL Vite prints (default `http://localhost:5173`).

## Data source

The dashboard starts in **demo mode** with realistic mock data. Click
**"⚡ Connect live API"** in the header to point it at a real
DevExMetrics instance:

- **Base URL** — e.g. `http://localhost:5100`
- **Tenant ID** — must match a tenant identifier configured in your
  service's `TenantConfigurationStore` (e.g. `tenant-1`)
- **API key** — sent as the `x-api-key` header; must match that
  tenant's `Authentication:ApiKey` setting exactly (the sample configs
  ship with the placeholder `your-api-key` — replace it in your real
  config before relying on it)
- **Sprint calendar year** — filters the `Sprints` OData query

All fetches happen client-side, straight from the browser to the
OData endpoints under `{baseUrl}/{tenantId}/odata/...`. See
`src/DevExDashboard.jsx` — the whole data layer (`odataFetch`,
`fetchTeams`, `fetchSprints`, `fetchPRMetricsRange`,
`fetchTeamAuthorMetricsBySprint`, `fetchReviewerMetricsBySprint`,
`aggregateLiveMetrics`) is at the top of the file, self-contained and
easy to swap out.

## CORS

Because the browser calls the API directly, **the DevExMetrics service
must send CORS headers** for whatever origin this dashboard runs on
(`http://localhost:5173` in dev). Something like this in
`Metrics.MCP.StreamableHTTP/Program.cs`, before `var app = builder.Build();`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevExDashboard", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

and after `app.UseRouting();`, before `app.UseAuthentication()`:

```csharp
app.UseCors("DevExDashboard");
```

`AllowAnyHeader()` matters here specifically because `x-api-key` is a
non-simple header and triggers a CORS preflight — a policy that only
allows `Content-Type` will silently block every request.

If you'd rather not touch the service's CORS policy at all, uncomment
the `proxy` block in `vite.config.js` and point the dashboard's Base
URL at `http://localhost:5173/api` instead — Vite's dev server will
forward requests to the real API same-origin, so the browser never
sees a cross-origin request.

## Known data-model gap

`ReviewerSprintMetrics` is scoped by **repository**, not team, in the
current OData schema — the reviewer leaderboard table isn't affected
by the team/value-stream filters when running in live mode. The
dashboard surfaces this as a note rather than silently mislabeling the
column.

## Project structure

```
├── index.html
├── vite.config.js
├── package.json
└── src/
    ├── main.jsx            # React entry point
    └── DevExDashboard.jsx  # the dashboard — data layer + UI, single file
```
