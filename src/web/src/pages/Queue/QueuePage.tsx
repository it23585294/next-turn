/**
 * QueuePage — allows an authenticated user to join a queue and track their position.
 *
 * Route: /queues/:tenantId/:queueId  (wrapped by ProtectedRoute)
 *
 * States:
 *  loading    — initial mount: checking if user already has an active ticket
 *  idle       — user is not in this queue; shows "Join Queue" CTA
 *  joining    — join request in-flight
 *  joined     — user has a ticket; shows boarding-pass style ticket card
 *  alreadyIn  — edge case: returned 409 without canBookAppointment (shouldn't normally be shown)
 *  full       — 409 with canBookAppointment: queue is at capacity
 *  error      — any other error
 *
 * On mount: calls getQueueStatus. If user already has an active entry the page
 * pre-loads the joined state from that initial check. This means the state is
 * preserved on page refresh.
 *
 * Polling: while in joined state, getQueueStatus is called every 30 seconds.
 */
import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { joinQueue, getQueueStatus, type QueueStatusResult } from '../../api/queues'
import { getTokenPayload } from '../../utils/authToken'
import type { ApiError } from '../../types/api'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './QueuePage.module.css'

type PageState =
  | { status: 'loading' }
  | { status: 'idle' }
  | { status: 'joining' }
  | { status: 'joined'; data: QueueStatusResult }
  | { status: 'alreadyIn' }
  | { status: 'full' }
  | { status: 'error'; detail: string }

function formatEta(seconds: number): string {
  if (seconds < 60) return `${seconds}s`
  const mins = Math.round(seconds / 60)
  return mins === 1 ? '1 min' : `${mins} mins`
}

function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={`${styles.statusBadge} ${
        status === 'Active'
          ? styles.statusActive
          : status === 'Paused'
          ? styles.statusPaused
          : styles.statusClosed
      }`}
    >
      <span className={styles.statusDot} aria-hidden="true" />
      {status}
    </span>
  )
}

