# AI Tools Used

> Honest report. Items marked **[FILL IN]** must be completed by the student – they were not invented.

## Tools

| Tool | Version / plan | What it helped with |
|---|---|---|
| **Claude** (Anthropic, claude.ai) | Model configured for the final session: `claude-sonnet-5-5` (the serving model may differ). Plan: free plan. | Reviewed the uploaded project; replaced in-memory sample data with SQL Server code; added admin login (cookie auth + PBKDF2 hashing), room and booking management, booking overlap prevention with transactions; wrote `HotelBookingDB.sql`, the ER diagram, README and this file; in the final review session fixed small issues (see below) and compile-checked the project. |
| **GitHub Copilot Chat** in Visual Studio | Version not recorded. The `.vs` folder of the original ZIP showed two Copilot chat sessions about `BookingsController` and `Bookings/Create.cshtml`. | Earlier help on the booking controller/view. **[FILL IN: confirm what you used it for, or delete this row]** |
| Other tools | **[FILL IN, or delete this row]** | |

## Useful prompts
These are real wording taken from the prompts used in the Claude sessions:
1. *"Please inspect my uploaded project ZIP … Do not create a completely new project and do not overwrite my existing work unnecessarily. Keep the existing UI and project structure."*
2. *"Inspect the actual schema before writing SQL queries. Do not silently rename database columns or tables."*
3. *"Implement secure admin login and logout. Use secure password hashing and server-side validation. Do not hardcode real passwords."*
4. *"Prevent overlapping bookings for the same room … Use parameterized SQL queries and appropriate transactions."*
5. *"Never claim a successful build, database connection or test unless it was genuinely executed."* (final review session)

(The first four are quoted from the earlier session summary kept in the previous version of this file; **[FILL IN: check they match what you really typed]**.)

## What Claude did in the final review session
- Rebuilt the project in a Linux sandbox. The build succeeded with 0 errors/0 warnings, but only against a stand-in for `Microsoft.Data.SqlClient` (NuGet was blocked there). No SQL Server was available, so database features were **not** run.
- Fixes: "My Bookings" now needs email **and** phone (previously any email showed its bookings); removed a fake newsletter `alert()` button; fixed "About Us" link on non-home pages; removed three identical "gallery" thumbnails on the room page; past check-in dates rejected on the room search; friendly error page for database failures; friendly message if two guests book at the same instant (SQL deadlock); added sample guest + bookings and two CHECK constraints to the SQL script.

## Problems found earlier in the original code and fixed (earlier Claude session)
- `BookingsController` read rooms from a static C# list while saving bookings in SQL.
- "My Bookings" showed every guest's bookings; phone number from the form was never saved.
- Login/Register buttons were `alert()` placeholders; no authentication existed.
- `/Home/Error` was referenced but did not exist; the solution file was named `appsettings.json.sln`; mixed namespaces.

## Changes made by the student after reviewing the AI output
**[FILL IN – write only what you really did, for example: ran the SQL script in SSMS, changed the connection string, fixed errors you hit, changed text/colours, tested pages, created the .bak and screenshots.]**
