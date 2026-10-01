import React, { useMemo, useState, useEffect, useCallback } from "react";
import {
  LineChart, Line, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from "recharts";

/* =================================================================
   OData data layer
   Talks to the real DevExMetrics service: {baseUrl}/{tenantId}/odata/...
   Auth: optional `x-api-key` header (see Metrics.MCP.StreamableHTTP/Program.cs)
   ================================================================= */

async function odataFetch(config, path, query) {
  const base = config.baseUrl.replace(/\/$/, "");
  const url = `${base}/${config.tenantId}/odata/${path}${query ? `?${query}` : ""}`;
  const headers = { Accept: "application/json" };
  if (config.apiKey) headers["x-api-key"] = config.apiKey;

  let res;
  try {
    res = await fetch(url, { headers });
  } catch (e) {
    throw new Error(`Network error reaching ${url} — is the service reachable and CORS-enabled? (${e.message})`);
  }
  if (!res.ok) {
    let detail = "";
    try {
      const body = await res.json();
      detail = body?.error?.message || "";
    } catch { /* non-JSON error body */ }
    throw new Error(`${res.status} ${res.statusText} on ${path}${detail ? ` — ${detail}` : ""}`);
  }
  const json = await res.json();
  return Array.isArray(json?.value) ? json.value : json;
}

/** .NET TimeSpan "c" format, e.g. "1.02:03:04.5000000" or "02:03:04" -> hours (float) */
function parseTimeSpanHours(ts) {
  if (!ts || typeof ts !== "string") return 0;
  const neg = ts.startsWith("-");
  const clean = neg ? ts.slice(1) : ts;
  const m = clean.match(/^(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d+))?$/);
  if (!m) return 0;
  const days = Number(m[1] || 0);
  const hours = Number(m[2]);
  const minutes = Number(m[3]);
  const seconds = Number(m[4]) + (m[5] ? Number(`0.${m[5]}`) : 0);
  const total = days * 24 + hours + minutes / 60 + seconds / 3600;
  return neg ? -total : total;
}

async function fetchTeams(config) {
  const rows = await odataFetch(config, "Teams");
  return rows.map((r) => ({ name: r.Name, vs: r.ValueStream, region: r.Region }));
}

async function fetchSprints(config, year) {
  const rows = await odataFetch(config, "Sprints", `$filter=Year eq ${year}&$orderby=SprintNumber`);
  return rows.map((r) => ({ number: r.SprintNumber, label: `S${r.SprintNumber}`, year: r.Year, start: r.StartDate, end: r.EndDate }));
}

/** One broad fetch of PRMetricsEx (the flattened OData entity) covering every sprint in scope.
 *  All team / value-stream / sprint slicing happens client-side afterward, same as demo mode. */
async function fetchPRMetricsRange(config, startISO, endISO) {
  const filter = `CreatedAt ge ${startISO} and CreatedAt le ${endISO}`;
  const query = `$filter=${encodeURIComponent(filter)}&$expand=Team,CopilotReviewMetrics&$top=5000`;
  return odataFetch(config, "PRMetricsEx", query);
}

async function fetchTeamAuthorMetricsBySprint(config, team, year, sprintNumber) {
  const query = `team=${encodeURIComponent(team)}&year=${year}&sprintNumber=${sprintNumber}`;
  return odataFetch(config, "TeamAuthorMetrics/Sprint", query);
}

async function fetchReviewerMetricsBySprint(config, year, sprintNumber) {
  const filter = `Year eq ${year} and SprintNumber eq ${sprintNumber}`;
  return odataFetch(config, "ReviewerSprintMetrics", `$filter=${encodeURIComponent(filter)}`);
}

/** Buckets raw PRMetricsEx rows into the same per-team/per-sprint shape the demo generator produces. */
function aggregateLiveMetrics(rawRows, sprints) {
  const bucket = new Map();
  for (const row of rawRows) {
    const team = row.Team?.Name ?? row.TeamName ?? "Unknown";
    const valueStream = row.Team?.ValueStream ?? row.ValueStream ?? "Unknown";
    const region = row.Team?.Region ?? row.TeamRegion ?? "";
    const createdAt = new Date(row.CreatedAt);
    const sprint = sprints.find((s) => createdAt >= new Date(s.start) && createdAt <= new Date(s.end));
    if (!sprint) continue;

    const key = `${team}|${sprint.label}`;
    if (!bucket.has(key)) {
      bucket.set(key, {
        team, valueStream, region, sprint: sprint.label, sprintNumber: sprint.number,
        codingTimeSum: 0, pickupTimeSum: 0, reviewTimeSum: 0, approveTimeSum: 0, mergeTimeSum: 0,
        cycleTimeSum: 0, leadTimeSum: 0, maturitySum: 0, maturityCount: 0,
        totalPrs: 0, sizeCounts: { Micro: 0, Small: 0, Medium: 0, Large: 0 },
        changesRequested: 0, codeExcellence: 0, teamReq: 0, others: 0,
        commentsAfterApproval: 0, copilotPrs: 0, filesChanged: 0, filesReviewedByCopilot: 0,
        copilotComments: 0, humanReviewComments: 0,
      });
    }
    const b = bucket.get(key);
    b.totalPrs += 1;
    b.codingTimeSum += parseTimeSpanHours(row.CodingTime);
    b.pickupTimeSum += parseTimeSpanHours(row.PickupTime);
    b.reviewTimeSum += parseTimeSpanHours(row.ReviewTime);
    b.approveTimeSum += parseTimeSpanHours(row.ApproveTime);
    b.mergeTimeSum += parseTimeSpanHours(row.MergeTime);
    b.cycleTimeSum += parseTimeSpanHours(row.CycleTime);
    b.leadTimeSum += parseTimeSpanHours(row.LeadTime);
    if (row.MaturityPercentage != null) { b.maturitySum += row.MaturityPercentage; b.maturityCount += 1; }
    if (row.PRSize && b.sizeCounts[row.PRSize] != null) b.sizeCounts[row.PRSize] += 1;
    b.changesRequested += row.TotalReviewChangesRequested || 0;
    b.codeExcellence += row.CodeExcellenceRequestedChanges || 0;
    b.teamReq += row.TeamRequestedChanges || 0;
    b.others += row.OthersRequestedChanges || 0;
    b.commentsAfterApproval += row.TotalReviewCommentsAfterFinalApproval || 0;
    b.humanReviewComments += row.TotalReviewComments || 0;
    if (row.CopilotReviewMetrics) {
      b.copilotPrs += 1;
      b.filesChanged += row.CopilotReviewMetrics.FilesChanged || 0;
      b.filesReviewedByCopilot += row.CopilotReviewMetrics.FilesReviewed || 0;
      b.copilotComments += row.CopilotReviewMetrics.Comments || 0;
    } else {
      b.filesChanged += row.ChangedFiles || 0;
    }
  }

  return Array.from(bucket.values()).map((b) => ({
    team: b.team, valueStream: b.valueStream, region: b.region, sprint: b.sprint, sprintNumber: b.sprintNumber,
    codingTime: b.totalPrs ? b.codingTimeSum / b.totalPrs : 0,
    pickupTime: b.totalPrs ? b.pickupTimeSum / b.totalPrs : 0,
    reviewTime: b.totalPrs ? b.reviewTimeSum / b.totalPrs : 0,
    approveTime: b.totalPrs ? b.approveTimeSum / b.totalPrs : 0,
    mergeTime: b.totalPrs ? b.mergeTimeSum / b.totalPrs : 0,
    cycleTime: b.totalPrs ? b.cycleTimeSum / b.totalPrs : 0,
    leadTime: b.totalPrs ? b.leadTimeSum / b.totalPrs : 0,
    maturity: b.maturityCount ? b.maturitySum / b.maturityCount : 0,
    totalPrs: b.totalPrs, sizeCounts: b.sizeCounts,
    changesRequested: b.changesRequested, codeExcellence: b.codeExcellence, teamReq: b.teamReq, others: b.others,
    commentsAfterApproval: b.commentsAfterApproval,
    copilotAdoption: b.totalPrs ? b.copilotPrs / b.totalPrs : 0,
    copilotPrs: b.copilotPrs, filesChanged: b.filesChanged, filesReviewedByCopilot: b.filesReviewedByCopilot,
    copilotComments: b.copilotComments, humanReviewComments: b.humanReviewComments,
  }));
}

