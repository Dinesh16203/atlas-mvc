# ATLAS requirements and test plan

This inventory combines the requirements document with the five screen designs in the PowerPoint. Text inside the attachments was treated as product scope and design reference; it does not override the user's chosen implementation stack. The Word document's UI and API technology fields are TBD and its storage row lists SharePoint / SQL Server / Azure SQL as indicative options. This local build uses React, ASP.NET Core MVC controllers, EF Core, and SQLite as requested.

## Pages and workflows

| Page / workflow | What it contains |
|---|---|
| Sign in | Local Identity login, password visibility control, demo account shortcuts, inline error state. |
| Home dashboard | Period, team, domain, and commercial model filters; hours, capacity, submitted, approvals, exceptions, utilization; weekly effort, model mix, reminders, quick actions, recent activity. |
| Time entry | Daily editor and weekly grid; date, employee, domain/team, task catalogue, activity/track/phase, work item, project/service, release/sprint/environment, hours, realized flag, comments, exception reason, derived reviewer/contract/bill ID/cost center/billing treatment. Recent entries and favorite work items are included. |
| Weekly timesheet | Week selection, daily columns, capacity/variance, validation, save all, add/remove rows, copy prior week, distribute weekly hours across weekdays, comments, submit, withdraw, and print. |
| Submissions and approval | Period/team/domain/reviewer/status/model/search filters; pending/returned/rejected/approved KPIs; entry detail, comments, decision history, approve/reject/return/request clarification, and required decision reason. |
| Team | Team compliance and submission follow-up for the signed-in lead/owner scope. |
| Resource planning | Team/domain capacity and effort view for planning. |
| My work | Searchable personal effort history with navigation to associated weeks. |
| Projects and services | Search and view project, domain, commercial, contract, billing, and cost center metadata. |
| Work items | Search and filter the local work item list by type and inspect linked project, release, sprint, and environment. |
| Reports and Insights | Date, domain, team, employee, project, model, and approval-status filters; effort/utilization summaries, trends, breakdowns, heatmap and alerts; CSV export and browser print-to-PDF. |
| Financials | Invoice-hour and MD comparisons, project billing metadata, date/domain/team/model filters, invoice record creation/update for administrators, CSV export. |
| Audit and compliance | Searchable audit history, weekly compliance counts, CSV export. |
| Administration | User and role CRUD, active state, team/domain association, and domain/team/project/work item/task catalogue maintenance and retirement. |
| Global shell | Responsive navigation, role-based links, notifications, account menu, loading/error/empty states, and persisted dark/light mode. |

### Screens visible in the design deck

1. Home dashboard with filters, KPI cards, weekly effort, commercial model split, reminders/actions, and recent activity.
2. Daily entry form with activity classification, linked work item/project fields, actual hours, comments, exception and derived billing metadata.
3. Weekly grid with required capacity, variance/validation, copy/add/save/submit/withdraw/export actions.
4. Submission review with filters, summary KPIs, detailed entries and history, comments, approval decisions and reason.
5. Executive effort/utilization analytics with filters, KPI cards, trends, model mix, heatmap, project ranking and quality alerts.

## Roles

| Role | Intended visibility and actions |
|---|---|
| Team Member | Own time entries and dashboard/history; submit own week; view shared reference data. |
| Team Lead | Own work plus team-scoped review/compliance; may enter and edit for team members. |
| Domain Owner | Domain-scoped dashboards/effort and submission review; administers domain catalogues in source requirements, though local catalogue writes are currently Administrator-only. |
| Administrator | Manage users, roles, teams, domains, projects, work items and task catalogue; organization audit; invoice records. |
| Management | Consolidated reports, financial view and cross-domain dashboards; no local master-data administration. |

API endpoints enforce authenticated identity and role/scope checks. Navigation hides pages that are not offered to a role, but server authorization is the security boundary. The seeded demo has a small sample of users and work data, not 300 production accounts.

## Business rules implemented

- A time entry references a task catalogue item, work item, project/service, employee, team/domain scope, date, commercial model, activity details, and actual hours.
- Entry hours must be 0.25–8.00 per record in 0.25-hour increments; future-dated work is rejected; a user's daily total may not exceed 8 hours.
- Users cannot modify other users' entries. Team Leads can add/update entries for users in their own team; Administrators can maintain users and data. Older months are locked for ordinary users.
- Task selection supplies its activity classification and commercial model to the entry. The stored work item identifier permits aggregation across multiple teams' contributions.
- Weekly submission requires at least one entry. A week above 40 hours needs an exception reason; entries still retain the 8-hour per-day cap. A reviewer cannot approve their own submission. Review decisions require a reason and are audited.
- Submissions can be drafted, submitted, withdrawn for correction, approved, rejected, returned, or sent for clarification. Comments and decisions are retained with the submission.
- Finance records require a valid project and non-negative invoice hours/MDs; duplicate project/month records are rejected.
- Create/update/delete and administrative changes write audit events. Notifications are in-app and users can mark them read.
- The report's `invoiced` / billed effort comparisons are estimates from approval and billing classification when no invoice record is present; the Financials page holds explicit invoice records.

