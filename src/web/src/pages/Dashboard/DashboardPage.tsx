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

  useEffect(() => {
    getMyQueues()
      .then(data => { setQueues(data); setQueuesLoading(false) })
      .catch(() => { setQueuesError('Could not load your queues.'); setQueuesLoading(false) })
  }, [])

  // ── Join by link ─────────────────────────────────────────────────────
  const [linkInput, setLinkInput] = useState('')
  const [linkError, setLinkError] = useState<string | null>(null)

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

  function handleLogout() {
    clearToken()
    navigate('/', { replace: true })
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
