# Fix plan for the review of 2 November 2026

> **For agentic workers:** use superpowers:subagent-driven-development. Each task below is one implementer + one reviewer. Item numbers (A1, B3, …) refer to `.planning/2026-11-02-review/findings.md`: read the row for each item you touch. A row marked `~` was reported, not confirmed: check it against the code first, and if it is wrong, say so in your report instead of "fixing" it.

**Goal:** close every finding of the end-to-end review.

**Tech stack:** .NET 10, EF Core + MySql.EntityFrameworkCore on MySQL 8.4, xUnit v3; React 19, TypeScript, Vitest, Playwright. Read `CLAUDE.md` first.

## Global Constraints

- TDD: a failing test first for every behaviour change, watched failing for the right reason, then the code. CSS-only and documentation changes are exempt.
- Work on `main` (the owner's way of working). Commit each finished piece; **never push**.
- Commit messages carry no AI attribution (no `Co-Authored-By`). Stage files by path, never `git add -A`.
- Never stage `progress-report/BaoCaoTienDo_Tuan02_TranHoangLong.docx`, the untracked `thesis/doc/Ý tưởng đề tài…docx`, `docker/.env`, `.agents/`, or anything under `setup/exam-package/sources/` or `App_Data/`.
- Do not run prettier. Match the surrounding code: comment density, naming, one entity or configuration per file.
- Staff and candidate UI text is English. Docs are Vietnamese.
- Change `attempts` timers only through `AttemptClock`. `DbUpdateConcurrencyException`: reload and retry, never force. Exam types and sections are data: never hard-code a section list.
- Any schema change is an EF migration: from `src/backend`, with `ConnectionStrings__ExamPrep="server=localhost;port=3306;database=examprep;user=examprep;password=$MYSQL_PASSWORD"` (load `docker/.env`), `dotnet ef migrations add <Name> -p src/ExamPrep.Infrastructure -s src/ExamPrep.Api -o Persistence/Migrations`. Read the generated file and keep existing data. dotnet is in `$USERPROFILE/.dotnet` and the ef tool in `$USERPROFILE/.dotnet/tools`.
- Backend tests: from `src/backend`, `set -a; . ../../docker/.env; set +a; EXAMPREP_TEST_MYSQL="server=localhost;port=3306;user=root;password=$MYSQL_ROOT_PASSWORD" dotnet test [--filter-class "*Name*"]`. The full suite takes 10–15 minutes: run the classes you touched, then the full suite once before your last commit, in the background. Frontend: from `src/frontend`, `npx tsc -b --noEmit`, `npm run lint`, `npx vitest run`.
- Existing tests that assert the old wrong behaviour are updated in the same commit, with the reason in the commit message.

---

### Task 1: No lost answers (A1, A2, A3, B10)

**Files:** `src/frontend/src/exam/autosave.ts`, `autosave.test.ts`, `useAttempt.ts`, `ExamScreen.tsx`, `exam/questions/*`; `src/backend/src/ExamPrep.Api/Delivery/AttemptExpiry.cs`, `CandidateAttemptsController.cs`; tests beside them.

- A1: a 409 for a paused attempt must not close the queue. The server marks it (ProblemDetails extension `code: "paused"`, the finished case `code: "finished"`); `ApiError` exposes the code. On paused the queue keeps every pending answer, shows `saving`, retries every `retryMs` and syncs the clock. 401, 403 and finished still close. While paused the exam `<main>` is `inert`, so nothing can be typed under the overlay.
- A2: one grace, `AttemptExpiry.AutosaveGrace` (5 s), used by every path that submits on time: the sweep (`EndsAt <= now - grace`), heartbeat and GET attempt. The clock the candidate sees is unchanged.
- A3: debounce gets `maxWaitMs` 3000 so continuous typing still saves. When the attempt turns Submitted, or on unmount, flush once before disposing. Pending answers are mirrored in `localStorage` under a key with exam, candidate and section, removed when acknowledged, replayed on mount (still-running attempts only) and sent with `fetch(..., { keepalive: true })` on `pagehide`.
- B10: essay and typed inputs get `maxLength` equal to the server's 20 000.

### Task 2: Clock and invigilator rules (A4, A11, B4, B5, B6, B7, B8, B9, plus small items)

**Files:** `src/backend/src/ExamPrep.Domain/Delivery/AttemptClock.cs`, `src/backend/src/ExamPrep.Api/Monitoring/*`, `Api/Delivery/CandidateAttemptsController.cs`, `Api/Security/SecurityEventRecorder.cs`, `Api/Exams/StaffExamsController.cs`; `src/frontend/src/staff/pages/MonitorPage.tsx`, `MonitorDialogs.tsx`, `ExamPage.tsx`; tests.

- A4: End now only ever brings the end forward: `EndAdjustSeconds += Math.Min(0, floor(now - StandardEnd))`, and a second End now changes nothing.
- End now outcome text says how many ended and how many keep working on extra time, and that paused candidates with extra time were resumed.
- A11: the incident-time dialog after a rewind applies only to candidates actually rewound (selected minus skipped).
- B4: implement the parent-level end from the invigilator guide (read `thesis/refs/cambridge-digital/Invigilator_Training_Test_User_Guide.pdf` with pypdf): `POST /api/staff/exams/{id}/end-all` with `candidateIds` (empty = everyone), for `MonitorableBy`, sets `candidates.parent_end_override_at = now`, recomputes running attempts through `AttemptClock`, logged in `invigilator_actions`, two-click confirm on the exam page. "Use test end time" clears the override: the clearing happens in the action code, which has the candidate.
- B5: Resume refuses offline candidates as Pause does, with the skip reason shown. Paused candidates who are offline stay paused and are not swept.
- B6: `MonitorPage` keeps one load in flight; a `sectionChanged` during it sets a dirty flag that triggers one more load afterwards. A response is applied unless a newer one already was. The 20 s timer is not restarted by each message.
- B7: heartbeat writes with one conditional update and notifies when the row changed from offline or stale to online.
- B8: `DbUpdateConcurrencyException` on candidate endpoints: reload and retry once, then 409 with a sentence. Never a 500.
- B9: a resolved warning of the same type on the same attempt is not raised again for 30 minutes.
- Rewind cannot exceed what was played of the recording, and the monitor's position stops at the recording's end, when `assets.duration_ms` is known for every part.
- Message dialog: busy guard against a double send. Monitor `sectionChanged` send failures keep the dirty flag (`RoomNotifier.cs:62`).

### Task 3: Listening player (A9, A10)

**Files:** `src/frontend/src/exam/ExamScreen.tsx`, `exam/listening/useRecordingPlayer.ts`, tests.

- A9: when the status goes Paused → InProgress and the recording had started, it restarts by itself at the server position, with no click. The browser's autoplay rule is satisfied because the candidate already pressed Play; if `play()` is still refused, show the existing "Continue listening" button as a fallback.
- A10: a rewind after the recording finished plays again: `finished` is left when the server position is below the total length.
- Check the start/stop race during a track load (pause while loading must not leave audio playing).

### Task 4: Marking (A5, A7, A8, A13, B11, C2)

**Files:** `src/backend/src/ExamPrep.Api/Results/StaffResultsController.cs`, `Infrastructure/Marking/MarkingAllocator.cs`, `Api/Content/ExamAssetIndex.cs`; `src/frontend/src/staff/pages/MarkingPage.tsx`, `ResultsPage.tsx`, `resultsCsv.ts`, `StaffAssignmentPanel.tsx`; tests.

- A5: the marking view returns, per Writing question, its prompt, its stimulus and the part's passage from `attempt.test_paper_id`. `MarkingPage` shows them sanitised (DOMPurify as the exam screen does).
- C2: an examiner's assets are only those of the Writing parts of scripts dealt to them.
- A7: a Final script stays with its examiner even if they leave the session. Part-marked scripts of a leaving examiner are dealt again and keep their marks. `manual_marks` keeps history: never delete, the latest by `marked_at` counts everywhere, including `RecomputeAsync`.
- A8: saving a mark and recomputing is one transaction; on a concurrency conflict reload and retry once. Double submit is harmless because the latest counts.
- A13: CSV gets a grading-status column per section so a provisional score cannot pass for final.
- B11: dealing considers only active accounts whose role is Examiner; `EnsureAllAsync` also finds scripts whose examiner is inactive or no longer on the session, and skips exams with no eligible examiner. Results shows a warning when scripts have no examiner. The marking endpoint answers 404 for a section without Writing.
- `StaffAssignmentPanel` help text says what examiners do now.

### Task 5: Exam lifecycle and seats (B1, B2, A12, B13, B14, B16 seats, C4)

**Files:** `src/backend/src/ExamPrep.Api/Admin/AdminExamsController.cs`, `AdminCandidatesController.cs`, `Api/Delivery/*`, `Api/Exams/StaffExamsController.cs`, `Api/Content/ExamAssetIndex.cs`; `src/frontend/src/App.tsx`, `screens/SignInScreens.tsx`, `exam/annotations/*`, `staff/pages/ExamPage.tsx`; tests.

- B1: transitions allowed: Draft ↔ Scheduled, Scheduled → Active, Active → Scheduled only with no attempts, Active → Closed, Closed → Active for administrators only. Any other answers 409 with a sentence. To Active needs at least one candidate and one invigilator (400 listing what is missing). The page confirms with two clicks.
- B2: Close is refused while any attempt is InProgress or Paused (409 saying how many). `Start` refuses unless the exam is Active.
- A12: going Active fills `seat_no` of unseated candidates from their list place, so later changes cannot shift anyone. Once any attempt exists: `seats_per_row` cannot change (409), and a new candidate must have a free numeric seat. Room plan and candidate list show the version of the attempt when one exists, the seat's otherwise.
- B16: a seat already taken in the session is refused (400).
- C4: the paper is served only for a running attempt (InProgress or Paused); the frontend already starts before reading, verify it. A candidate's assets are those of the paper of a running attempt only. An uploaded picture of another version or a locked section answers 404.
- B13: Exit clears the candidate's notes and highlights from `sessionStorage`.
- B14: Open skips the readiness check when the section is InProgress or Paused.

### Task 6: Papers (A6, B3, B15, B16 matcher, C1, C5, points)

**Files:** `src/backend/src/ExamPrep.Infrastructure/Content/PaperSkeletons.cs`, `PaperPackageImporter.cs`, `Api/Content/*`, `Api/Approvals/*`, `Api/Admin/AdminExamsController.cs`, `Domain/Marking/AnswerMatcher.cs`, `Domain/Papers/PaperShape.cs`, `Infrastructure/Marking/AttemptMarker.cs`; `src/frontend/src/staff/pages/PaperEditors.tsx`, `PaperPage.tsx`, `PapersPage.tsx`, `ExamFormPage.tsx`; tests.

- A6: blank papers start with no answer on choice, card and typed questions, and publishing refuses "Question n: the answer is not set.". The importer takes this only from the blank path; ordinary packages still need answers.
- A6 repair: an administrator can correct the answers of a published paper: `PUT /api/admin/papers/{id}/questions/{n}/answers`, logged, re-marks that question on every submitted attempt on that paper, and scores are recomputed. Page: a button on the paper's page, admin only, with a two-click confirm.
- B3: the group editor edits `itemsHeading`, `poolHeading` and each card's `title`, so a blank B1 Reading paper can be published from the portal.
- B15: a mix used by no section can be deleted through the two-admin approval (new `ApprovalKind.DeleteMix`, migration for the enum CHECK), freeing its sources. "Used by" counts sessions through a mix and shows when a paper is a mix source.
- B16: `AnswerMatcher` trims again after dropping the final full stop ("cat ." matches "cat").
- `PaperShape` also compares points per question.
- C1: every `/api/assets/{id}` inside HTML saved by the editor or import must be already used by that paper or uploaded by the person; otherwise 400. `ExamAssetIndex` keeps reading HTML.
- C5: `exam-papers` and `exam-paper-mixes` return only what the exam form uses (id, title, section, status, version count). Check the frontend and the room plan before trimming. `assignable-staff` returns active accounts plus those already assigned.

### Task 7: Access and accounts (C3, C6, C7, edit-exam route, hubs)

**Files:** `src/backend/src/ExamPrep.Api/Approvals/ApprovalActions.cs`, `Exams/ExamAccess.cs`, `Content/AssetsController.cs`, `Realtime/RoomHubs.cs`; `src/frontend/src/staff/StaffApp.tsx`, `pages/ExamFormPage.tsx`; tests.

- C3: a role change removes the account's `exam_invigilators`, `exam_examiners` and `exam_candidate_managers` rows as fit, and `ExamAccess` filters also require the current role.
- C6: `MayReadAsync` decides from the Staff identity explicitly; verify whether a request with both cookies confuses the id, and test it.
- C7: deactivating an administrator cancels their pending requests. The login lockout stays as designed; a line goes in `setup/INSTALL.md` on unlocking (wait 5 minutes).
- `StaffRoomHub.WatchSection` checks the account from the database (active, stamp) rather than the connection's claims.
- The edit-exam route shows "not part of your role" when `canOrganise` is false.

### Task 8: Deployment, performance, documents (D1–D6, B12, E, remaining minors)

**Files:** `docker/*`, `src/backend/Dockerfile`, `src/frontend/nginx.conf`, `src/backend/src/ExamPrep.Api/Demo/DemoSeeder.cs`, `Program.cs`, `Api/Content/AdminPaperConversionController.cs`, `PaperConversion.cs`, `Infrastructure/Marking/*`, `Infrastructure/Persistence/Configurations/AttemptConfiguration.cs`, migration `MixManyPapers.cs` comment, `README.md`, `CLAUDE.md`, `setup/INSTALL.md`, `src/frontend/README.md`.

- D1, D2: the base compose stays the demo (the owner's commands and the Word guide use it). New `docker/docker-compose.prod.yml` override: Production, Demo off, HTTPS in nginx on 443 from `docker/certs/` (git-ignored; `setup/INSTALL.md` gives the openssl command to make a certificate for the LAN address). INSTALL states plainly that the demo stack must not hold a real exam, and how to run the production one. Verify sign-in over HTTPS with the override.
- D3: MySQL binds `127.0.0.1:${MYSQL_PORT}`. Compose must not break e2e.
- D4: documents say what reset does. Reset only ever touches exams the seeder made (mark them, not by name alone).
- B12: seeder failures are logged and do not stop the API; a renamed demo exam does not crash-loop.
- D5: migration adds indexes on `attempts` `(status, ends_at)`, `(status, grading_status)`, `(last_heartbeat_at)`; `AttemptMarker` loads questions once per paper.
- D6: backup and restore in INSTALL dump inside the container and `docker cp`, with no PowerShell pipe.
- Minors: the exam form's default date is the local one; conversion temp folder inside `try`, malformed report JSON gives 502 with a sentence; API image runs as a non-root user (`App_Data` writable); `Down` comment in `MixManyPapers`.
- E: fix every false statement listed in findings.md section E, and add what Tasks 1–7 changed to `CLAUDE.md` and `README.md`.