### Design decision where sources conflict

The weekly design says to add an exception reason if a day exceeds 8 hours, while the same design and the rules enforced by the API cap each daily entry at 8 hours. This build follows the enforceable cap and allows a reason for a weekly total above 40 hours. Confirm whether policy should instead allow a daily overage with a reason before production rollout.

## Requirement-to-feature checklist

### User Functional Requirements

| ID | Requirement | Implementation / status |
|---|---|---|
| UFR-01 | Log hours against a predefined activity/task. | Implemented in daily and weekly entry; server validates catalogue association. |
| UFR-02 | Associate entries with incident/problem/change/SSR/task IDs. | Implemented through typed local work items (incident, SSR, change, defect/story/task and non-ticketing examples). |
| UFR-03 | Select activities from task catalogue. | Implemented; catalogue is managed in Administration. |
| UFR-04 | View/update own entries subject to permissions. | Implemented in Time Entry and My Work; API checks ownership and edit state. |
| UFR-05 | Query by activity, incident, domain, team, or person. | Implemented through report filters, work item search, and drill-down. |
| UFR-06 | Drill from summaries to detailed records. | Implemented in Reports and Approvals detail panels. |
| UFR-07 | Personal productivity/utilization dashboard. | Implemented in Home and Reports. |
| UFR-08 | Team lead dashboards and roll-up reports. | Implemented in Team, Reports, and Insights with role-scoped data. |
| UFR-09 | Export filtered reports to Excel and PDF. | Partial: filtered CSV and browser print-to-PDF work. Native `.xlsx` export is not implemented. |
| UFR-10 | MTTR, incident aging, SPI, and CPI. | Not implemented: definitions, formulas, lifecycle timestamps, and project-plan/cost data are not specified or supplied. |
| UFR-11 | Consolidate effort across teams for one work item. | Implemented by shared work item identifiers and report aggregation; seed data demonstrates cross-team activity. |
| UFR-12 | Domain effort/utilization trends. | Implemented in Reports/Insights filters and charts. |
| UFR-13 | Prevent updates to previous months and other users' data. | Implemented for ordinary users; Team Lead team edits and Administrator maintenance follow UFR-14 and admin scope. |
| UFR-14 | Leads can enter/update their team's entries. | Implemented with team scope enforced by API. |
| UFR-15 | Associate team leads and members. | Implemented via team membership and reviewer assignment on submissions; there is no separate configurable lead-member mapping table yet. |
| UFR-16 | Management cross-team/domain reports. | Implemented with consolidated Reports/Insights scope. |
| UFR-17 | Executive report export. | Partial: filtered CSV and print-to-PDF from browser; no formatted native Excel/PDF generator. |
| UFR-18 | Weekly reminders with three reminders. | Partial: in-app notification center exists; no scheduled reminder engine or three-reminder policy. |
| UFR-19 | Two reminders and lead escalation for missing time. | Not implemented: schedule, escalation timing, subject lines, and delivery service are unspecified; no email/background scheduler. |
| UFR-20 | Classify activity as T&M, Non-T&M, or Out. | Partial: the task catalogue automatically applies and stores the seeded T&M / Non-T&M / Out labels; domain owners must approve the production mapping for each activity. |
| UFR-21 | Weekly/monthly/user/team/overall classification reports. | Partial: reports filter by commercial model and date and group by user/team/domain; the exact T&M/Non-T&M/Out mapping remains to be confirmed. |

### Admin Functional Requirements

| ID | Requirement | Implementation / status |
|---|---|---|
| AFR-01 | Create/update/deactivate/manage users. | Implemented in Administration. |
| AFR-02 | Define/manage the five roles. | Implemented with ASP.NET Identity roles; Administrator assigns role. |
| AFR-03 | Create/manage teams and hierarchies. | Teams can be created/edited; team hierarchy/parent-child structure is not implemented because no hierarchy rules are supplied. |
| AFR-04 | Maintain domains. | Implemented in Administration; seed domains are placeholders from the source document. |
| AFR-05 | Manage team-specific task catalogue. | Implemented through task CRUD/retire and team/domain scoping. |
| AFR-06 | Configure RBAC and visibility. | Role and row scopes are enforced in API code; no editable policy designer. |
| AFR-07 | Configure widgets, report views, KPI calculations. | Not implemented; the current dashboard is fixed to the supplied design. |
| AFR-08 | Organization-wide reports. | Implemented in Reports/Insights for Administrator and Management; export limited to CSV/print. |
| AFR-09 | ITSM integration/validation. | Not implemented; no ServiceNow credentials/API contract or field mapping was provided. |
| AFR-10 | Configure MTTR/SPI/CPI/utilization/aging formulas. | Partial: utilization is calculated; configurable KPI formulas and other operational metrics need definitions and source data. |
| AFR-11 | Maintain audit logs. | Implemented for entry, submission, finance and admin mutations. |
| AFR-12 | Schedule reports and distribution. | Not implemented; requires schedule policy and mail/service configuration. |
| AFR-13 | Maintain domains, categories, teams, reporting mappings. | Implemented for local master data; there is no separate configurable report-mapping editor. |

