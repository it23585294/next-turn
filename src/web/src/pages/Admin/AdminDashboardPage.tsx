/**
 * AdminDashboardPage — Org admin queue management.
 *
 * Route: /admin/:tenantId  (wrapped by ProtectedRoute with OrgAdmin/SystemAdmin roles)
 *
 * Features:
 *  - Lists all queues owned by this organisation (loaded on mount)
 *  - Create queue form (name, max capacity, avg service time)
 *  - After create: shows the shareable link with a copy button
 *  - Per-queue: copy shareable link button
 */
import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { createQueue, getOrgQueues, type OrgQueueSummary } from '../../api/queues'
import {
  getAppointmentSchedule,
  configureAppointmentSchedule,
  type AppointmentDayRule,
} from '../../api/appointments'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './AdminDashboardPage.module.css'

// ── Create Queue Form state ────────────────────────────────────────────────────

interface CreateForm {
  name: string
  maxCapacity: string
  averageServiceTimeSeconds: string
}

const defaultForm: CreateForm = {
  name: '',
  maxCapacity: '50',
  averageServiceTimeSeconds: '300',
}

const dayLabels = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

function toInputTime(time: string): string {
  return time.slice(0, 5)
}

// ── Component ─────────────────────────────────────────────────────────────────

export function AdminDashboardPage() {
  const navigate     = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload      = getTokenPayload()

  const [queues,      setQueues]      = useState<OrgQueueSummary[]>([])
  const [loadError,   setLoadError]   = useState<string | null>(null)
  const [form,        setForm]        = useState<CreateForm>(defaultForm)
  const [formErrors,  setFormErrors]  = useState<Partial<CreateForm>>({})
  const [creating,    setCreating]    = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const [newLink,     setNewLink]     = useState<string | null>(null)
  const [copiedId,    setCopiedId]    = useState<string | null>(null)

  const [appointmentRules, setAppointmentRules] = useState<AppointmentDayRule[]>([])
  const [appointmentShareLink, setAppointmentShareLink] = useState<string | null>(null)
  const [scheduleLoading, setScheduleLoading] = useState(true)
  const [scheduleSaving, setScheduleSaving] = useState(false)
  const [scheduleError, setScheduleError] = useState<string | null>(null)
  const [scheduleSuccess, setScheduleSuccess] = useState<string | null>(null)
  const [copiedAppointmentLink, setCopiedAppointmentLink] = useState(false)

  // Redirect if token is invalid (defensive — ProtectedRoute should already catch this)
  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  // ── Load queues on mount ───────────────────────────────────────────────────
  // eslint-disable-next-line react-hooks/rules-of-hooks
  useEffect(() => {
    if (!tenantId) return

    getOrgQueues(tenantId)
      .then(setQueues)
      .catch(() => setLoadError('Could not load queues. Please refresh the page.'))

    getAppointmentSchedule(tenantId)
      .then(config => {
        setAppointmentRules(config.dayRules)
        setAppointmentShareLink(config.shareableLink)
      })
      .catch(() => setScheduleError('Could not load appointment schedule.'))
      .finally(() => setScheduleLoading(false))
  }, [tenantId])

  // ── Handlers ──────────────────────────────────────────────────────────────

  function handleLogout() {
    clearToken()
    navigate('/', { replace: true })
  }

  function validate(): boolean {
    const errors: Partial<CreateForm> = {}
    if (!form.name.trim())                     errors.name = 'Queue name is required.'
    if (!form.maxCapacity || parseInt(form.maxCapacity) < 1)
      errors.maxCapacity = 'Capacity must be at least 1.'
    if (!form.averageServiceTimeSeconds || parseInt(form.averageServiceTimeSeconds) < 1)
      errors.averageServiceTimeSeconds = 'Service time must be at least 1 second.'
    setFormErrors(errors)
    return Object.keys(errors).length === 0
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    if (!tenantId || !validate()) return

    setCreating(true)
    setCreateError(null)
    setNewLink(null)

    try {
      const result = await createQueue(tenantId, {
        name:                      form.name.trim(),
        maxCapacity:               parseInt(form.maxCapacity),
        averageServiceTimeSeconds: parseInt(form.averageServiceTimeSeconds),
      })

      // Add the new queue to the top of the list
      const newQueue: OrgQueueSummary = {
        queueId:                  result.queueId,
        name:                     form.name.trim(),
        maxCapacity:               parseInt(form.maxCapacity),
        averageServiceTimeSeconds: parseInt(form.averageServiceTimeSeconds),
        status:                    'Active',
        shareableLink:             result.shareableLink,
      }
      setQueues(prev => [newQueue, ...prev])
      setNewLink(result.shareableLink)
      setForm(defaultForm)
      setFormErrors({})
    } catch (err) {
      const apiErr = err as ApiError
      if (apiErr.status === 422) {
        const firstError = apiErr.errors ? Object.values(apiErr.errors)[0]?.[0] : undefined
        setCreateError(firstError ?? 'Please check your input and try again.')
      } else {
        setCreateError(apiErr.detail ?? 'Failed to create queue. Please try again.')
      }
    } finally {
      setCreating(false)
    }
  }

  async function copyLink(queue: OrgQueueSummary) {
    const fullUrl = `${window.location.origin}${queue.shareableLink}`
    await navigator.clipboard.writeText(fullUrl)
    setCopiedId(queue.queueId)
    setTimeout(() => setCopiedId(null), 2000)
  }

  async function copyAppointmentLink() {
    if (!appointmentShareLink) return

    await navigator.clipboard.writeText(`${window.location.origin}${appointmentShareLink}`)
    setCopiedAppointmentLink(true)
    setTimeout(() => setCopiedAppointmentLink(false), 2000)
  }

  function updateRule(dayOfWeek: number, changes: Partial<AppointmentDayRule>) {
    setAppointmentRules(prev => prev.map(rule =>
      rule.dayOfWeek === dayOfWeek ? { ...rule, ...changes } : rule))
    setScheduleSuccess(null)
  }

  async function saveSchedule() {
    if (!tenantId || appointmentRules.length !== 7) return

    setScheduleSaving(true)
    setScheduleError(null)
    setScheduleSuccess(null)

    try {
      const result = await configureAppointmentSchedule(tenantId, appointmentRules)
      setAppointmentShareLink(result.shareableLink)
      setScheduleSuccess('Appointment settings saved.')
    } catch (err) {
      const apiErr = err as ApiError
      setScheduleError(apiErr.detail ?? 'Could not save appointment settings.')
    } finally {
      setScheduleSaving(false)
    }
  }

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <div className={styles.page}>
      {/* Top nav */}
      <nav className={styles.topNav}>
        <div className={styles.navLeft}>
          <img src={logoImg} alt="NextTurn" className={styles.logo} />
          <span className={styles.navTitle}>Admin Dashboard</span>
        </div>
        <div className={styles.navRight}>
          <span className={styles.navUser}>{payload.name}</span>
          <button className={styles.logoutBtn} onClick={handleLogout} type="button">
            Log out
          </button>
        </div>
      </nav>

      <main className={styles.main}>

        {/* ── Create Queue ────────────────────────────────────────── */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Create a New Queue</h2>

          {createError && (
            <div className={styles.errorBanner} role="alert" data-testid="create-error">
              {createError}
            </div>
          )}

          {newLink && (
            <div className={styles.successBanner} role="status" data-testid="new-link-banner">
              <span>Queue created! Shareable link:</span>
              <strong className={styles.linkText}>{window.location.origin}{newLink}</strong>
            </div>
          )}

          <form className={styles.createForm} onSubmit={handleCreate} noValidate>
            <div className={styles.formGrid}>
              <div className={styles.formGroup}>
                <label className={styles.label} htmlFor="queue-name">Queue Name</label>
                <input
                  id="queue-name"
                  className={`${styles.input} ${formErrors.name ? styles.inputError : ''}`}
                  type="text"
                  placeholder="e.g. General Counter"
                  value={form.name}
                  onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                />
                {formErrors.name && (
                  <span className={styles.fieldError}>{formErrors.name}</span>
                )}
              </div>

              <div className={styles.formGroup}>
                <label className={styles.label} htmlFor="queue-capacity">Max Capacity</label>
                <input
                  id="queue-capacity"
                  className={`${styles.input} ${formErrors.maxCapacity ? styles.inputError : ''}`}
                  type="number"
                  min={1}
                  value={form.maxCapacity}
                  onChange={e => setForm(f => ({ ...f, maxCapacity: e.target.value }))}
                />
                {formErrors.maxCapacity && (
                  <span className={styles.fieldError}>{formErrors.maxCapacity}</span>
                )}
              </div>

              <div className={styles.formGroup}>
                <label className={styles.label} htmlFor="queue-avg-time">
                  Avg. Service Time (seconds)
                </label>
                <input
                  id="queue-avg-time"
                  className={`${styles.input} ${formErrors.averageServiceTimeSeconds ? styles.inputError : ''}`}
                  type="number"
                  min={1}
                  value={form.averageServiceTimeSeconds}
                  onChange={e => setForm(f => ({ ...f, averageServiceTimeSeconds: e.target.value }))}
                />
                {formErrors.averageServiceTimeSeconds && (
                  <span className={styles.fieldError}>{formErrors.averageServiceTimeSeconds}</span>
                )}
              </div>
            </div>

            <button
              className={styles.createBtn}
              type="submit"
              disabled={creating}
              data-testid="create-queue-btn"
            >
              {creating ? 'Creating…' : 'Create Queue'}
            </button>
          </form>
        </section>

        {/* ── Queue List ──────────────────────────────────────────── */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Your Queues</h2>

          {loadError && (
            <div className={styles.errorBanner} role="alert">{loadError}</div>
          )}

          {queues.length === 0 && !loadError && (
            <p className={styles.emptyNote}>
              No queues yet. Create your first queue above and share the link with users.
            </p>
          )}

          {queues.length > 0 && (
            <ul className={styles.queueList} data-testid="queue-list">
              {queues.map(queue => (
                <li key={queue.queueId} className={styles.queueCard} data-testid="queue-card">
                  <div className={styles.queueCardLeft}>
                    <span className={styles.queueName}>{queue.name}</span>
                    <span className={styles.queueMeta}>
                      Capacity: {queue.maxCapacity} &middot;{' '}
                      Avg. {queue.averageServiceTimeSeconds}s &middot;{' '}
                      <span
                        className={
                          queue.status === 'Active'
                            ? styles.statusActive
                            : styles.statusInactive
                        }
                      >
                        {queue.status}
                      </span>
                    </span>
                    <span className={styles.queueLink}>
                      {window.location.origin}{queue.shareableLink}
                    </span>
                  </div>
                  <button
                    className={styles.copyBtn}
                    type="button"
                    onClick={() => copyLink(queue)}
                    data-testid={`copy-btn-${queue.queueId}`}
                  >
                    {copiedId === queue.queueId ? '✓ Copied!' : 'Copy Link'}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Appointment Booking Settings</h2>

          {scheduleError && (
            <div className={styles.errorBanner} role="alert">{scheduleError}</div>
          )}

          {scheduleSuccess && (
            <div className={styles.successBanner} role="status">{scheduleSuccess}</div>
          )}

          {appointmentShareLink && (
            <div className={styles.successBanner} role="status">
              <span>Shareable appointment link:</span>
              <strong className={styles.linkText}>{window.location.origin}{appointmentShareLink}</strong>
              <button className={styles.copyBtn} type="button" onClick={copyAppointmentLink}>
                {copiedAppointmentLink ? '✓ Copied!' : 'Copy Link'}
              </button>
            </div>
          )}

          {scheduleLoading && <p className={styles.emptyNote}>Loading schedule...</p>}

          {!scheduleLoading && appointmentRules.length > 0 && (
            <div className={styles.queueList}>
              {appointmentRules
                .slice()
                .sort((a, b) => a.dayOfWeek - b.dayOfWeek)
                .map(rule => (
                  <div key={rule.dayOfWeek} className={styles.queueCard}>
                    <div className={styles.queueCardLeft}>
                      <span className={styles.queueName}>{dayLabels[rule.dayOfWeek]}</span>
                    </div>
                    <div className={styles.formGrid}>
                      <label className={styles.label}>
                        <input
                          type="checkbox"
                          checked={rule.isEnabled}
                          onChange={e => updateRule(rule.dayOfWeek, { isEnabled: e.target.checked })}
                        />{' '}
                        Enabled
                      </label>

                      <input
                        className={styles.input}
                        type="time"
                        value={toInputTime(rule.startTime)}
                        disabled={!rule.isEnabled}
                        onChange={e => updateRule(rule.dayOfWeek, { startTime: `${e.target.value}:00` })}
                      />

                      <input
                        className={styles.input}
                        type="time"
                        value={toInputTime(rule.endTime)}
                        disabled={!rule.isEnabled}
                        onChange={e => updateRule(rule.dayOfWeek, { endTime: `${e.target.value}:00` })}
                      />

                      <input
                        className={styles.input}
                        type="number"
                        min={5}
                        max={240}
                        value={rule.slotDurationMinutes}
                        disabled={!rule.isEnabled}
                        onChange={e => updateRule(rule.dayOfWeek, { slotDurationMinutes: parseInt(e.target.value || '30') })}
                      />
                    </div>
                  </div>
                ))}
            </div>
          )}

          <button
            className={styles.createBtn}
            type="button"
            onClick={saveSchedule}
            disabled={scheduleSaving || scheduleLoading || appointmentRules.length !== 7}
          >
            {scheduleSaving ? 'Saving…' : 'Save Appointment Settings'}
          </button>
        </section>

      </main>
    </div>
  )
}