export function QueuePage() {
  const { tenantId, queueId } = useParams<{ tenantId: string; queueId: string }>()
  const navigate = useNavigate()
  const [state, setState] = useState<PageState>({ status: 'loading' })
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const payload = getTokenPayload()

  // ── On mount: check if user already has an active ticket ─────────────
  useEffect(() => {
    if (!tenantId || !queueId) {
      setState({ status: 'idle' })
      return
    }

    getQueueStatus(queueId, tenantId)
      .then(result => {
        setState({ status: 'joined', data: result })
        setLastUpdated(new Date())
      })
      .catch((err: ApiError) => {
        // 400 means no active entry — drop to idle, not an error
        if (err.status === 400) {
          setState({ status: 'idle' })
        } else {
          setState({ status: 'idle' }) // still go idle; don't block joining
        }
      })
  }, [tenantId, queueId])

  // ── 30-second polling when joined ────────────────────────────────────
  useEffect(() => {
    if (state.status !== 'joined' || !tenantId || !queueId) return

    const id = setInterval(async () => {
      try {
        const fresh = await getQueueStatus(queueId, tenantId)
        setState(prev =>
          prev.status === 'joined' ? { ...prev, data: fresh } : prev
        )
        setLastUpdated(new Date())
      } catch {
        // Silently ignore poll errors — stale data beats a crash
      }
    }, 30_000)

    return () => clearInterval(id)
  }, [state.status, tenantId, queueId])

  async function handleJoin() {
    if (!tenantId || !queueId) return
    setState({ status: 'joining' })

    try {
      const result = await joinQueue(queueId, tenantId)
      setState({
        status: 'joined',
        data: { ...result, queueStatus: 'Active' },
      })
      setLastUpdated(new Date())
    } catch (err) {
      const apiErr = err as ApiError
      if (apiErr.status === 409) {
        const raw = apiErr.raw as Record<string, unknown>
        setState(raw.canBookAppointment === true ? { status: 'full' } : { status: 'alreadyIn' })
      } else {
        setState({ status: 'error', detail: apiErr.detail ?? 'Something went wrong.' })
      }
    }
  }

  function handleBackToDashboard() {
    const payload_ = getTokenPayload()
    if (!payload_ || !tenantId) { navigate('/'); return }
    const isAdmin = payload_.role === 'OrgAdmin' || payload_.role === 'SystemAdmin'
    navigate(isAdmin ? `/admin/${tenantId}` : `/dashboard/${tenantId}`)
  }

  // ── Loading ─────────────────────────────────────────────────────────
  if (state.status === 'loading') {
    return (
      <div className={styles.page}>
        <TopBar onBack={handleBackToDashboard} />
        <main className={styles.main}>
          <div className={styles.loadingCard}>
            <span className={styles.spinner} aria-hidden="true" />
            <p>Checking your queue status…</p>
          </div>
        </main>
      </div>
    )
  }

  return (
    <div className={styles.page}>
      <TopBar onBack={handleBackToDashboard} />

      <main className={styles.main}>

        {/* ── Idle — no ticket yet ── */}
        {state.status === 'idle' && (
          <div className={styles.idleCard} data-testid="idle-card">
            <div className={styles.idleIcon} aria-hidden="true">
              <TicketIcon />
            </div>
            <h1 className={styles.idleHeading}>Ready to join?</h1>
            <p className={styles.idleBody}>
              Tap below to take your place in line. We'll track your position in real time.
            </p>
            <button className={styles.joinBtn} onClick={handleJoin} type="button">
              Join Queue
            </button>
            <p className={styles.idleNote}>
              You'll receive a digital ticket with your position and estimated wait time.
            </p>
          </div>
        )}

        {/* ── Joining ── */}
        {state.status === 'joining' && (
          <div className={styles.idleCard}>
            <span className={styles.spinner} aria-hidden="true" />
            <p className={styles.idleBody}>Securing your spot…</p>
          </div>
        )}

        {/* ── Joined — boarding-pass ticket ── */}
        {state.status === 'joined' && (
          <div className={styles.ticket} data-testid="success-block">

            {/* Banner alerts */}
            {state.data.queueStatus === 'Paused' && (
              <div className={styles.alertBanner} role="alert" data-testid="paused-banner">
                Queue is paused — your position is held.
              </div>
            )}
            {state.data.queueStatus === 'Closed' && (
              <div className={`${styles.alertBanner} ${styles.alertClosed}`} role="alert" data-testid="closed-banner">
                Queue has closed. Please contact the organisation.
              </div>
            )}
            {state.data.positionInQueue === 1 && state.data.queueStatus === 'Active' && (
              <div className={`${styles.alertBanner} ${styles.alertNext}`} role="status" data-testid="youre-next-banner">
                You're next — please proceed to the counter.
              </div>
            )}

            {/* Ticket header */}
            <div className={styles.ticketHeader}>
              <div className={styles.ticketHeaderTop}>
                <span className={styles.ticketLabel}>YOUR TICKET</span>
                <StatusBadge status={state.data.queueStatus} />
              </div>
              <div className={styles.ticketNumber} aria-label={`Ticket number ${state.data.ticketNumber}`}>
                #{String(state.data.ticketNumber).padStart(3, '0')}
              </div>
              {payload && (
                <p className={styles.ticketName}>{payload.name}</p>
              )}
            </div>

            {/* Perforated divider */}
            <div className={styles.perforation} aria-hidden="true">
              <div className={styles.perforationLeft} />
              <div className={styles.perforationLine} />
              <div className={styles.perforationRight} />
            </div>

            {/* Ticket body */}
            <div className={styles.ticketBody}>
              <div className={styles.ticketStats}>
                <div className={styles.ticketStat}>
                  <span className={styles.ticketStatValue}>{state.data.positionInQueue}</span>
                  <span className={styles.ticketStatLabel}>Position</span>
                </div>
                <div className={styles.ticketStatDivider} aria-hidden="true" />
                <div className={styles.ticketStat}>
                  <span className={styles.ticketStatValue}>{formatEta(state.data.estimatedWaitSeconds)}</span>
                  <span className={styles.ticketStatLabel}>Est. Wait</span>
                </div>
              </div>

              {/* Position progress bar */}
              <div className={styles.progressWrap} aria-label={`Position ${state.data.positionInQueue} in queue`}>
                <div
                  className={styles.progressBar}
                  style={{
                    width: state.data.positionInQueue <= 1
                      ? '100%'
                      : `${Math.max(10, 100 - (state.data.positionInQueue - 1) * 10)}%`,
                  }}
                />
              </div>
              <p className={styles.progressLabel}>
                {state.data.positionInQueue === 1
                  ? 'You are next!'
                  : `${state.data.positionInQueue - 1} ${state.data.positionInQueue - 1 === 1 ? 'person' : 'people'} ahead of you`}
              </p>
            </div>

            {/* Ticket footer */}
            <div className={styles.ticketFooter}>
              {lastUpdated && (
                <p className={styles.lastUpdated} aria-live="polite">
                  Updated {lastUpdated.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  {' · '}refreshes every 30s
                </p>
              )}
              <p className={styles.ticketNote}>
                Please stay nearby. You'll be called when it's your turn.
              </p>
            </div>
          </div>
        )}

        {/* ── Already in queue ── */}
        {state.status === 'alreadyIn' && (
          <div className={styles.idleCard} data-testid="already-in-block">
            <div className={styles.idleIcon}>
              <InfoIcon />
            </div>
            <h1 className={styles.idleHeading}>Already in queue</h1>
            <p className={styles.idleBody}>You already have an active ticket for this queue.</p>
            <button
              className={`${styles.joinBtn} ${styles.joinBtnSecondary}`}
              onClick={() => setState({ status: 'idle' })}
              type="button"
            >
              Back
            </button>
          </div>
        )}

        {/* ── Queue full ── */}
        {state.status === 'full' && (
          <div className={styles.idleCard} data-testid="queue-full-block">
            <div className={`${styles.idleIcon} ${styles.idleIconWarning}`}>
              <FullIcon />
            </div>
            <h1 className={styles.idleHeading}>Queue is full</h1>
            <p className={styles.idleBody}>
              This queue has reached capacity. Try again later or book an appointment for a guaranteed time slot.
            </p>
            <button className={styles.joinBtn} type="button" disabled>
              Queue is Full
            </button>
            <button
              className={`${styles.joinBtn} ${styles.joinBtnAccent}`}
              type="button"
              data-testid="book-appointment-btn"
            >
              Book an Appointment
            </button>
          </div>
        )}

        {/* ── Generic error ── */}
        {state.status === 'error' && (
          <div className={styles.idleCard} data-testid="error-block">
            <div className={`${styles.idleIcon} ${styles.idleIconError}`}>
              <ErrorIcon />
            </div>
            <h1 className={styles.idleHeading}>Something went wrong</h1>
            <p className={styles.idleBody}>{state.detail}</p>
            <button
              className={`${styles.joinBtn} ${styles.joinBtnSecondary}`}
              onClick={() => setState({ status: 'idle' })}
              type="button"
            >
              Try Again
            </button>
          </div>
        )}

      </main>
    </div>
  )
}

/* ── Top bar ─────────────────────────────────────────────────────── */
function TopBar({ onBack }: { onBack: () => void }) {
  return (
    <header className={styles.topBar}>
      <button onClick={onBack} className={styles.backBtn} type="button" aria-label="Back to dashboard">
        <BackIcon />
        <span>Dashboard</span>
      </button>
      <img src={logoImg} alt="NextTurn" className={styles.topBarLogo} />
    </header>
  )
}

/* ── Icons ───────────────────────────────────────────────────────── */
function TicketIcon() {
  return (
    <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2 9a3 3 0 010-6h20a3 3 0 010 6"/>
      <path d="M2 15a3 3 0 000 6h20a3 3 0 000-6"/>
      <line x1="2" y1="12" x2="22" y2="12"/>
    </svg>
  )
}
function InfoIcon() {
  return (
    <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10"/>
      <line x1="12" y1="8" x2="12" y2="12"/>
      <line x1="12" y1="16" x2="12.01" y2="16"/>
    </svg>
  )
}
function FullIcon() {
  return (
    <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10"/>
      <line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/>
    </svg>
  )
}
function ErrorIcon() {
  return (
    <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10"/>
      <line x1="12" y1="8" x2="12" y2="12"/>
      <line x1="12" y1="16" x2="12.01" y2="16"/>
    </svg>
  )
}
function BackIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="15 18 9 12 15 6"/>
    </svg>
  )
}
