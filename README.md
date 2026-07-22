# SLIC Life Events — Event Management System

A full-stack event management platform built with **ASP.NET Core Razor Pages** and **Oracle Database**, featuring role-based access, capacity-aware registration with automatic waitlisting, and QR-code ticketing with live camera check-in.

Built as a self-directed portfolio project during a software engineering internship, to deepen hands-on experience with the Razor Pages + Oracle stack beyond the internship's initial practice curriculum.

## Features

- **Authentication & roles** — cookie-based auth with Admin and Attendee roles, SHA-256 password hashing
- **Event management** — full CRUD for events (Admin only), public browsing for everyone
- **Registration with waitlisting** — attendees register for events; once capacity is reached, further registrations are automatically waitlisted
- **QR ticketing** — every registration generates a unique QR code, rendered client-side, that serves as the attendee's ticket
- **Check-in** — Admin-facing check-in page supports three paths to the same outcome:
  - Live camera QR scanning (in-browser, via `html5-qrcode`)
  - Manual check-in by name/email search (for lost or undisplayable tickets)
  - Attendance is tracked separately from registration, recording *how* each person was checked in (scan vs. manual)
- **Notifications** — in-app notifications on registration, waitlisting, and check-in
- **Admin dashboard** — at-a-glance stats (total/upcoming events, registered, waitlisted, checked-in) plus a full event management table

## Tech stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core Razor Pages (.NET 9) |
| Database | Oracle Database, accessed via `Oracle.ManagedDataAccess.Core` (raw ADO.NET, no ORM) |
| Auth | ASP.NET Core Cookie Authentication |
| QR generation | `qrcode.js` (client-side rendering) |
| QR scanning | `html5-qrcode` (browser camera access) |
| Styling | Custom CSS design system (no framework) |

## Database schema

Five tables: `USERS`, `EVENTS`, `REGISTRATIONS`, `ATTENDANCE`, `NOTIFICATIONS`. Key design choices:

- **Attendance is a separate table from registration**, not a status flag — this preserves a clean audit trail of *who registered* vs. *who actually showed up*, and records the check-in method (`Scan` / `Manual`).
- **Waitlisting logic lives in the application layer**, not a database trigger — `RegistrationService.RegisterAsync` counts active registrations against event capacity before deciding whether to register or waitlist. Easier to read, test, and change than equivalent PL/SQL.
- **QR tokens are opaque random GUIDs**, not encoded/signed data — the QR code carries no logic of its own; it's just a lookup key. All validation happens server-side against the `REGISTRATIONS` table.

See `setup.sql` for full DDL.

## Getting started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- An Oracle Database instance (Oracle XE, Oracle Cloud Autonomous DB, or any reachable Oracle instance)

### Setup

1. Clone the repo and restore dependencies:
```bash
   dotnet restore
```
2. Run `setup.sql` against your Oracle schema to create the required tables and sequences.
3. Update the connection string in `appsettings.json`:
```json
   "ConnectionStrings": {
     "OracleTest": "User Id=<user>;Password=<password>;Data Source=<host>:<port>/<service_name>;"
   }
```
4. Run the app:
```bash
   dotnet run
```
5. Register an account at `/Account/Register`, then promote it to Admin directly in the database:
```sql
   UPDATE users SET role = 'Admin' WHERE email = 'your-email@example.com';
   COMMIT;
```

### Testing the QR scanner

Camera access requires a secure context. On `localhost` this works out of the box. To test from a second device (e.g. a phone) over a local network, either:
- Run the app over HTTPS with a trusted dev certificate, or
- Use `dotnet run --urls "http://0.0.0.0:<port>"` and enable `chrome://flags/#unsafely-treat-insecure-origin-as-secure` (or the Edge equivalent) for your local IP on the scanning device.

## 📁 Project Structure

```text
EventManagementSystem/
├── Data/              # ADO.NET service classes (data access layer per entity)
├── Models/            # Plain Old CLR Objects (POCOs) with validation attributes
├── Pages/
│   ├── Account/       # Authentication (Login, Register, Logout)
│   ├── Admin/         # Administrative dashboard and controls
│   ├── Events/        # Event CRUD operations and public discovery catalog
│   ├── Registrations/ # User registration management and attendee check-in
│   ├── Notifications/ # System notifications and alerts
│   └── Shared/        # Reusable Razor layouts and partial views
├── wwwroot/
│   └── css/           # Custom design system and style assets
└── setup.sql          # Database DDL script and seed data
```
## Known limitations

- Notifications are in-app only — no email or SMS delivery
- No automated tests
- Password hashing is unsalted SHA-256, matching this author's earlier CRMConnect project; a production system should use a per-user salt or a purpose-built algorithm (BCrypt/PBKDF2)

## Author

**Dinith Tharusha**
IT undergraduate, SLIIT | Software Engineering Intern, SLIC Life
[Portfolio](https://diniththarusha.netlify.app) · [GitHub](https://github.com/diniththarusha) · [LinkedIn](https://linkedin.com/in/diniththarusha)
