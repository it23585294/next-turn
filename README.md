 # next-turn

> This is a smart queue and appointment management system, enabling virtual queue joining, real-time wait-time tracking, notifications, admin dashboards and analytics for hospitals, banks, universities and government services.

## Confluence Handoff: Appointments SRS Update

Confluence cannot be edited from this repository workspace, so apply the following SRS updates manually in the Appointments section:

- Module placement: Appointment module is part of the modular monolith (`NextTurn.Domain/Appointment`, `NextTurn.Application/Appointment`, `NextTurn.Infrastructure/Appointment`).
- API endpoints:
	- `POST /api/appointments` (authenticated) returns `appointmentId` on success.
	- `GET /api/appointments/slots?organisationId=&date=` returns available slots for the requested organisation and date.
- Concurrency behavior:
	- Overlap pre-check in application layer.
	- DB-level unique constraint for active exact-slot collisions (`UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active`).
	- Conflict response: HTTP 409 with detail `This time slot is already booked.`.
- Roles:
	- Consumer users and org-member users (e.g., `User`, `Staff`) can both book appointments.
- Frontend:
	- `AppointmentPage` calendar flow added with slot selection and confirmation ID display.
