# git-activity

Collects GitHub pull requests and commits into a shared SQLite database, and exports them to an
xlsx workbook with a `Pull Requests` sheet and a `Commits` sheet. It works on one repository
(`--repo`), every repository whose name starts with a prefix (`--repo-prefix`), or every
repository of an organization or user (`--org`).

## Setup

Set `GITHUB_TOKEN` (or `GitActivity:PAT` in `appsettings.json`). The token needs the `repo` and `read:user` scopes.

## Collect

```bash
git-activity collect --repo acme/product-core                       # current month
git-activity collect --repo acme/product-core --period Quarter      # current quarter
git-activity collect --repo acme/product-core -p Month -n 3         # last 3 months incl. this one
git-activity collect --repo acme/product-core -p Quarter --anchor 2026-05-01
git-activity collect --repo acme/product-core --start 2026-01-01 --end 2026-04-01
```

- PRs are selected by **created date** and commits by **commit date**, on the default branch unless you pass `--branch`.
- Re-running the same window replaces the stored rows, so you don't get duplicates.
- Every repository goes into the same database (`--db`, default `gitactivity.db`). Each run is recorded in `CollectionRuns`.

## Multiple repositories

Use `--repo-prefix owner/prefix` instead of `--repo` to work on every repository whose name starts with the prefix. The match is case-insensitive, and the prefix can end with `.` or `-`.

```bash
git-activity collect --repo-prefix acme/source. -p Quarter
git-activity export  --repo-prefix acme/source. -p Quarter -o source-q4.xlsx
```

**collect**
- Lists the owner's repositories (an organization or a user) through GitHub and collects PRs and commits for each match, one after another.
- Archived repositories are skipped unless you pass `--include-archived`.
- A repository that already completed this window is skipped. `--refresh` re-collects it.
- **Errors:**
  - **One repository fails:** it is reported and the rest continue. The command exits with code 1 and lists what didn't finish.
  - **Rate-limit wait budget exhausted:** the run stops, because the remaining repositories would hit the same limit.
  - **Either case:** re-run the same command. Finished repositories are skipped and unfinished ones resume from their checkpoints.

**export**
- Reads the matching repositories from the database; no GitHub token is needed.
- Writes one workbook with a single `Pull Requests` sheet and a single `Commits` sheet, ordered by repository and then date.
- The `Commits` sheet gets an extra leading `Repository` column. The PR sheet already has one.

### All repositories of an organization

`--org owner` selects every repository of an organization or user. It behaves exactly like `--repo-prefix` with an empty prefix, including skipping archived repositories, `--refresh`, resuming, and the single combined workbook (default name `<owner>_all-repos_<window>.xlsx`).

```bash
git-activity collect --org acme -p Quarter
git-activity export  --org acme -p Quarter
```

Export with `--org` includes every stored repository of that owner, whichever command collected it.

## Resume and rate limits

- PRs are read oldest first through GitHub search, in 30-day `created:` chunks (`SearchChunkDays`). This mirrors `GitHubSynchronizer`'s last-run tracking.
- If a chunk has more than 1,000 results, it is halved automatically.
- Each page of PRs or commits is saved in one transaction together with a row in `CollectionCheckpoints`. That row is keyed by repo, phase and window, and records the chunk position, cursor and last PR number or commit hash with its date.
- **Failed run:** re-run the same command. It continues from the last saved page. If GitHub rejects the stored cursor, it restarts from the last saved item's date instead; any overlap is upserted, so nothing is duplicated.
- **Completed window:** running it again collects the whole window from the start, which refreshes open PRs.
- `--restart` ignores an unfinished checkpoint and starts over.
- **Rate limits:**
  - **Primary limit:** the run sleeps until `resetAt`.
  - **Secondary limit (403/429):** the run waits for the `Retry-After` delay.
  - **Wait budget:** if total waiting would exceed `RateLimitMaxWaitMinutes` (default 60), the run stops and saves the error on the checkpoint. Re-run later to resume.
- The checkpoint is keyed by the exact window. A window ending at "now" (`--start` without `--end`) changes on every run, so it can't resume. Give an explicit `--end` for long collections.

## Export

```bash
git-activity export --repo acme/product-core -p Quarter -o product-core-q4.xlsx
git-activity export --repo acme/product-core --all
```

## Field notes

| Field | Source |
|---|---|
| Author / reviewer email | `GitActivity:UserEmails` login→email map, then the public GitHub email, then the login |
| Reviewer(s) | Everyone who submitted a review, except the author |
| Review Submitted Date | First approve or changes-requested review by someone other than the author |
| First Review Date | First review or conversation comment by someone other than the author |
| Comment Authors | Conversation commenters, plus reviewers who left review comments |
| Review Rounds | Number of changes-requested reviews + 1 if the PR was reviewed at all, otherwise 0 |
| Linked Issues | GitHub closing references (`#77`) plus ticket keys (`PROJ-123`) found in the title, branch or description |
| Requested Reviewers | Reviewers who are still pending; GitHub drops a request once that reviewer submits a review |
| CI / Build Status | Status-check rollup on the head commit (Passed / Failed / Pending) |

Limits: each PR loads at most 100 reviews, 100 conversation comments and 20 labels, requested reviewers and linked issues.