/* =================================================================
   Deterministic PRNG for demo/fallback data
   ================================================================= */
function hashSeed(str) {
  let h = 1779033703 ^ str.length;
  for (let i = 0; i < str.length; i++) {
    h = Math.imul(h ^ str.charCodeAt(i), 3432918353);
    h = (h << 13) | (h >>> 19);
  }
  return () => {
    h = Math.imul(h ^ (h >>> 16), 2246822507);
    h = Math.imul(h ^ (h >>> 13), 3266489909);
    h ^= h >>> 16;
    return (h >>> 0) / 4294967296;
  };
}
function rand(rng, min, max) { return min + rng() * (max - min); }

/* Domain constants for demo mode — mirrors Metrics.Models / appsettings.sample */
const DEMO_VALUE_STREAMS = ["Core Platform", "Payments", "Growth"];
const DEMO_TEAMS = [
  { name: "Alpha", vs: "Core Platform", region: "NA" },
  { name: "Beta", vs: "Core Platform", region: "EU" },
  { name: "Gamma", vs: "Payments", region: "NA" },
  { name: "Delta", vs: "Payments", region: "APAC" },
  { name: "Epsilon", vs: "Growth", region: "EU" },
  { name: "Zeta", vs: "Growth", region: "NA" },
];
const DEMO_SPRINTS = Array.from({ length: 8 }, (_, i) => ({ number: 41 + i, label: `S${41 + i}`, year: 2026 }));
const SIZE_ORDER = ["Micro", "Small", "Medium", "Large"];
const SIZE_COLOR = { Micro: "#3ED9C5", Small: "#6FE0A0", Medium: "#F0A63B", Large: "#EF6461" };
const AUTHORS_POOL = ["r.nunez", "k.iyer", "s.walsh", "t.moreau", "a.kader", "j.lindqvist", "p.oyelaran", "m.suzuki", "d.hartley", "c.beaumont"];

function generateDemoTeamSprintMetric(team, sprint) {
  const rng = hashSeed(`${team.name}-${sprint.label}-v2`);
  const codingTime = rand(rng, 4, 20);
  const pickupTime = rand(rng, 1, 14) * (team.name === "Delta" ? 1.8 : 1);
  const reviewTime = rand(rng, 2, 16) * (team.name === "Beta" ? 1.6 : 1);
  const approveTime = rand(rng, 0.5, 5);
  const mergeTime = rand(rng, 0.2, 3);
  const cycleTime = codingTime + pickupTime + reviewTime + approveTime + mergeTime;
  const leadTime = cycleTime + rand(rng, 0, 3);
  const maturity = Math.max(35, Math.round(100 - pickupTime * 1.8 - reviewTime * 0.9 + rand(rng, -6, 6)));

  const totalPrs = Math.round(rand(rng, 14, 42));
  const sizeWeights = [0.35, 0.32, 0.24, 0.09].map((w) => w + rand(rng, -0.05, 0.05));
  const sizeCounts = {};
  let remaining = totalPrs;
  SIZE_ORDER.forEach((s, i) => {
    const c = i === SIZE_ORDER.length - 1 ? remaining : Math.round(totalPrs * Math.max(0.02, sizeWeights[i]));
    sizeCounts[s] = Math.min(c, remaining);
    remaining -= sizeCounts[s];
  });

  const changesRequested = Math.round(rand(rng, 6, 30));
  const ceShare = rand(rng, 0.15, 0.4);
  const teamShare = rand(rng, 0.35, 0.6);
  const codeExcellence = Math.round(changesRequested * ceShare);
  const teamReq = Math.round(changesRequested * teamShare);
  const others = Math.max(0, changesRequested - codeExcellence - teamReq);

  const commentsAfterApproval = Math.round(rand(rng, 0, 6));
  const copilotAdoption = Math.min(1, Math.max(0, rand(rng, 0.2, 0.95)));
  const copilotPrs = Math.round(totalPrs * copilotAdoption);
  const filesChanged = Math.round(rand(rng, 60, 260));
  const filesReviewedByCopilot = Math.round(filesChanged * copilotAdoption * rand(rng, 0.55, 0.95));
  const copilotComments = Math.round(copilotPrs * rand(rng, 1.2, 4));
  const humanReviewComments = Math.round(totalPrs * rand(rng, 1.5, 5.5));

  return {
    team: team.name, valueStream: team.vs, region: team.region, sprint: sprint.label, sprintNumber: sprint.number,
    codingTime, pickupTime, reviewTime, approveTime, mergeTime, cycleTime, leadTime, maturity,
    totalPrs, sizeCounts,
    changesRequested, codeExcellence, teamReq, others,
    commentsAfterApproval, copilotAdoption, copilotPrs, filesChanged, filesReviewedByCopilot,
    copilotComments, humanReviewComments,
  };
}

