# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Vietnamese university capstone ("Đồ án chuyên ngành", class DA23TTA) built for the Victory English centre: a computer-based exam system for the centre's exam room that reproduces the Cambridge/Inspera candidate and invigilator experience (layout, format, interface). Exams are taken at the centre on its computers.

- **Scope:** A2 Key for Schools first (demo due Fri 16/10/2026), then B1 Preliminary for Schools, then IELTS (no fixed format; later). B2/C1/C2 are out of scope.
- **Everything is built from scratch**, including question authoring: Victory's old exam-entry system is gone (decided 10/10/2026). Victory supplies the exam content (questions, answer keys, audio split into one track per Listening part).
- **Done so far:** database schema (EF Core migrations on MySQL, reviewed 10/10 — see `.planning/2026-10-10-sprint-plan/findings.md`), the server-side exam clock (`AttemptClock`), staff sign-in (VE-3), candidate sign-in with session PIN (VE-4), exam package import (VE-5), the candidate screen for A2 Reading & Writing (VE-6/7), server clock + autosave + time-up submission (VE-8), Listening with one continuous recording and drag-and-drop (VE-10); 149 backend + 48 frontend tests. Next: auto-marking (VE-11), PDF converter (VE-56). Not yet: admin/invigilator portals, SignalR.
- **Planning:** sprint backlog in `.planning/2026-10-10-sprint-plan/` (source of truth `sprints.py`), mirrored on Jira https://victoryexam.atlassian.net project `SCRUM` (story VE-n = SCRUM-(n+17)).

Deadline: final report submitted 31/12/2026. Faculty check-in weeks: 19–25/10, 09–15/11, 30/11–06/12, 21–27/12/2026.

## Commands

Run from `src/backend` unless noted. Requires .NET SDK 10 (pinned in `global.json`) and Docker Desktop.

```bash
# Start MySQL (from repo root). docker/.env holds the passwords and is git-ignored; copy docker/.env.example first.
docker compose -f docker/docker-compose.yml --env-file docker/.env up -d

# Connection string for the API (Development reads user-secrets)
dotnet user-secrets set "ConnectionStrings:ExamPrep" "server=localhost;port=3306;database=examprep;user=examprep;password=<MYSQL_PASSWORD>" --project src/ExamPrep.Api

# Build / run (health check at /health)
dotnet build
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/ExamPrep.Api

# Migrations (design-time factory reads ConnectionStrings__ExamPrep from the environment)
dotnet ef migrations add <Name> -p src/ExamPrep.Infrastructure -s src/ExamPrep.Api -o Persistence/Migrations
ConnectionStrings__ExamPrep="<conn>" dotnet ef database update -p src/ExamPrep.Infrastructure -s src/ExamPrep.Api

# Tests (Microsoft.Testing.Platform runner, xUnit v3). DB tests need a server-level root connection
# and are reported as skipped without it; each run creates and drops its own examprep_test_* database.
EXAMPREP_TEST_MYSQL="server=localhost;port=3306;user=root;password=<MYSQL_ROOT_PASSWORD>" dotnet test
dotnet test --filter-method "*SnakeCase*"      # single test / pattern
```

## Frontend (`src/frontend`)

React 19 + TypeScript + Vite; `npm run dev` (port 5173, `/api` proxied to the API on 5180, launch profile `http`), `npm test` (Vitest + Testing Library), `npm run build`, `npm run lint` (oxlint). The candidate screen copies the Inspera player measured from the official A2 sample (colours and sizes in `src/index.css` `:root`, notes in `.planning/2026-10-10-sprint-plan/findings.md`). `exam/paperLogic.ts` holds paging (A2 Reading Part 1 one notice per page, other parts whole), `{{q:n}}` gap slots and word counting; gap controls are portalled into the passage HTML. Exam HTML is sanitised on import (HtmlSanitizer) and again in the browser (DOMPurify). Development runs a `DemoSeeder` (section `Demo` in `appsettings.Development.json`) that opens an A2 exam from the samples; the dev admin login lives in user-secrets (`Bootstrap:*`).

## Backend architecture (`src/backend`)

