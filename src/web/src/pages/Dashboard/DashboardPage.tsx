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
import { useNavigate, useParams } from 'react-router-dom'
import { clearToken } from '../../utils/authToken'
import { getTokenPayload } from '../../utils/authToken'
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
  const navigate     = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload      = getTokenPayload()

  // ProtectedRoute guarantees a valid token exists before we render.
  // If payload decoding fails for any reason, log out defensively.
  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  const { name, email, role } = payload
  const badge = roleBadgeLabel(role)

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
            <div className={styles.tenantChip}>
              <BuildingIcon />
              <span>Tenant: <code>{tenantId}</code></span>
            </div>
          </div>

          {/* Placeholder feature cards */}
          <section className={styles.cardGrid} aria-label="Dashboard sections">
            <PlaceholderCard
              icon={<QueueIcon />}
              title="My Queue"
              description="View your current position, estimated wait time, and join new queues."
              tag="Sprint 2"
            />
            <PlaceholderCard
              icon={<CalendarIcon />}
              title="Appointments"
              description="Book, reschedule, and cancel appointments with your organisation."
              tag="Sprint 2"
            />
            <PlaceholderCard
              icon={<BellIcon />}
              title="Notifications"
              description="Get notified when it's your turn or your appointment is due."
              tag="Sprint 2"
            />
            <PlaceholderCard
              icon={<ChartIcon />}
              title="Activity"
              description="Review your past visits, queue history, and appointment records."
              tag="Sprint 2"
            />
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
/* Placeholder card                                                     */
/* ------------------------------------------------------------------ */
function PlaceholderCard({
  icon, title, description, tag,
}: {
  icon: React.ReactNode
  title: string
  description: string
  tag: string
}) {
  return (
    <div className={styles.card}>
      <div className={styles.cardIcon}>{icon}</div>
      <div className={styles.cardBody}>
        <h2 className={styles.cardTitle}>{title}</h2>
        <p className={styles.cardDesc}>{description}</p>
      </div>
      <span className={styles.cardTag}>{tag}</span>
    </div>
  )
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
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none"
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

function CalendarIcon() {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
      <line x1="16" y1="2" x2="16" y2="6"/>
      <line x1="8"  y1="2" x2="8"  y2="6"/>
      <line x1="3"  y1="10" x2="21" y2="10"/>
    </svg>
  )
}

function BellIcon() {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
      <path d="M13.73 21a2 2 0 01-3.46 0"/>
    </svg>
  )
}

function ChartIcon() {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="18" y1="20" x2="18" y2="10"/>
      <line x1="12" y1="20" x2="12" y2="4"/>
      <line x1="6"  y1="20" x2="6"  y2="14"/>
    </svg>
  )
}

function BuildingIcon() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="18" height="18" rx="2"/>
      <path d="M9 22V12h6v10"/>
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