function generateDemoAuthorRows(team, sprint) {
  const rng = hashSeed(`authors-${team.name}-${sprint.label}`);
  const n = Math.round(rand(rng, 3, 6));
  const chosen = [...AUTHORS_POOL].sort(() => rng() - 0.5).slice(0, n);
  return chosen.map((author) => {
    const prsAuthored = Math.round(rand(rng, 2, 14));
    return {
      author, team: team.name,
      prsAuthored,
      prsClosed: Math.max(0, prsAuthored - Math.round(rand(rng, 0, 2))),
      avgPrSize: Math.round(rand(rng, 40, 620)),
      avgCycleTime: rand(rng, 6, 40),
      avgReviewComments: rand(rng, 1, 9),
      avgPRMaturity: Math.round(rand(rng, 45, 96)),
    };
  });
}

function generateDemoReviewerRows(team, sprint) {
  const rng = hashSeed(`reviewers-${team.name}-${sprint.label}`);
  const n = Math.round(rand(rng, 2, 5));
  const chosen = [...AUTHORS_POOL].sort(() => rng() - 0.5).slice(0, n);
  return chosen.map((reviewer) => {
    const reviewsRequested = Math.round(rand(rng, 5, 22));
    const reviewsSubmitted = Math.max(0, reviewsRequested - Math.round(rand(rng, 0, 3)));
    return {
      reviewer, team: team.name,
      reviewsRequested, reviewsSubmitted,
      approvalRatePercentage: Math.round(rand(rng, 55, 98)),
      averageResponseTimeHours: rand(rng, 0.8, 18),
      commentCount: Math.round(rand(rng, 4, 40)),
    };
  });
}

/* =================================================================
   Presentational helpers
   ================================================================= */
function fmtHours(h) {
  if (h == null || isNaN(h)) return "—";
  if (h < 24) return `${h.toFixed(1)}h`;
  return `${(h / 24).toFixed(1)}d`;
}

function Kpi({ label, value, sub, tone }) {
  return (
    <div className="kpi-card">
      <div className="kpi-label">{label}</div>
      <div className={`kpi-value tone-${tone || "base"}`}>{value}</div>
      {sub && <div className="kpi-sub">{sub}</div>}
    </div>
  );
}

const STAGE_META = [
  { key: "codingTime", label: "Coding", color: "#5B8DEF" },
  { key: "pickupTime", label: "Pickup", color: "#F0A63B" },
  { key: "reviewTime", label: "Review", color: "#EF6461" },
  { key: "approveTime", label: "Approve", color: "#9B7EDE" },
  { key: "mergeTime", label: "Merge", color: "#3ED9C5" },
];

function FlowBar({ metric, maxTotal }) {
  const total = STAGE_META.reduce((s, st) => s + metric[st.key], 0);
  return (
    <div className="flowbar-row">
      <div className="flowbar-meta">
        <span className="flowbar-team">{metric.team}</span>
        <span className="flowbar-total">{fmtHours(total)}</span>
      </div>
      <div className="flowbar-track">
        {STAGE_META.map((st) => {
          const w = (metric[st.key] / maxTotal) * 100;
          return (
            <div
              key={st.key}
              className="flowbar-seg"
              style={{ width: `${w}%`, background: st.color }}
              title={`${st.label}: ${fmtHours(metric[st.key])}`}
            />
          );
        })}
      </div>
    </div>
  );
}

const DEFAULT_CONFIG = { baseUrl: "http://localhost:5100", tenantId: "tenant-1", apiKey: "", year: 2026 };

/* =================================================================
   Main dashboard
   ================================================================= */
