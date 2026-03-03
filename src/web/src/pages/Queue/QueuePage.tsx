/**
 * QueuePage — allows an authenticated user to join a queue.
 *
 * Route: /queues/:tenantId/:queueId  (wrapped by ProtectedRoute)
 *
 * States:
 *  idle       — shows a "Join Queue" button
 *  joining    — button disabled, spinner shown
 *  joined     — success card: ticket number, position, ETA
 *  alreadyIn  — 409 without canBookAppointment: user is already in this queue
 *  full       — 409 with canBookAppointment: queue is at capacity
 *  error      — any other error
 */
import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { joinQueue, getQueueStatus, type QueueStatusResult } from '../../api/queues'
import type { ApiError } from '../../types/api'
import styles from './QueuePage.module.css'

type PageState =
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

export function QueuePage() {
  const { tenantId, queueId } = useParams<{ tenantId: string; queueId: string }>()
  const [state, setState] = useState<PageState>({ status: 'idle' })

  // ── 30-second polling when joined ────────────────────────────────────────
  useEffect(() => {
    if (state.status !== 'joined' || !tenantId || !queueId) return

    const id = setInterval(async () => {
      try {
        const fresh = await getQueueStatus(queueId, tenantId)
        setState(prev =>
          prev.status === 'joined'
            ? { ...prev, data: fresh }
            : prev
        )
      } catch {
        // Silently ignore poll errors — stale data is better than crashing.
        // If the entry expires on the server, the next successful poll will return 400
        // and the user will see no change until they manually refresh.
      }
    }, 30_000)

    return () => clearInterval(id)
  }, [state.status, tenantId, queueId])

  async function handleJoin() {
    if (!tenantId || !queueId) return
    setState({ status: 'joining' })

    try {
      const result = await joinQueue(queueId, tenantId)
      // Seed the joined state with join result + default Active status.
      // Subsequent polls via getQueueStatus will update all fields including queueStatus.
      setState({
        status: 'joined',
        data: { ...result, queueStatus: 'Active' },
      })
    } catch (err) {
      const apiErr = err as ApiError
      if (apiErr.status === 409) {
        // Distinguish "already in queue" (no canBookAppointment) from "queue full"
        const raw = apiErr.raw as Record<string, unknown>
        if (raw.canBookAppointment === true) {
          setState({ status: 'full' })
        } else {
          setState({ status: 'alreadyIn' })
        }
      } else {
        setState({ status: 'error', detail: apiErr.detail ?? 'Something went wrong.' })
      }
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.heading}>Join Queue</h1>

        {/* ── Idle ── */}
        {state.status === 'idle' && (
          <>
            <p className={styles.body}>Press the button below to take your place in the queue.</p>
            <button className={styles.joinBtn} onClick={handleJoin} type="button">
              Join Queue
            </button>
          </>
        )}

        {/* ── Joining (loading) ── */}
        {state.status === 'joining' && (
          <div className={styles.loadingRow}>
            <span className={styles.spinner} aria-hidden="true" />
            <span>Joining queue…</span>
          </div>
        )}

        {/* ── Joined (success) ── */}
        {state.status === 'joined' && (
          <div className={styles.successBlock} data-testid="success-block">
            {/* Queue status banners */}
            {state.data.queueStatus === 'Paused' && (
              <div className={styles.pausedBanner} role="alert" data-testid="paused-banner">
                ⏸ This queue is currently paused. Your position is held.
              </div>
            )}
            {state.data.queueStatus === 'Closed' && (
              <div className={styles.closedBanner} role="alert" data-testid="closed-banner">
                This queue has closed. Please contact the organisation.
              </div>
            )}

            {/* You're next highlight */}
            {state.data.positionInQueue === 1 && state.data.queueStatus === 'Active' && (
              <div className={styles.youreNext} role="status" data-testid="youre-next-banner">
                🎉 You&apos;re next! Please proceed to the counter.
              </div>
            )}

            <div className={styles.ticketBadge}>
              <span className={styles.ticketLabel}>Your ticket</span>
              <span className={styles.ticketNumber}>#{state.data.ticketNumber}</span>
            </div>
            <dl className={styles.statsList}>
              <div className={styles.stat}>
                <dt>Position in queue</dt>
                <dd>{state.data.positionInQueue}</dd>
              </div>
              <div className={styles.stat}>
                <dt>Estimated wait</dt>
                <dd>{formatEta(state.data.estimatedWaitSeconds)}</dd>
              </div>
            </dl>
            <p className={styles.successNote}>
              Please stay nearby. You&apos;ll be called when it&apos;s your turn.
            </p>
            <p className={styles.pollingNote} aria-live="polite">
              Position updates every 30 seconds.
            </p>
          </div>
        )}

        {/* ── Already in queue ── */}
        {state.status === 'alreadyIn' && (
          <div className={styles.infoBlock} data-testid="already-in-block">
            <p className={styles.infoText}>You already have an active ticket for this queue.</p>
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
          <div className={styles.fullBlock} data-testid="queue-full-block">
            <p className={styles.fullText}>
              This queue is currently full.
            </p>
            <p className={styles.body}>
              You can book an appointment instead to guarantee a time slot.
            </p>
            <button className={styles.joinBtn} type="button" disabled>
              Queue is Full
            </button>
            <button
              className={`${styles.joinBtn} ${styles.joinBtnAppointment}`}
              type="button"
              data-testid="book-appointment-btn"
            >
              Book an Appointment
            </button>
          </div>
        )}

        {/* ── Generic error ── */}
        {state.status === 'error' && (
          <div className={styles.errorBlock} data-testid="error-block">
            <p className={styles.errorText}>{state.detail}</p>
            <button
              className={`${styles.joinBtn} ${styles.joinBtnSecondary}`}
              onClick={() => setState({ status: 'idle' })}
              type="button"
            >
              Try Again
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
