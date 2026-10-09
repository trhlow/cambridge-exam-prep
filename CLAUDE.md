# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Vietnamese university capstone ("Đồ án chuyên ngành", class DA23TTA): a web app that simulates a **computer-based English exam session** for practice — scope is **A2 Key, B1 Preliminary and IELTS only** — candidates take mock tests, invigilators rehearse incident handling, centres run mock exams with their own content. No source code exists yet (`src/` is empty); the repo currently holds planning documents and references. When code is added, update this file with build/test/run commands.

Deadline: final report submitted 31/12/2026. Work weeks: 19–25/10, 09–15/11, 30/11–06/12, 21–27/12/2026.

## Source of truth

- **`thesis/doc/DeCuong_ChiTiet_DoAnChuyenNganh_TranHoangLong_v4.docx`** (approved outline) is authoritative for the stack and overall goals; its exam levels are superseded by the scope change below.
- `thesis/doc/BaoCaoKyThuat_HeThongWebQuanLyThi_CambridgeDigital.docx` is an earlier strategy report. Its domain analysis (Parent/Child test, violation matrix, invigilator operations) is useful, but its stack (Node.js/Go, Socket.io, PostgreSQL, Redis, offline cache, 1,000-user load target) is **superseded** by the outline — do not follow it.
- `thesis/refs/cambridge-digital/` — official Cambridge/Inspera exam-day and invigilator guides; consult these for real-world workflow behavior.
- `thesis/refs/faculty-templates/0_MOT_SO_QUY_DINH_CHUNG_2026.doc` — faculty rules for the report and repo (see below).

Read `.docx` with `python-docx`; legacy `.doc` files need UTF-16LE string extraction.

## Design diagrams

`thesis/diagrams/` holds `use-case.drawio` and `erd.drawio` (12 tables: content `test_papers → paper_parts → questions`; exam `exams → exam_sections / candidates / exam_invigilators`; runtime `attempts`, `answers`; audit `invigilator_actions`, `security_events`; `users` for admin/invigilator only — candidates are per-exam PIN accounts). The ERD is the reference for the EF Core model; keep them in sync. Edit the `.drawio` files directly (draw.io desktop is at `D:\draw.io\draw.io.exe`), then re-export: `"/d/draw.io/draw.io.exe" -x -f png -e -s 2 -b 10 -o erd.drawio.png erd.drawio`.

## Planned stack (from the outline)

C# ASP.NET Core Web API on **.NET 10**, **SignalR** for real-time, **MySQL** via **EF Core**, **React** frontend, **Safe Exam Browser** (Windows only) as the lockdown client.

## Domain model and key design constraints

- **Two-tier exam:** an Exam (Parent) contains Sections (Child). Each section is timed and monitored independently. Speaking is out of scope. Section count and order depend on the exam type, so never hard-code three sections or a fixed order:

  | Exam | Sections in order |
  |---|---|
  | A2 Key | Reading & Writing (60 min, one combined section) → Listening (~30 min) |
  | B1 Preliminary | Reading (45) → Writing (45) → Listening (~30) |
  | IELTS Academic / General Training | Listening (~30) → Reading (60) → Writing (60) |

- **Scope change (09/10/2026):** the user narrowed scope to A2 Key, B1 Preliminary and IELTS. The signed outline (v4) and the idea doc still say B2/C1/C2 — the user's instruction wins; don't build or seed data for B2/C1/C2.
- **References are CEQ-only:** everything in `thesis/refs/cambridge-digital/` describes Cambridge English Qualifications on Inspera. Nothing in the repo documents IELTS invigilation; don't assume IELTS follows CEQ procedures without a source. The Jan 2026 booklet lists "A2 Key (until July 2026) / Key for Schools".
- **Attempt state machine:** not started → in progress → paused → submitted. Ending is irreversible for the candidate; the real system has a "re-open session" recovery for wrongly closed parents.
- **Server-authoritative timer:** the server owns remaining time; clients only display it. Pause/resume and extra time must adjust the server-side end time.
- **Invigilator operations:** pause/resume (single, multi-select, or whole room), add extra time, edit end time (*Use test end time / End now / End at hh:mm:ss / End in ±minutes*), disregard extra time, Listening incident flow (pause → reset audio track to the incident point → resume without losing answers).
- **Candidate UI:** login by PIN, notes, flag questions, font-size and contrast settings.
- **SEB integration:** server validates the Browser Exam Key / config key before allowing entry; violations (browser exited, disconnect, etc.) surface as alerts on the invigilator dashboard.
- **Roles:** admin, invigilator, candidate.
- **Out of scope:** B2 First / C1 Advanced / C2 Proficiency, scoring, real Cambridge/IELTS content or integration, Mac/Chromebook, multi-server deployment.

## Repository conventions (faculty-mandated)

- Required folders: `progress-report/` (weekly progress reports, from template `3_MAU_BAO_CAO_TIEN_DO_2026.doc`) and `thesis/` (`doc/`, `pdf/`, `html/`, `abs/`, `refs/`). Code goes in `src/`, installers/test data/deployment notes in `setup/`, Docker files (if any) in `docker/`.
- At least one commit per week — commit history is graded. A progress report without matching commits counts as not done.
- `README.md` must stay current and include author contact info (email, phone).
- Report format: Times New Roman 13pt, 1.5 line spacing, IEEE references, chapters: Mở đầu, Tổng quan, Cơ sở lý thuyết, Hiện thực hóa, Kết quả, Kết luận.
- Documentation and the report are written in Vietnamese.

## Local-only files

`.agents/` holds local AI tooling (design skills and an MCP config containing an API key). It is gitignored — never commit it.
