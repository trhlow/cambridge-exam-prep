# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Vietnamese university capstone ("Đồ án chuyên ngành", class DA23TTA): a web app that simulates a **Cambridge English computer-based exam session** for practice — candidates take mock tests, invigilators rehearse incident handling, centres run mock exams with their own content. No source code exists yet (`src/` is empty); the repo currently holds planning documents and references. When code is added, update this file with build/test/run commands.

Deadline: final report submitted 31/12/2026. Work weeks: 19–25/10, 09–15/11, 30/11–06/12, 21–27/12/2026.

## Source of truth

- **`thesis/doc/DeCuong_ChiTiet_DoAnChuyenNganh_TranHoangLong_v4.docx`** (approved outline) is authoritative for scope and stack.
- `thesis/doc/BaoCaoKyThuat_HeThongWebQuanLyThi_CambridgeDigital.docx` is an earlier strategy report. Its domain analysis (Parent/Child test, violation matrix, invigilator operations) is useful, but its stack (Node.js/Go, Socket.io, PostgreSQL, Redis, offline cache, 1,000-user load target) is **superseded** by the outline — do not follow it.
- `thesis/refs/cambridge-digital/` — official Cambridge/Inspera exam-day and invigilator guides; consult these for real-world workflow behavior.
- `thesis/refs/faculty-templates/0_MOT_SO_QUY_DINH_CHUNG_2026.doc` — faculty rules for the report and repo (see below).

Read `.docx` with `python-docx`; legacy `.doc` files need UTF-16LE string extraction.

## Planned stack (from the outline)

C# ASP.NET Core Web API on **.NET 10**, **SignalR** for real-time, **MySQL** via **EF Core**, **React** frontend, **Safe Exam Browser** (Windows only) as the lockdown client.

## Domain model and key design constraints

- **Two-tier exam:** an Exam (Parent) contains Sections (Child): Reading & Use of English, Writing, Listening. Each section is timed and monitored independently. Speaking is out of scope.
- **Attempt state machine:** not started → in progress → paused → submitted. Ending is irreversible for the candidate; the real system has a "re-open session" recovery for wrongly closed parents.
- **Server-authoritative timer:** the server owns remaining time; clients only display it. Pause/resume and extra time must adjust the server-side end time.
- **Invigilator operations:** pause/resume (single, multi-select, or whole room), add extra time, edit end time (*Use test end time / End now / End at hh:mm:ss / End in ±minutes*), disregard extra time, Listening incident flow (pause → reset audio track to the incident point → resume without losing answers).
- **Candidate UI:** login by PIN, notes, flag questions, font-size and contrast settings.
- **SEB integration:** server validates the Browser Exam Key / config key before allowing entry; violations (browser exited, disconnect, etc.) surface as alerts on the invigilator dashboard.
- **Roles:** admin, invigilator, candidate.
- **Out of scope:** scoring, real Cambridge content or integration, Mac/Chromebook, multi-server deployment. Sample data targets B2 First, C1 Advanced, C2 Proficiency.

## Repository conventions (faculty-mandated)

- Required folders: `progress-report/` (weekly progress reports, from template `3_MAU_BAO_CAO_TIEN_DO_2026.doc`) and `thesis/` (`doc/`, `pdf/`, `html/`, `abs/`, `refs/`). Code goes in `src/`, installers/test data/deployment notes in `setup/`, Docker files (if any) in `docker/`.
- At least one commit per week — commit history is graded. A progress report without matching commits counts as not done.
- `README.md` must stay current and include author contact info (email, phone).
- Report format: Times New Roman 13pt, 1.5 line spacing, IEEE references, chapters: Mở đầu, Tổng quan, Cơ sở lý thuyết, Hiện thực hóa, Kết quả, Kết luận.
- Documentation and the report are written in Vietnamese.

## Local-only files

`.agents/` holds local AI tooling (design skills and an MCP config containing an API key). It is gitignored — never commit it.