export default function DevExDashboard() {
  const [valueStream, setValueStream] = useState("All");
  const [team, setTeam] = useState("All");
  const [sprintIdx, setSprintIdx] = useState(DEMO_SPRINTS.length - 1);

  const [dataSource, setDataSource] = useState("demo"); // 'demo' | 'live'
  const [config, setConfig] = useState(DEFAULT_CONFIG);
  const [draftConfig, setDraftConfig] = useState(DEFAULT_CONFIG);
  const [settingsOpen, setSettingsOpen] = useState(false);

  const [liveTeams, setLiveTeams] = useState([]);
  const [liveSprints, setLiveSprints] = useState([]);
  const [rawPRRows, setRawPRRows] = useState([]);
  const [liveAuthorRows, setLiveAuthorRows] = useState([]);
  const [liveReviewerRows, setLiveReviewerRows] = useState([]);

  const [coreStatus, setCoreStatus] = useState("idle"); // idle | loading | ok | error
  const [coreError, setCoreError] = useState(null);
  const [authorStatus, setAuthorStatus] = useState("idle");
  const [reviewerStatus, setReviewerStatus] = useState("idle");

  /* ---- core fetch: teams, sprints, and the full PR range for the year ---- */
  useEffect(() => {
    if (dataSource !== "live") return;
    let cancelled = false;
    async function run() {
      setCoreStatus("loading");
      setCoreError(null);
      try {
        const teams = await fetchTeams(config);
        const sprints = await fetchSprints(config, config.year);
        if (!sprints.length) throw new Error(`No sprints found for year ${config.year}`);
        const start = new Date(sprints[0].start).toISOString();
        const end = new Date(sprints[sprints.length - 1].end).toISOString();
        const rows = await fetchPRMetricsRange(config, start, end);
        if (cancelled) return;
        setLiveTeams(teams);
        setLiveSprints(sprints);
        setRawPRRows(rows);
        setCoreStatus("ok");
      } catch (e) {
        if (cancelled) return;
        setCoreStatus("error");
        setCoreError(e.message);
      }
    }
    run();
    return () => { cancelled = true; };
  }, [dataSource, config]);

  const teams = dataSource === "live" && liveTeams.length ? liveTeams : DEMO_TEAMS;
  const sprints = dataSource === "live" && liveSprints.length ? liveSprints : DEMO_SPRINTS;
  const valueStreams = dataSource === "live" && liveTeams.length
    ? [...new Set(liveTeams.map((t) => t.vs))]
    : DEMO_VALUE_STREAMS;

  const clampedSprintIdx = Math.min(sprintIdx, sprints.length - 1);
  const currentSprint = sprints[clampedSprintIdx] || sprints[sprints.length - 1];

  const teamsInScope = useMemo(
    () => teams.filter((t) => valueStream === "All" || t.vs === valueStream),
    [teams, valueStream]
  );
  const scopedTeams = team !== "All" ? teams.filter((t) => t.name === team) : teamsInScope;
  const scopedTeamNames = new Set(scopedTeams.map((t) => t.name));

  const demoAllMetrics = useMemo(() => {
    const rows = [];
    DEMO_TEAMS.forEach((t) => DEMO_SPRINTS.forEach((s) => rows.push(generateDemoTeamSprintMetric(t, s))));
    return rows;
  }, []);

  const liveAllMetrics = useMemo(
    () => (rawPRRows.length ? aggregateLiveMetrics(rawPRRows, liveSprints) : []),
    [rawPRRows, liveSprints]
  );

  const allMetrics = dataSource === "live" ? liveAllMetrics : demoAllMetrics;

  const currentMetrics = allMetrics.filter(
    (m) => m.sprintNumber === currentSprint.number && scopedTeamNames.has(m.team)
  );

  const trendData = sprints.map((s) => {
    const rows = allMetrics.filter((m) => m.sprintNumber === s.number && scopedTeamNames.has(m.team));
    const avg = (key) => rows.reduce((sum, r) => sum + r[key], 0) / (rows.length || 1);
    return {
      sprint: s.label,
      cycleTime: +avg("cycleTime").toFixed(1),
      leadTime: +avg("leadTime").toFixed(1),
      maturity: Math.round(avg("maturity")),
    };
  });

  const agg = (key) => currentMetrics.reduce((sum, r) => sum + r[key], 0) / (currentMetrics.length || 1);

  const totalPrs = currentMetrics.reduce((s, r) => s + r.totalPrs, 0);
  const changesRequestedTotal = currentMetrics.reduce((s, r) => s + r.changesRequested, 0);
  const changesRate = totalPrs ? Math.round((changesRequestedTotal / totalPrs) * 100) : 0;

  const sizeMixData = sprints.map((s) => {
    const rows = allMetrics.filter((m) => m.sprintNumber === s.number && scopedTeamNames.has(m.team));
    const entry = { sprint: s.label };
    SIZE_ORDER.forEach((sz) => { entry[sz] = rows.reduce((sum, r) => sum + (r.sizeCounts[sz] || 0), 0); });
    return entry;
  });

  const changesSourcePie = [
    { name: "Code Excellence", value: currentMetrics.reduce((s, r) => s + r.codeExcellence, 0), color: "#9B7EDE" },
    { name: "Team", value: currentMetrics.reduce((s, r) => s + r.teamReq, 0), color: "#5B8DEF" },
    { name: "Others", value: currentMetrics.reduce((s, r) => s + r.others, 0), color: "#8A93A6" },
  ];

  const copilotAdoptionAvg = Math.round(agg("copilotAdoption") * 100);

  const copilotTrendData = sprints.map((s) => {
    const rows = allMetrics.filter((m) => m.sprintNumber === s.number && scopedTeamNames.has(m.team));
    const totalPrsS = rows.reduce((sum, r) => sum + r.totalPrs, 0);
    const copilotPrsS = rows.reduce((sum, r) => sum + r.copilotPrs, 0);
    return {
      sprint: s.label,
      adoption: totalPrsS ? Math.round((copilotPrsS / totalPrsS) * 100) : 0,
      copilotComments: rows.reduce((sum, r) => sum + r.copilotComments, 0),
      humanComments: rows.reduce((sum, r) => sum + r.humanReviewComments, 0),
    };
  });

  const copilotByTeam = currentMetrics.map((m) => ({
    team: m.team,
    coverage: m.filesChanged ? Math.round((m.filesReviewedByCopilot / m.filesChanged) * 100) : 0,
    copilotComments: m.copilotComments,
    humanComments: m.humanReviewComments,
    adoption: Math.round(m.copilotAdoption * 100),
  }));

  const copilotTotals = {
    filesChanged: currentMetrics.reduce((s, r) => s + r.filesChanged, 0),
    filesReviewed: currentMetrics.reduce((s, r) => s + r.filesReviewedByCopilot, 0),
    copilotComments: currentMetrics.reduce((s, r) => s + r.copilotComments, 0),
    humanComments: currentMetrics.reduce((s, r) => s + r.humanReviewComments, 0),
    copilotPrs: currentMetrics.reduce((s, r) => s + r.copilotPrs, 0),
  };
  const copilotCoveragePct = copilotTotals.filesChanged
    ? Math.round((copilotTotals.filesReviewed / copilotTotals.filesChanged) * 100) : 0;
  const copilotShareOfComments = copilotTotals.copilotComments + copilotTotals.humanComments
    ? Math.round((copilotTotals.copilotComments / (copilotTotals.copilotComments + copilotTotals.humanComments)) * 100) : 0;

  /* ---- author metrics: real TeamAuthorMetrics/Sprint endpoint, one call per team in scope ---- */
  useEffect(() => {
    if (dataSource !== "live") return;
    let cancelled = false;
    async function run() {
      setAuthorStatus("loading");
      try {
        const results = await Promise.all(
          scopedTeams.map((t) => fetchTeamAuthorMetricsBySprint(config, t.name, currentSprint.year, currentSprint.number))
        );
        if (cancelled) return;
        setLiveAuthorRows(results.flat().map((r) => ({
          author: r.Author, team: r.Team,
          prsAuthored: r.PrsAuthored, prsClosed: r.PrsClosed,
          avgPrSize: r.AvgPrSize, avgCycleTime: r.AvgCycleTime,
          avgReviewComments: r.AvgReviewComments, avgPRMaturity: r.AvgPRMaturity,
        })));
        setAuthorStatus("ok");
      } catch (e) {
        if (cancelled) return;
        setAuthorStatus("error");
      }
    }
    if (scopedTeams.length && currentSprint) run();
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dataSource, config, team, valueStream, currentSprint?.label]);

  /* ---- reviewer metrics: ReviewerSprintMetrics is repository-scoped, not team-scoped, in the current schema ---- */
  useEffect(() => {
    if (dataSource !== "live") return;
    let cancelled = false;
    async function run() {
      setReviewerStatus("loading");
      try {
        const rows = await fetchReviewerMetricsBySprint(config, currentSprint.year, currentSprint.number);
        if (cancelled) return;
        setLiveReviewerRows(rows.map((r) => ({
          reviewer: r.Reviewer, team: r.Repository,
          reviewsRequested: r.ReviewsRequested ?? r.PrsRequested,
          reviewsSubmitted: r.ReviewsSubmitted ?? r.PrsReviewed,
          approvalRatePercentage: r.ApprovalRatePercentage,
          averageResponseTimeHours: r.AverageResponseTimeHours,
          commentCount: r.CommentCount ?? r.CommentCountWhenReviewed,
        })));
        setReviewerStatus("ok");
      } catch (e) {
        if (cancelled) return;
        setReviewerStatus("error");
      }
    }
    if (currentSprint) run();
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dataSource, config, currentSprint?.label]);

  const demoAuthorRows = useMemo(() => {
    const rows = [];
    scopedTeams.forEach((t) => rows.push(...generateDemoAuthorRows(t, currentSprint)));
    return rows.sort((a, b) => b.prsAuthored - a.prsAuthored).slice(0, 8);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team, valueStream, currentSprint?.label]);

  const demoReviewerRows = useMemo(() => {
    const rows = [];
    scopedTeams.forEach((t) => rows.push(...generateDemoReviewerRows(t, currentSprint)));
    return rows.sort((a, b) => b.reviewsSubmitted - a.reviewsSubmitted).slice(0, 8);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team, valueStream, currentSprint?.label]);

  const authorRows = dataSource === "live"
    ? [...liveAuthorRows].sort((a, b) => b.prsAuthored - a.prsAuthored).slice(0, 8)
    : demoAuthorRows;
  const reviewerRows = dataSource === "live"
    ? [...liveReviewerRows].sort((a, b) => b.reviewsSubmitted - a.reviewsSubmitted).slice(0, 8)
    : demoReviewerRows;

  const maxFlowTotal = Math.max(1, ...currentMetrics.map((m) => STAGE_META.reduce((s, st) => s + m[st.key], 0)));

  const applyConfig = useCallback(() => {
    setConfig(draftConfig);
    setDataSource("live");
    setSettingsOpen(false);
  }, [draftConfig]);

  return (
    <div className="dxd-root">
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@500;600;700&family=IBM+Plex+Mono:wght@400;500;600&family=Inter:wght@400;500;600&display=swap');

        .dxd-root {
          --bg: #10131A; --panel: #171C26; --panel-2: #1D2330; --border: #2A3040;
          --text: #E8ECF1; --muted: #8A93A6; --accent: #3ED9C5; --warn: #F0A63B;
          --danger: #EF6461; --blue: #5B8DEF; --purple: #9B7EDE;
          font-family: 'Inter', system-ui, sans-serif; background: var(--bg); color: var(--text);
          padding: 24px; min-height: 100%; box-sizing: border-box; position: relative;
        }
        .dxd-root * { box-sizing: border-box; }
        .dxd-header { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: 20px; flex-wrap: wrap; gap: 12px; }
        .dxd-title { font-family: 'Space Grotesk', sans-serif; font-weight: 700; font-size: 22px; letter-spacing: -0.01em; }
        .dxd-title span { color: var(--accent); }
        .dxd-subtitle { color: var(--muted); font-size: 13px; margin-top: 2px; display: flex; align-items: center; gap: 8px; }

        .filters { display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
        .filter-select {
          background: var(--panel-2); color: var(--text); border: 1px solid var(--border);
          border-radius: 8px; padding: 7px 10px; font-size: 13px; font-family: 'IBM Plex Mono', monospace; cursor: pointer;
        }
        .sprint-rail { display: flex; gap: 4px; background: var(--panel-2); border: 1px solid var(--border); border-radius: 8px; padding: 4px; }
        .sprint-chip { font-family: 'IBM Plex Mono', monospace; font-size: 12px; padding: 5px 9px; border-radius: 6px; color: var(--muted); cursor: pointer; border: none; background: transparent; }
        .sprint-chip.active { background: var(--accent); color: #0A1210; font-weight: 600; }
        .btn-ghost {
          background: var(--panel-2); color: var(--text); border: 1px solid var(--border); border-radius: 8px;
          padding: 7px 12px; font-size: 12.5px; cursor: pointer; font-family: 'IBM Plex Mono', monospace;
        }
        .btn-ghost:hover { border-color: var(--accent); }
        .btn-primary { background: var(--accent); color: #0A1210; border: none; border-radius: 8px; padding: 8px 14px; font-size: 13px; font-weight: 600; cursor: pointer; }

        .status-pill { display: inline-flex; align-items: center; gap: 5px; padding: 2px 8px; border-radius: 999px; font-size: 11px; font-family: 'IBM Plex Mono', monospace; }
        .status-dot { width: 6px; height: 6px; border-radius: 50%; }
        .status-demo { background: rgba(138,147,166,.15); color: var(--muted); }
        .status-demo .status-dot { background: var(--muted); }
        .status-ok { background: rgba(62,217,197,.15); color: var(--accent); }
        .status-ok .status-dot { background: var(--accent); }
        .status-loading { background: rgba(240,166,59,.15); color: var(--warn); }
        .status-loading .status-dot { background: var(--warn); animation: pulse 1s infinite; }
        .status-error { background: rgba(239,100,97,.15); color: var(--danger); }
        .status-error .status-dot { background: var(--danger); }
        @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.3} }

        .settings-overlay { position: fixed; inset: 0; background: rgba(6,8,12,.6); display: flex; align-items: center; justify-content: center; z-index: 50; }
        .settings-panel { background: var(--panel); border: 1px solid var(--border); border-radius: 14px; padding: 24px; width: 420px; max-width: 90vw; }
        .settings-title { font-family: 'Space Grotesk', sans-serif; font-weight: 600; font-size: 16px; margin-bottom: 4px; }
        .settings-desc { color: var(--muted); font-size: 12px; margin-bottom: 16px; }
        .field-label { font-size: 11px; color: var(--muted); text-transform: uppercase; letter-spacing: .05em; margin-bottom: 5px; display: block; }
        .field-input { width: 100%; background: var(--panel-2); border: 1px solid var(--border); color: var(--text); border-radius: 8px; padding: 9px 11px; font-size: 13px; font-family: 'IBM Plex Mono', monospace; margin-bottom: 14px; }
        .field-input:focus { outline: none; border-color: var(--accent); }
        .settings-actions { display: flex; justify-content: space-between; align-items: center; margin-top: 6px; }
        .error-banner { background: rgba(239,100,97,.1); border: 1px solid rgba(239,100,97,.35); color: var(--danger); border-radius: 8px; padding: 10px 12px; font-size: 12px; font-family: 'IBM Plex Mono', monospace; margin-bottom: 16px; }

        .kpi-strip { display: grid; grid-template-columns: repeat(6, 1fr); gap: 12px; margin-bottom: 20px; }
        .kpi-card { background: var(--panel); border: 1px solid var(--border); border-radius: 10px; padding: 14px 16px; }
        .copilot-kpi-row { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
        .copilot-kpi { background: var(--panel-2); border: 1px solid var(--border); border-radius: 10px; padding: 12px 14px; }
        .kpi-label { font-size: 11px; color: var(--muted); text-transform: uppercase; letter-spacing: 0.06em; margin-bottom: 8px; }
        .kpi-value { font-family: 'Space Grotesk', sans-serif; font-size: 24px; font-weight: 700; }
        .kpi-sub { font-size: 11px; color: var(--muted); margin-top: 4px; font-family: 'IBM Plex Mono', monospace; }
        .tone-good { color: var(--accent); } .tone-warn { color: var(--warn); } .tone-bad { color: var(--danger); }

        .grid-2 { display: grid; grid-template-columns: 1.3fr 1fr; gap: 16px; margin-bottom: 16px; }
        .grid-3 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 16px; }

        .panel { background: var(--panel); border: 1px solid var(--border); border-radius: 12px; padding: 18px 20px; position: relative; }
        .panel-title { font-family: 'Space Grotesk', sans-serif; font-size: 14px; font-weight: 600; margin-bottom: 2px; }
        .panel-desc { font-size: 11.5px; color: var(--muted); margin-bottom: 14px; }
        .panel-note { font-size: 11px; color: var(--warn); margin-top: 8px; font-style: italic; }

        .flowbar-legend { display: flex; gap: 14px; margin-bottom: 12px; flex-wrap: wrap; }
        .flowbar-legend-item { display: flex; align-items: center; gap: 6px; font-size: 11.5px; color: var(--muted); font-family: 'IBM Plex Mono', monospace; }
        .swatch { width: 9px; height: 9px; border-radius: 2px; display: inline-block; }

        .flowbar-row { margin-bottom: 12px; }
        .flowbar-meta { display: flex; justify-content: space-between; font-size: 12px; margin-bottom: 4px; }
        .flowbar-team { font-weight: 600; }
        .flowbar-total { color: var(--muted); font-family: 'IBM Plex Mono', monospace; }
        .flowbar-track { display: flex; height: 14px; border-radius: 4px; overflow: hidden; background: var(--panel-2); }
        .flowbar-seg { height: 100%; min-width: 2px; }

        table.dxd-table { width: 100%; border-collapse: collapse; font-size: 12.5px; }
        table.dxd-table th { text-align: left; color: var(--muted); font-weight: 500; padding: 6px 8px; border-bottom: 1px solid var(--border); font-size: 11px; text-transform: uppercase; letter-spacing: 0.04em; }
        table.dxd-table td { padding: 7px 8px; border-bottom: 1px solid var(--border); font-family: 'IBM Plex Mono', monospace; }
        table.dxd-table tr:last-child td { border-bottom: none; }
        .name-cell { font-family: 'Inter', sans-serif !important; font-weight: 500; }
        .pill { display: inline-block; padding: 2px 7px; border-radius: 999px; font-size: 11px; font-family: 'IBM Plex Mono', monospace; }
        .empty-note { color: var(--muted); font-size: 12px; padding: 20px 0; text-align: center; }

        .recharts-cartesian-axis-tick text { fill: var(--muted); font-size: 11px; font-family: 'IBM Plex Mono', monospace; }
        .recharts-legend-item-text { color: var(--muted) !important; font-size: 12px !important; }

        @media (max-width: 900px) {
          .kpi-strip { grid-template-columns: repeat(2, 1fr); }
          .grid-2, .grid-3 { grid-template-columns: 1fr; }
          .copilot-kpi-row { grid-template-columns: repeat(2, 1fr); }
        }
      `}</style>

      <div className="dxd-header">
        <div>
          <div className="dxd-title">DevEx<span>Metrics</span> · Flow Dashboard</div>
          <div className="dxd-subtitle">
            <span>{totalPrs} PRs · {scopedTeams.length} team{scopedTeams.length !== 1 ? "s" : ""} · {currentSprint?.label} {currentSprint?.year}</span>
            {dataSource === "demo" && <span className="status-pill status-demo"><span className="status-dot" />Demo data</span>}
            {dataSource === "live" && coreStatus === "loading" && <span className="status-pill status-loading"><span className="status-dot" />Connecting…</span>}
            {dataSource === "live" && coreStatus === "ok" && <span className="status-pill status-ok"><span className="status-dot" />Live · {config.baseUrl.replace(/^https?:\/\//, "")}</span>}
            {dataSource === "live" && coreStatus === "error" && <span className="status-pill status-error"><span className="status-dot" />Connection failed</span>}
          </div>
        </div>
        <div className="filters">
          <select className="filter-select" value={valueStream} onChange={(e) => { setValueStream(e.target.value); setTeam("All"); }}>
            <option value="All">All value streams</option>
            {valueStreams.map((vs) => <option key={vs} value={vs}>{vs}</option>)}
          </select>
          <select className="filter-select" value={team} onChange={(e) => setTeam(e.target.value)}>
            <option value="All">All teams</option>
            {teamsInScope.map((t) => <option key={t.name} value={t.name}>{t.name}</option>)}
          </select>
          <div className="sprint-rail">
            {sprints.map((s, i) => (
              <button key={s.label} className={`sprint-chip ${i === clampedSprintIdx ? "active" : ""}`} onClick={() => setSprintIdx(i)}>
                {s.label}
              </button>
            ))}
          </div>
          <button className="btn-ghost" onClick={() => { setDraftConfig(config); setSettingsOpen(true); }}>
            {dataSource === "live" ? "⚙ Connection" : "⚡ Connect live API"}
          </button>
          {dataSource === "live" && (
            <button className="btn-ghost" onClick={() => setDataSource("demo")}>Use demo data</button>
          )}
        </div>
      </div>

      {dataSource === "live" && coreStatus === "error" && (
        <div className="error-banner">
          Couldn't load live data: {coreError}. Check the base URL, tenant ID and API key, and confirm CORS is enabled on the service for this origin. Showing empty state until this resolves — click "Use demo data" to fall back.
        </div>
      )}

      {settingsOpen && (
        <div className="settings-overlay" onClick={() => setSettingsOpen(false)}>
          <div className="settings-panel" onClick={(e) => e.stopPropagation()}>
            <div className="settings-title">Connect to DevExMetrics API</div>
            <div className="settings-desc">
              Points at the real OData endpoints — <code>{"{baseUrl}/{tenantId}/odata/..."}</code>. Requires the service to allow cross-origin requests from this page.
            </div>
            <label className="field-label">Base URL</label>
            <input className="field-input" value={draftConfig.baseUrl}
              onChange={(e) => setDraftConfig((c) => ({ ...c, baseUrl: e.target.value }))}
              placeholder="https://localhost:5001" />
            <label className="field-label">Tenant ID</label>
            <input className="field-input" value={draftConfig.tenantId}
              onChange={(e) => setDraftConfig((c) => ({ ...c, tenantId: e.target.value }))}
              placeholder="tenant-1" />
            <label className="field-label">API key (x-api-key header, optional)</label>
            <input className="field-input" type="password" value={draftConfig.apiKey}
              onChange={(e) => setDraftConfig((c) => ({ ...c, apiKey: e.target.value }))} />
            <label className="field-label">Sprint calendar year</label>
            <input className="field-input" type="number" value={draftConfig.year}
              onChange={(e) => setDraftConfig((c) => ({ ...c, year: Number(e.target.value) }))} />
            <div className="settings-actions">
              <button className="btn-ghost" onClick={() => setSettingsOpen(false)}>Cancel</button>
              <button className="btn-primary" onClick={applyConfig}>Connect</button>
            </div>
          </div>
        </div>
      )}

      <div className="kpi-strip">
        <Kpi label="Lead Time" value={fmtHours(agg("leadTime"))} sub="create → merge" tone={agg("leadTime") > 40 ? "bad" : "good"} />
        <Kpi label="Cycle Time" value={fmtHours(agg("cycleTime"))} sub="first commit → merge" tone={agg("cycleTime") > 36 ? "bad" : "good"} />
        <Kpi label="Pickup Time" value={fmtHours(agg("pickupTime"))} sub="open → first review" tone={agg("pickupTime") > 10 ? "warn" : "good"} />
        <Kpi label="Review Time" value={fmtHours(agg("reviewTime"))} sub="first review → merge" tone={agg("reviewTime") > 12 ? "warn" : "good"} />
        <Kpi label="Maturity" value={`${Math.round(agg("maturity"))}%`} sub="stability after review starts" tone={agg("maturity") < 60 ? "bad" : agg("maturity") < 75 ? "warn" : "good"} />
        <Kpi label="Changes Req. Rate" value={`${changesRate}%`} sub={`${changesRequestedTotal} of ${totalPrs} PRs`} tone={changesRate > 55 ? "warn" : "good"} />
      </div>

      <div className="grid-2">
        <div className="panel">
          <div className="panel-title">Cycle time flow decomposition</div>
          <div className="panel-desc">Where PR time actually goes, per team — {currentSprint?.label}. Bar width scaled to the slowest team in scope.</div>
          <div className="flowbar-legend">
            {STAGE_META.map((st) => (
              <div key={st.key} className="flowbar-legend-item"><span className="swatch" style={{ background: st.color }} />{st.label}</div>
            ))}
          </div>
          {currentMetrics.length
            ? currentMetrics.map((m) => <FlowBar key={m.team} metric={m} maxTotal={maxFlowTotal} />)
            : <div className="empty-note">No PR data for this scope and sprint yet.</div>}
        </div>

        <div className="panel">
          <div className="panel-title">Changes requested — by source</div>
          <div className="panel-desc">Governance (Code Excellence) vs. peer (Team) vs. other review pressure.</div>
          <ResponsiveContainer width="100%" height={200}>
            <PieChart>
              <Pie data={changesSourcePie} dataKey="value" nameKey="name" innerRadius={50} outerRadius={78} paddingAngle={3}>
                {changesSourcePie.map((e) => <Cell key={e.name} fill={e.color} stroke="none" />)}
              </Pie>
              <Tooltip contentStyle={{ background: "#1D2330", border: "1px solid #2A3040", borderRadius: 8, fontSize: 12 }} />
              <Legend wrapperStyle={{ fontSize: 12 }} />
            </PieChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="panel" style={{ marginBottom: 16 }}>
        <div className="panel-title">Copilot review statistics</div>
        <div className="panel-desc">GitHub Copilot's coverage of review work, and how it compares to human reviewers — {currentSprint?.label}.</div>

        <div className="copilot-kpi-row">
          <div className="copilot-kpi">
            <div className="kpi-label">Adoption</div>
            <div className="kpi-value tone-good">{copilotAdoptionAvg}%</div>
            <div className="kpi-sub">{copilotTotals.copilotPrs} of {totalPrs} PRs reviewed by Copilot</div>
          </div>
          <div className="copilot-kpi">
            <div className="kpi-label">File coverage</div>
            <div className="kpi-value tone-good">{copilotCoveragePct}%</div>
            <div className="kpi-sub">{copilotTotals.filesReviewed} of {copilotTotals.filesChanged} changed files</div>
          </div>
          <div className="copilot-kpi">
            <div className="kpi-label">Share of review comments</div>
            <div className="kpi-value tone-warn">{copilotShareOfComments}%</div>
            <div className="kpi-sub">{copilotTotals.copilotComments} Copilot vs {copilotTotals.humanComments} human</div>
          </div>
          <div className="copilot-kpi">
            <div className="kpi-label">Avg comments / PR reviewed</div>
            <div className="kpi-value">{copilotTotals.copilotPrs ? (copilotTotals.copilotComments / copilotTotals.copilotPrs).toFixed(1) : "—"}</div>
            <div className="kpi-sub">when Copilot participates</div>
          </div>
        </div>

        <div className="grid-2" style={{ marginTop: 16, marginBottom: 0 }}>
          <div>
            <div className="panel-desc" style={{ marginBottom: 8 }}>Adoption &amp; comment volume trend</div>
            <ResponsiveContainer width="100%" height={210}>
              <LineChart data={copilotTrendData} margin={{ left: -10, right: 10 }}>
                <CartesianGrid stroke="#2A3040" strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="sprint" stroke="#2A3040" />
                <YAxis yAxisId="left" stroke="#2A3040" unit="%" />
                <YAxis yAxisId="right" orientation="right" stroke="#2A3040" />
                <Tooltip contentStyle={{ background: "#1D2330", border: "1px solid #2A3040", borderRadius: 8, fontSize: 12 }} />
                <Legend wrapperStyle={{ fontSize: 12 }} />
                <Line yAxisId="left" type="monotone" dataKey="adoption" name="Adoption %" stroke="#3ED9C5" strokeWidth={2.5} dot={false} />
                <Line yAxisId="right" type="monotone" dataKey="copilotComments" name="Copilot comments" stroke="#9B7EDE" strokeWidth={2} dot={false} />
                <Line yAxisId="right" type="monotone" dataKey="humanComments" name="Human comments" stroke="#5B8DEF" strokeWidth={2} dot={false} strokeDasharray="4 3" />
              </LineChart>
            </ResponsiveContainer>
          </div>
          <div>
            <div className="panel-desc" style={{ marginBottom: 8 }}>Copilot vs human comments, by team</div>
            <ResponsiveContainer width="100%" height={210}>
              <BarChart data={copilotByTeam} margin={{ left: -10, right: 10 }}>
                <CartesianGrid stroke="#2A3040" strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="team" stroke="#2A3040" />
                <YAxis stroke="#2A3040" />
                <Tooltip contentStyle={{ background: "#1D2330", border: "1px solid #2A3040", borderRadius: 8, fontSize: 12 }} />
                <Legend wrapperStyle={{ fontSize: 12 }} />
                <Bar dataKey="copilotComments" name="Copilot" fill="#9B7EDE" radius={[3, 3, 0, 0]} />
                <Bar dataKey="humanComments" name="Human" fill="#5B8DEF" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      <div className="grid-3">
        <div className="panel">
          <div className="panel-title">Cycle &amp; lead time trend</div>
          <div className="panel-desc">Sprint-over-sprint average, current scope.</div>
          <ResponsiveContainer width="100%" height={230}>
            <LineChart data={trendData} margin={{ left: -10, right: 10 }}>
              <CartesianGrid stroke="#2A3040" strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="sprint" stroke="#2A3040" />
              <YAxis stroke="#2A3040" unit="h" />
              <Tooltip contentStyle={{ background: "#1D2330", border: "1px solid #2A3040", borderRadius: 8, fontSize: 12 }} />
              <Legend wrapperStyle={{ fontSize: 12 }} />
              <Line type="monotone" dataKey="cycleTime" name="Cycle Time" stroke="#3ED9C5" strokeWidth={2.5} dot={false} />
              <Line type="monotone" dataKey="leadTime" name="Lead Time" stroke="#5B8DEF" strokeWidth={2.5} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>

        <div className="panel">
          <div className="panel-title">PR size mix per sprint</div>
          <div className="panel-desc">Micro/Small/Medium/Large by total line changes.</div>
          <ResponsiveContainer width="100%" height={230}>
            <BarChart data={sizeMixData} margin={{ left: -10, right: 10 }}>
              <CartesianGrid stroke="#2A3040" strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="sprint" stroke="#2A3040" />
              <YAxis stroke="#2A3040" />
              <Tooltip contentStyle={{ background: "#1D2330", border: "1px solid #2A3040", borderRadius: 8, fontSize: 12 }} />
              <Legend wrapperStyle={{ fontSize: 12 }} />
              {SIZE_ORDER.map((sz) => (
                <Bar key={sz} dataKey={sz} stackId="a" fill={SIZE_COLOR[sz]} radius={sz === "Large" ? [3, 3, 0, 0] : 0} />
              ))}
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="grid-2">
        <div className="panel">
          <div className="panel-title">Top authors — {currentSprint?.label}</div>
          <div className="panel-desc">
            Volume and quality signal side by side.
            {dataSource === "live" && authorStatus === "loading" && " Loading…"}
            {dataSource === "live" && authorStatus === "error" && " (failed to load — check connection)"}
          </div>
          {authorRows.length ? (
            <table className="dxd-table">
              <thead><tr><th>Author</th><th>Team</th><th>PRs</th><th>Avg size</th><th>Avg cycle</th><th>Maturity</th></tr></thead>
              <tbody>
                {authorRows.map((a) => (
                  <tr key={a.author + a.team}>
                    <td className="name-cell">{a.author}</td>
                    <td>{a.team}</td>
                    <td>{a.prsAuthored}</td>
                    <td>{Math.round(a.avgPrSize)}L</td>
                    <td>{fmtHours(a.avgCycleTime)}</td>
                    <td>
                      <span className="pill" style={{ background: a.avgPRMaturity < 60 ? "rgba(239,100,97,.15)" : "rgba(62,217,197,.15)", color: a.avgPRMaturity < 60 ? "var(--danger)" : "var(--accent)" }}>
                        {Math.round(a.avgPRMaturity)}%
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : <div className="empty-note">No author data for this scope and sprint yet.</div>}
        </div>

        <div className="panel">
          <div className="panel-title">Reviewer load &amp; responsiveness — {currentSprint?.label}</div>
          <div className="panel-desc">
            Who's carrying review load, and how fast they turn it around.
            {dataSource === "live" && reviewerStatus === "loading" && " Loading…"}
          </div>
          {reviewerRows.length ? (
            <table className="dxd-table">
              <thead><tr><th>Reviewer</th><th>{dataSource === "live" ? "Repository" : "Team"}</th><th>Req'd</th><th>Done</th><th>Approval %</th><th>Avg response</th></tr></thead>
              <tbody>
                {reviewerRows.map((r) => (
                  <tr key={r.reviewer + r.team}>
                    <td className="name-cell">{r.reviewer}</td>
                    <td>{r.team}</td>
                    <td>{r.reviewsRequested}</td>
                    <td>{r.reviewsSubmitted}</td>
                    <td>
                      <span className="pill" style={{ background: r.approvalRatePercentage < 70 ? "rgba(240,166,59,.15)" : "rgba(62,217,197,.15)", color: r.approvalRatePercentage < 70 ? "var(--warn)" : "var(--accent)" }}>
                        {r.approvalRatePercentage}%
                      </span>
                    </td>
                    <td>{fmtHours(r.averageResponseTimeHours)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : <div className="empty-note">No reviewer data for this sprint yet.</div>}
          {dataSource === "live" && (
            <div className="panel-note">Note: ReviewerSprintMetrics is scoped by repository, not team, in the current data model — this table isn't filtered by the team/value-stream selector above.</div>
          )}
        </div>
      </div>
    </div>
  );
}