- `ExamPrep.Domain` — entities (one class per file), enums, and domain logic: `Delivery/AttemptClock` (all timer and audio rules; throws `Common/DomainRuleException` for operations not allowed in the current state). No EF dependency.
- `ExamPrep.Infrastructure` — `Persistence/ExamPrepDbContext`, one `IEntityTypeConfiguration` per entity in `Persistence/Configurations`, migrations, `SeedData` (exam types, section templates, default branding "Victory"). `ModelConventions` stores every enum as `varchar(32)` text and every `DateTime` as UTC; `SnakeCaseNaming` lower-snake-cases all tables and columns. Provider is Oracle's `MySql.EntityFrameworkCore` 10 (`UseMySQL`), because Pomelo has no EF Core 10 release.
- `ExamPrep.Api` — ASP.NET Core host; `AddInfrastructure` fails fast if the connection string is missing. `Auth/` has two separate cookie schemes sharing `ApiCookieOptions` (HttpOnly, SameSite=Strict, 401/403 instead of redirects): **Staff** (`victory.staff`, policies `Staff`/`Admin`, first admin from `Bootstrap:AdminUsername`/`AdminPassword`) and **Candidate** (`victory.candidate`; login → `pin_verified=False`, `POST /api/candidate/auth/pin` re-issues it with `True`; policy `CandidateExam` requires the PIN). Both re-check `security_stamp` on every request; candidate login rotates it. Candidates can sign in only while `exams.status = Active`.
- `ExamPrep.Infrastructure/Content` — exam packages (one .zip per paper: `paper.json` + images/ + audio/, spec in `setup/exam-package/FORMAT.md`): `PaperPackageReader` (strict JSON, names confined to the package, size limits) → `PaperPackageValidator` (template part count, numbering 1..N, per-interaction rules; returns every error as an admin-readable sentence) → `PaperPackageImporter` (SHA-256 content-addressed `IAssetStorage` under `Storage:AssetsRoot`, `asset:` links → `/api/assets/{id}`, HtmlSanitizer, one transaction, paper saved as Draft). Exposed as `POST /api/admin/papers/import` (Admin). The synthetic samples in `setup/exam-package/samples` are also the test fixtures; `python -I setup/exam-package/pack.py <folder> [--placeholders]` zips one.
- `ExamPrep.Tests` — mirrors `src/`; `MySqlDatabaseFixture` + `A2ExamBuilder` for DB integration tests.

The schema has 21 tables in four groups: **content** (`exam_types → section_templates`, `test_papers → paper_parts → question_groups → questions → answer_keys`, `assets`), **sessions** (`exams → exam_sections / candidates / exam_invigilators`), **delivery** (`attempts`, `responses`, `annotations`, `manual_marks`), **monitoring** (`invigilator_actions`, `security_events`, `candidate_messages`), plus `users` (staff only) and `site_settings` (centre name + logo, admin-editable).

Key modelling decisions:
- Exam types and their ordered sections are **data** (`section_templates`), so A2 (2 sections), B1 (3) and later IELTS share one engine. Never hard-code a section list.
- Candidate delivery API (`Api/Delivery`): `POST/GET /api/candidate/sections/{id}/attempt` start or resume through `AttemptClock` and return the clock plus saved answers; `PUT .../responses` upserts answers (409 when not running; 5 s grace). `AttemptExpiryWorker` sweeps every 5 s and submits expired attempts (TimeExpired) so offline candidates still end on time. There is deliberately no candidate submit (Cambridge: wait for time to expire or the invigilator to end the test). Listening: `POST .../audio` sets `AudioStartedAt` once; `audioPositionMs` comes from `AttemptClock.AudioPosition`; the browser (`exam/listening/useRecordingPlayer`) plays the part tracks back to back with no controls and resumes at the server position after a reload.
- Sections run strictly in order (Cambridge: each component ends before the next; Listening is last). `CandidateSectionsController` reports a later section as `Locked` and refuses its paper (403) until every earlier section has an attempt with status `Submitted` (candidate submit, time expired or invigilator End now).
- `question_groups.interaction_type` drives the candidate UI: `SingleChoice`, `ImageChoice`, `InlineChoice` (word picker over a gap), `InlineText` (typed gap), `DragDropMatch` (word cards), `Essay` (word counter). Inline gaps live in `body_html` as `{{q:19}}`.
- Listening audio is one track per part (`paper_parts.audio_asset_id`); the player chains them into one continuous recording, as Inspera does.
- Server-authoritative timer on `attempts`: change it only through `AttemptClock`, which recomputes `ends_at` from `started_at`, section duration, `paused_seconds`, `end_adjust_seconds` (signed; End now / End at / End in — relative so a later pause still moves the end), `planned_extra_seconds` + `incident_extra_seconds` (dropped when `disregard_extra_time`), capped by `candidates.parent_end_override_at`. Listening position = time since `audio_started_at` minus `audio_adjust_ms` (pauses and rewinds).
- `concurrency_stamp` is rotated automatically by `ExamPrepDbContext.SaveChanges` for every modified attempt; a write based on a stale read throws `DbUpdateConcurrencyException` — reload and retry, never force.
- `users.security_stamp` / `candidates.security_stamp` invalidate sign-in cookies (candidate login rotates it, so the previous computer is signed out). `exam_sections.start_announced_at` only drives the "started early" warning; it never blocks a candidate.
- Value rules are also MySQL CHECK constraints (durations > 0, extra percent 0–100, non-negative timer fields, scores within max); cross-row rules (unique question numbers per paper, paper matches the section template) are validated by the importer.
- Auto-marking uses `answer_keys` (several rows = accepted alternatives). Writing has no key: teachers mark it in `manual_marks`.

