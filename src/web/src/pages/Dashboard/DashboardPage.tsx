/**
 * DashboardPage — Sprint 1 protected placeholder.
 *
 * Route: /dashboard/:tenantId  (wrapped by ProtectedRoute)
 *
 * Sprint 1 scope:
 *  - Decode the stored JWT and display the user's name and role
 *  - Provide a working logout button (clearToken + navigate to /)
 *  - Visual placeholder cards for Queue & Appointments (Sprint 2)
 *
 * This page is intentionally minimal. The real dashboard (queue lists,
 * appointment management, staff views) is built in Sprint 2+. The value
 * of this page in Sprint 1 is proving the end-to-end auth flow works:
 *  Register → Login → JWT stored → ProtectedRoute passes → Dashboard shown
 *
 * Sprint 2 additions (NT-XX):
 *  - Replace placeholder cards with real queue / appointment data
 *  - Role-based layout (staff vs user vs admin views)
 *  - Token refresh / 401 interceptor
 */
import { useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { clearToken } from '../../utils/authToken'
import { getTokenPayload } from '../../utils/authToken'
import { getMyQueues, type MyQueueEntry } from '../../api/queues'
import { cancelAppointment, getMyAppointmentBookings, type MyAppointmentBooking } from '../../api/appointments'
import type { ApiError } from '../../types/api'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './DashboardPage.module.css'

/** Human-readable label for the UserRole enum string */
function roleBadgeLabel(role: string): { label: string; className: string } {
  switch (role) {
    case 'Staff':       return { label: 'Staff',          className: styles.roleStaff }
    case 'OrgAdmin':    return { label: 'Org Admin',       className: styles.roleOrgAdmin }
    case 'SystemAdmin': return { label: 'System Admin',    className: styles.roleSystemAdmin }
    default:            return { label: 'User',            className: styles.roleUser }
  }
}

export function DashboardPage() {
  const navigate = useNavigate()
  const payload  = getTokenPayload()

  // ProtectedRoute guarantees a valid token exists before we render.
  // If payload decoding fails for any reason, log out defensively.
  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  const { name, email, role } = payload
  const badge = roleBadgeLabel(role)
  // ── My active queues ──────────────────────────────────────────────────
  const [queues, setQueues] = useState<MyQueueEntry[]>([])
  const [queuesLoading, setQueuesLoading] = useState(true)
  const [queuesError, setQueuesError] = useState<string | null>(null)

  const [appointments, setAppointments] = useState<MyAppointmentBooking[]>([])
  const [appointmentsLoading, setAppointmentsLoading] = useState(true)
  const [appointmentsError, setAppointmentsError] = useState<string | null>(null)
  const [appointmentsSuccess, setAppointmentsSuccess] = useState<string | null>(null)
  const [cancellingAppointmentId, setCancellingAppointmentId] = useState<string | null>(null)

  useEffect(() => {
    getMyQueues()
      .then(data => { setQueues(data); setQueuesLoading(false) })
      .catch(() => { setQueuesError('Could not load your queues.'); setQueuesLoading(false) })

    getMyAppointmentBookings()
      .then(data => { setAppointments(data); setAppointmentsLoading(false) })
      .catch(() => { setAppointmentsError('Could not load your appointment bookings.'); setAppointmentsLoading(false) })
  }, [])

  useEffect(() => {
    if (!appointmentsSuccess) return

    const timer = window.setTimeout(() => {
      setAppointmentsSuccess(null)
    }, 4000)

    return () => window.clearTimeout(timer)
  }, [appointmentsSuccess])

  // ── Join by link ─────────────────────────────────────────────────────
  const [linkInput, setLinkInput] = useState('')
  const [linkError, setLinkError] = useState<string | null>(null)

  const [appointmentLinkInput, setAppointmentLinkInput] = useState('')
  const [appointmentLinkError, setAppointmentLinkError] = useState<string | null>(null)

  function handleJoinByLink() {
    setLinkError(null)
    try {
      // Accept full URLs or just the path segment /queues/:tenantId/:queueId
      const url = new URL(linkInput.includes('://') ? linkInput : `https://x.com${linkInput}`)
      const match = url.pathname.match(/\/queues\/([^/]+)\/([^/]+)/)
      if (!match) throw new Error('invalid')
      const [, linkTenant, linkQueue] = match
      navigate(`/queues/${linkTenant}/${linkQueue}`)
    } catch {
      setLinkError('Invalid queue link. Paste the full URL or the /queues/… path.')
    }
  }

  function handleOpenAppointmentByLink() {
    setAppointmentLinkError(null)
    try {
      // Accept full URLs or just /appointments/:tenantId/:appointmentProfileId path.
      const url = new URL(appointmentLinkInput.includes('://') ? appointmentLinkInput : `https://x.com${appointmentLinkInput}`)
      const match = url.pathname.match(/\/appointments\/([^/]+)\/([^/]+)/)
      if (!match) throw new Error('invalid')

      const [, linkTenant, linkProfile] = match
      navigate(`/appointments/${linkTenant}/${linkProfile}`)
    } catch {
      setAppointmentLinkError('Invalid appointment link. Paste the full URL or the /appointments/tenant/profile path.')
    }
  }

  function handleLogout() {
    clearToken()
    navigate('/', { replace: true })
  }

  async function handleCancelAppointment(appointment: MyAppointmentBooking) {
    const shouldCancel = window.confirm('Cancel this appointment booking?')
    if (!shouldCancel) return

    setAppointmentsError(null)
    setAppointmentsSuccess(null)
    setCancellingAppointmentId(appointment.appointmentId)

    try {
      await cancelAppointment(appointment.appointmentId, appointment.organisationId)
      setAppointments(prev => prev.filter(a => a.appointmentId !== appointment.appointmentId))
      setAppointmentsSuccess('Appointment booking cancelled successfully.')
    } catch (err) {
      const apiErr = err as ApiError
      setAppointmentsError(apiErr.detail ?? 'Could not cancel this appointment booking.')
    } finally {
      setCancellingAppointmentId(null)
    }
  }

  return (
    <div className={styles.page}>
      {/* ── Top nav ─────────────────────────────────────────────── */}
      <header className={styles.navbar}>
        <div className={styles.navInner}>
          <div className={styles.navBrand}>
            <img src={logoImg} alt="NextTurn" className={styles.navLogo} />
          </div>

          <div className={styles.navUser}>
            <div className={styles.avatarCircle} aria-hidden="true">
              {name.charAt(0).toUpperCase()}
            </div>
            <div className={styles.userMeta}>
              <span className={styles.userName}>{name}</span>
              <span className={`${styles.roleBadge} ${badge.className}`}>{badge.label}</span>
            </div>
            <button
              className={styles.logoutBtn}
              onClick={handleLogout}
              type="button"
              aria-label="Sign out"
            >
              <LogoutIcon />
              <span>Sign out</span>
            </button>
          </div>
        </div>
      </header>

      {/* ── Main content ────────────────────────────────────────── */}
      <main className={styles.main}>
        <div className={styles.contentInner}>

          {/* Welcome banner */}
          <div className={styles.welcome}>
            <div>
              <h1 className={styles.welcomeHeading}>Welcome back, {name.split(' ')[0]}!</h1>
              <p className={styles.welcomeSub}>{email}</p>
            </div>
          </div>

          {/* ── My active queues ─────────────────────────────────── */}
          <section className={styles.queueSection} aria-label="My active queues">
            <div className={styles.sectionHeader}>
              <QueueIcon />
              <h2 className={styles.sectionTitle}>My Active Queues</h2>
            </div>

            {queuesLoading && (
              <div className={styles.queuePlaceholder}>
                <span className={styles.queueSpinner} aria-hidden="true" />
                <span>Loading queues…</span>
              </div>
            )}

            {!queuesLoading && queuesError && (
              <p className={styles.queueError}>{queuesError}</p>
            )}

            {!queuesLoading && !queuesError && queues.length === 0 && (
              <p className={styles.queueEmpty}>You haven't joined any queues yet.</p>
            )}

            {!queuesLoading && queues.length > 0 && (
              <ul className={styles.queueList}>
                {queues.map(q => (
                  <li key={q.queueId} className={styles.queueCard}>
                    <div className={styles.queueCardInfo}>
                      <span className={styles.queueCardName}>{q.queueName}</span>
                      <QueueStatusBadge status={q.queueStatus} />
                      <span className={styles.ticketChip}>#{q.ticketNumber}</span>
                    </div>
                    <Link
                      to={`/queues/${q.organisationId}/${q.queueId}`}
                      className={styles.queueJoinLink}
                      aria-label={`View queue ${q.queueName}`}
                    >
                      View &rarr;
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className={styles.queueSection} aria-label="My active appointment bookings">
            <div className={styles.sectionHeader}>
              <CalendarIcon />
              <h2 className={styles.sectionTitle}>My Active Appointment Bookings</h2>
            </div>

            {appointmentsLoading && (
              <div className={styles.queuePlaceholder}>
                <span className={styles.queueSpinner} aria-hidden="true" />
                <span>Loading bookings…</span>
              </div>
            )}

            {!appointmentsLoading && appointmentsError && (
              <p className={styles.queueError}>{appointmentsError}</p>
            )}

            {!appointmentsLoading && !appointmentsError && appointmentsSuccess && (
              <p className={styles.queueSuccess}>{appointmentsSuccess}</p>
            )}

            {!appointmentsLoading && !appointmentsError && appointments.length === 0 && (
              <p className={styles.queueEmpty}>You don't have any active appointment bookings yet.</p>
            )}

            {!appointmentsLoading && appointments.length > 0 && (
              <ul className={styles.queueList}>
                {appointments.map(a => (
                  <li key={a.appointmentId} className={styles.appointmentCard}>
                    <div className={styles.appointmentInfo}>
                      <span className={styles.queueCardName}>{a.appointmentProfileName}</span>
                      <span className={styles.appointmentMeta}>{a.organisationName}</span>
                      <span className={styles.appointmentMeta}>
                        {formatDashboardSlot(a.slotStart, a.slotEnd)}
                      </span>
                    </div>

                    <div className={styles.appointmentActions}>
                      <Link
                        to={`/appointments/${a.organisationId}/${a.appointmentProfileId}`}
                        className={styles.queueJoinLink}
                        aria-label={`View appointment booking ${a.appointmentProfileName}`}
                      >
                        View &rarr;
                      </Link>
                      <button
                        type="button"
                        className={styles.appointmentCancelBtn}
                        onClick={() => handleCancelAppointment(a)}
                        disabled={cancellingAppointmentId === a.appointmentId}
                      >
                        {cancellingAppointmentId === a.appointmentId ? 'Cancelling...' : 'Cancel'}
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>

          {/* ── Join by link ─────────────────────────────────────────── */}
          <section className={styles.joinWidget} aria-label="Join a queue by link">
            <div className={styles.sectionHeader}>
              <LinkIcon />
              <h2 className={styles.sectionTitle}>Join by Link</h2>
            </div>
            <p className={styles.joinWidgetDesc}>Have a queue URL? Paste it below to jump straight in.</p>
            <div className={styles.joinWidgetRow}>
              <input
                className={styles.joinWidgetInput}
                type="text"
                placeholder="https://… or /queues/tenant/queue"
                value={linkInput}
                onChange={e => { setLinkInput(e.target.value); setLinkError(null) }}
                onKeyDown={e => e.key === 'Enter' && handleJoinByLink()}
                aria-label="Queue link"
              />
              <button
                className={styles.joinWidgetBtn}
                onClick={handleJoinByLink}
                type="button"
                disabled={!linkInput.trim()}
              >
                Go
              </button>
            </div>
            {linkError && <p className={styles.joinWidgetError}>{linkError}</p>}
          </section>

          <section className={styles.joinWidget} aria-label="Open appointment booking by link">
            <div className={styles.sectionHeader}>
              <CalendarIcon />
              <h2 className={styles.sectionTitle}>Open Appointment by Link</h2>
            </div>
            <p className={styles.joinWidgetDesc}>Have an appointment booking URL? Paste it here.</p>
            <div className={styles.joinWidgetRow}>
              <input
                className={styles.joinWidgetInput}
                type="text"
                placeholder="https://… or /appointments/tenant/profile"
                value={appointmentLinkInput}
                onChange={e => { setAppointmentLinkInput(e.target.value); setAppointmentLinkError(null) }}
                onKeyDown={e => e.key === 'Enter' && handleOpenAppointmentByLink()}
                aria-label="Appointment link"
              />
              <button
                className={styles.joinWidgetBtn}
                onClick={handleOpenAppointmentByLink}
                type="button"
                disabled={!appointmentLinkInput.trim()}
              >
                Go
              </button>
            </div>
            {appointmentLinkError && <p className={styles.joinWidgetError}>{appointmentLinkError}</p>}
          </section>

          {/* Auth flow confirmation — useful during demo / grading */}
          <div className={styles.authCard} role="note">
            <CheckCircleIcon />
            <div>
              <p className={styles.authCardTitle}>Authentication successful</p>
              <p className={styles.authCardBody}>
                JWT verified · Role: <strong>{role}</strong> · Token stored in localStorage
              </p>
            </div>
          </div>

        </div>
      </main>
    </div>
  )
}

function formatDashboardSlot(slotStart: string, slotEnd: string): string {
  const start = new Date(slotStart)
  const end = new Date(slotEnd)

  const date = start.toLocaleDateString([], { month: 'short', day: 'numeric', year: 'numeric' })
  const from = start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  const to = end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })

  return `${date} · ${from} - ${to}`
}

/* ------------------------------------------------------------------ */
/* Queue status badge                                                   */
/* ------------------------------------------------------------------ */
function QueueStatusBadge({ status }: { status: string }) {
  const cls =
    status === 'Active'  ? styles.queueStatusActive :
    status === 'Paused'  ? styles.queueStatusPaused :
                           styles.queueStatusClosed
  return <span className={`${styles.queueStatusBadge} ${cls}`}>{status}</span>
}

/* ------------------------------------------------------------------ */
/* SVG icons                                                            */
/* ------------------------------------------------------------------ */
function LogoutIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/>
      <polyline points="16 17 21 12 16 7"/>
      <line x1="21" y1="12" x2="9" y2="12"/>
    </svg>
  )
}

function QueueIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="8" y1="6" x2="21" y2="6"/>
      <line x1="8" y1="12" x2="21" y2="12"/>
      <line x1="8" y1="18" x2="21" y2="18"/>
      <line x1="3" y1="6" x2="3.01" y2="6"/>
      <line x1="3" y1="12" x2="3.01" y2="12"/>
      <line x1="3" y1="18" x2="3.01" y2="18"/>
    </svg>
  )
}

function LinkIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M10 13a5 5 0 007.54.54l3-3a5 5 0 00-7.07-7.07l-1.72 1.71"/>
      <path d="M14 11a5 5 0 00-7.54-.54l-3 3a5 5 0 007.07 7.07l1.71-1.71"/>
    </svg>
  )
}

function CalendarIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
      <line x1="16" y1="2" x2="16" y2="6"/>
      <line x1="8" y1="2" x2="8" y2="6"/>
      <line x1="3" y1="10" x2="21" y2="10"/>
    </svg>
  )
}

function CheckCircleIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      style={{ flexShrink: 0, color: 'var(--color-primary)' }}>
      <path d="M22 11.08V12a10 10 0 11-5.93-9.14"/>
      <polyline points="22 4 12 14.01 9 11.01"/>
    </svg>
  )
}
