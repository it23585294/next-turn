/**
 * AccessDeniedPage — shown when an authenticated user tries to access a route
 * that requires a higher role than they hold (NT-12-6).
 *
 * Displays:
 *  - The user's current role so they know who they're signed in as
 *  - A "Go to Dashboard" button (safe landing zone)
 *  - A "Sign Out" button (clears token, returns to welcome page)
 */
import { useNavigate, useParams } from 'react-router-dom'
import { clearToken } from '../../utils/authToken'
import { getTokenPayload } from '../../utils/authToken'
import styles from './AccessDeniedPage.module.css'

export function AccessDeniedPage() {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const role = payload?.role ?? 'Unknown'

  function handleDashboard() {
    // Navigate back to the dashboard for the current tenant if known,
    // otherwise fall back to the welcome page.
    const dest = tenantId
      ? `/dashboard/${tenantId}`
      : payload?.tid
        ? `/dashboard/${payload.tid}`
        : '/'
    navigate(dest, { replace: true })
  }

  function handleSignOut() {
    clearToken()
    navigate('/', { replace: true })
  }

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        {/* Icon */}
        <div className={styles.iconWrap} aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" />
            <line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
          </svg>
        </div>

        <h1 className={styles.heading}>Access Denied</h1>

        <p className={styles.message}>
          You don't have permission to view that page.
        </p>

        <div className={styles.roleChip}>
          Signed in as <strong>{role}</strong>
        </div>

        <div className={styles.actions}>
          <button
            type="button"
            className={styles.primaryBtn}
            onClick={handleDashboard}
          >
            Go to Dashboard
          </button>
          <button
            type="button"
            className={styles.secondaryBtn}
            onClick={handleSignOut}
          >
            Sign Out
          </button>
        </div>
      </div>
    </div>
  )
}