`thesis/diagrams/erd.drawio` is generated from the live MySQL schema — after a migration run `python -I thesis/diagrams/tools/gen_erd.py` (repo root, MySQL container up); `tools/gen_use_case.py` regenerates the use case. Export from `thesis/diagrams`: `"/d/draw.io/draw.io.exe" -x -f png -e -s 2 -b 10 -o erd.drawio.png erd.drawio`, then `python -I ../../.agents/skills/drawio-skill/scripts/repair_png.py erd.drawio.png` (draw.io desktop is at `D:\draw.io`).

## Source of truth

- `thesis/refs/cambridge-digital/` — official Cambridge/Inspera exam-day and invigilator guides. Invigilator procedures are identical for A2, B1 and IELTS (all on Inspera; confirmed by the user 09/10/2026).
- `thesis/refs/inspera-ui-a2.md` — screen-by-screen notes of the official A2 Key for Schools sample tests: header icons, part tabs with "n of m", split panes, inline word picker, drag-and-drop matching, Options menu (contrast, text size, hide timer), notes/highlight. Build the candidate UI against this.
- `thesis/refs/ielts-format.md` — IELTS structure and question types.
- `thesis/doc/DeCuong_ChiTiet_DoAnChuyenNganh_TranHoangLong_v4.docx` (approved outline) — authoritative for the stack and goals; its exam levels and "self-authored content" are superseded by the decisions above.
- `thesis/doc/BaoCaoKyThuat_HeThongWebQuanLyThi_CambridgeDigital.docx` — earlier strategy report; its stack (Node/Go, PostgreSQL, Redis, 1,000 users) is superseded — do not follow it.

Read `.docx` with `python-docx`, `.pdf` with `pypdf`; legacy `.doc` needs UTF-16LE string extraction.

## Exam-room behaviour to reproduce (from the refs)

- **Candidate login:** printed username (first 5 letters of family name + first initial + candidate number) and password, then the session PIN the invigilator reads out (`exams.session_pin`). `exams.invigilator_password` exits the lockdown browser or moves a candidate to another computer.
- **Invigilator operations** happen at section level, only on candidates who are online and in progress: pause/resume (single, multi-select, whole room), incident extra time, Edit End Time (*End now / End at / End in*; End now offers "Disregard extra time" for candidates with extra time). End now at section level is irreversible.
- **Listening incident:** two separate actions — rewind audio by N minutes (only while the candidate is not paused, at most the minutes already played), then add the same minutes as incident extra time.
- **Ended at parent level by mistake:** restore with Edit End Time → "Use test end time" (`candidates.parent_end_override_at` back to null).
- **Offline candidates** cannot be paused; their clock keeps running and the invigilator adds incident time when they log back in.
- **SEB:** the server validates the Safe Exam Browser config key; only server-observable warnings are real (`security_events`).

## Repository conventions (faculty-mandated)

- Required folders: `progress-report/` (weekly reports from template `3_MAU_BAO_CAO_TIEN_DO_2026.doc`) and `thesis/` (`doc/`, `pdf/`, `html/`, `abs/`, `refs/`, plus `diagrams/`). Code in `src/`, installers/test data in `setup/`, Docker files in `docker/`.
- At least one commit per week — commit history is graded.
- `README.md` must stay current and include author contact info.
- Report: Times New Roman 13pt, 1.5 spacing, IEEE references; chapters Mở đầu, Tổng quan, Cơ sở lý thuyết, Hiện thực hóa, Kết quả, Kết luận. Documentation and report are in Vietnamese.
- Never commit Cambridge or Victory exam content, `docker/.env`, or `.agents/` (local AI tooling with an API key). Real papers live in `setup/exam-package/sources/<set>/` (git-ignored except its README; the official A2 2022 sample is `a2-key-fs-2022-sample/`); uploaded files in `App_Data/` (git-ignored). Stage files by path, never `git add -A`.