### Non-functional targets

| Source target | Current local build |
|---|---|
| ~300 named users / concurrency | Relational schema and role scopes are suitable for a small pilot. No 300-user load test has been run. |
| Save under 2 seconds; report under 5 seconds | Not benchmarked. SQLite is appropriate for local single-user evaluation, not a claim of production concurrency. |
| 99.5% business-hours availability | Local application has no hosting/availability guarantee. |
| SSO, RBAC, audit, privacy | Local cookie/Identity login, RBAC, and audit are implemented. Corporate SSO and organizational data-governance review remain. |
| Entry in under 30 seconds | The form uses catalogue selections and quick actions, but this target has not been measured. |
| Scale beyond 300 | No production deployment or load test. A hosted database may be required for concurrent use. |

## Ambiguities and decisions needed before production

1. **Daily over-capacity conflict:** the deck suggests an exception reason above 8 hours/day, while its validation text and the API rule cap daily entries at 8. Confirm the governing policy.
2. **Classification taxonomy:** define the mapping between activity/task and T&M, Non-T&M, and Out, including whether this is independent from commercial models such as Fixed Price or Managed Services.
3. **Operational KPI formulas:** define MTTR, aging clock start/stop, SPI, CPI, utilization denominator, and holiday/leave/part-time capacity treatment. Provide required source fields.
4. **Reminder policy:** confirm reminder days/times, weekly cutoff, number of attempts, escalation conditions, recipients, time zone, email provider, and templates.
5. **SSO and user source:** provide Entra ID/OIDC tenant settings, provisioning/deprovisioning source, group/role mapping, and account identifier; the current local demo uses email/password.
6. **ITSM:** provide ServiceNow/ITSM API access, record types, validation rules, rate limits, and expected behavior when the service is unavailable.
7. **Domains, teams, users:** validate the 19 placeholder domain names and seed teams/users. The design document calls these placeholders and elsewhere mentions both nine domains/~200 users and ~300 users.
8. **Team lead association:** decide if one team can have multiple leads, whether members can report to multiple teams, and how a substitute reviewer is selected.
9. **Approval semantics:** define who may approve, reject, return, or request clarification; whether approval is final; allowed post-approval correction; and SLA/overdue thresholds.
10. **Export formats:** specify whether real `.xlsx` and generated PDF files are required, report layout, branding, and data volume.
11. **Finance/invoice semantics:** define actual vs approved vs billable vs invoiced calculations, billing periods, currency, invoice corrections, and approval workflow.
12. **Production hosting:** select hosting, database, backup/retention, encryption, monitoring, and availability approach. SQLite is configured for this local build only.

## Manual acceptance checklist

Use a fresh seeded database the first time. Sign in using each demo role where a step mentions a role.

- [ ] Sign in as member; Home loads; use both light and dark mode; refresh and confirm theme preference persists.
- [ ] Create a daily entry with a catalogue task, work item, project, 1.25 hours, and comments; save, edit, and delete it; confirm API errors show inline.
- [ ] Try 0, 0.1, 8.25, future date, and daily total over 8 hours; confirm browser/server validation prevents invalid entries.
- [ ] Mark a work item favorite, use Add from Favorites, and verify the selected work item is prefilled.
- [ ] In Weekly View, add a row, distribute 7.75 hours, change a comment, Save All, reload, and verify all five daily values and comment persist.
- [ ] Copy a previous week; confirm the copied row is a draft until Save All; submit a saved week; withdraw it as its owner.
- [ ] Attempt to read another member's entries as a member; confirm the API denies access. As a Team Lead, select a same-team member and verify team entry edits are allowed.
- [ ] As Team Lead, filter submissions by week/team/domain/status/model; open a detail; add a comment; approve/return with a reason; verify decision history and audit trail.
- [ ] As a reviewer, attempt to decide your own submission; confirm the API blocks self-approval.
- [ ] In Reports, filter by date/team/domain/person/project/model/status; drill into rows; export CSV and print/save as PDF.
- [ ] As Administrator, create/edit/deactivate a user; create/edit/retire a team/domain/project/work item/task; verify a retired item is not available for new entry.
- [ ] As Administrator, add and edit a monthly invoice record; verify a duplicate project/month and negative hours are rejected.
- [ ] As Management, verify consolidated reports and financial view load; verify admin mutation controls are unavailable.
- [ ] As Administrator, search Audit & Compliance and export the audit log; confirm entry, approval, finance and master-data events appear.

## Not included in the local acceptance claim

The local app does not send email, run background reminder schedules, use corporate SSO, call ITSM, calculate undefined MTTR/SPI/CPI metrics, generate native Excel files, or provide a production-grade migration/backup/hosting plan. Those need the decisions and integrations listed above. Unit test source is in `tests/Atlas.Tests`; run it with the .NET 10 SDK using the README commands.
