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
import { useMemo, useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { createQueue, getOrgQueues, type OrgQueueSummary } from '../../api/queues'
import {
  getAppointmentSchedule,
  configureAppointmentSchedule,
  listAppointmentProfiles,
  createAppointmentProfile,
  type AppointmentProfileSummary,
  type AppointmentDayRule,
} from '../../api/appointments'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './AdminDashboardPage.module.css'

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

function toMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number)
  if (!Number.isFinite(h) || !Number.isFinite(m)) return 0
  return (h * 60) + m
}

function slotsForRule(rule: AppointmentDayRule): number {
  if (!rule.isEnabled) return 0
  const windowMinutes = toMinutes(rule.endTime) - toMinutes(rule.startTime)
  if (windowMinutes <= 0 || rule.slotDurationMinutes <= 0) return 0
  return Math.floor(windowMinutes / rule.slotDurationMinutes)
}

export function AdminDashboardPage() {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const [queues, setQueues] = useState<OrgQueueSummary[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateForm>(defaultForm)
  const [formErrors, setFormErrors] = useState<Partial<CreateForm>>({})
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const [newLink, setNewLink] = useState<string | null>(null)
  const [copiedId, setCopiedId] = useState<string | null>(null)

  const [appointmentRules, setAppointmentRules] = useState<AppointmentDayRule[]>([])
  const [appointmentProfiles, setAppointmentProfiles] = useState<AppointmentProfileSummary[]>([])
  const [selectedAppointmentProfileId, setSelectedAppointmentProfileId] = useState<string>('')
  const [newAppointmentProfileName, setNewAppointmentProfileName] = useState('')
  const [profileError, setProfileError] = useState<string | null>(null)
  const [profileLoading, setProfileLoading] = useState(true)
  const [profileCreating, setProfileCreating] = useState(false)
  const [appointmentShareLink, setAppointmentShareLink] = useState<string | null>(null)
  const [scheduleLoading, setScheduleLoading] = useState(true)
  const [scheduleSaving, setScheduleSaving] = useState(false)
  const [scheduleError, setScheduleError] = useState<string | null>(null)
  const [scheduleSuccess, setScheduleSuccess] = useState<string | null>(null)
  const [copiedAppointmentLink, setCopiedAppointmentLink] = useState(false)
  const [activeTab, setActiveTab] = useState<'queues' | 'appointments'>('queues')

  const appointmentSummary = useMemo(() => {
    const enabledDays = appointmentRules.filter(r => r.isEnabled).length
    const totalWeeklySlots = appointmentRules.reduce((sum, rule) => sum + slotsForRule(rule), 0)
    return { enabledDays, totalWeeklySlots }
  }, [appointmentRules])

  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  // eslint-disable-next-line react-hooks/rules-of-hooks
  useEffect(() => {
    if (!tenantId) return

    getOrgQueues(tenantId)
      .then(setQueues)
      .catch(() => setLoadError('Could not load queues. Please refresh the page.'))

    listAppointmentProfiles(tenantId)
      .then(profiles => {
        setAppointmentProfiles(profiles)
        setSelectedAppointmentProfileId(prev => prev || profiles[0]?.appointmentProfileId || '')
      })
      .catch((err: ApiError) => {
        if (err.status === 403) {
          setProfileError('You do not have permission to load appointment profiles.')
          return
        }

        if (err.status === 401) {
          setProfileError('Your session is not authorized. Please sign in again.')
          return
        }

        setProfileError(err.detail ?? 'Could not load appointment profiles.')
      })
      .finally(() => setProfileLoading(false))
  }, [tenantId])

  // eslint-disable-next-line react-hooks/rules-of-hooks
  useEffect(() => {
    if (!tenantId || !selectedAppointmentProfileId) {
      setAppointmentRules([])
      setAppointmentShareLink(null)
      setScheduleLoading(false)
      return
    }

    setScheduleLoading(true)
    setScheduleError(null)

    getAppointmentSchedule(tenantId, selectedAppointmentProfileId)
      .then(config => {
        setAppointmentRules(config.dayRules)
        setAppointmentShareLink(config.shareableLink)
      })
      .catch(() => setScheduleError('Could not load appointment schedule.'))
      .finally(() => setScheduleLoading(false))
  }, [tenantId, selectedAppointmentProfileId])

  function handleLogout() {
    clearToken()
    navigate('/', { replace: true })
  }

  function validate(): boolean {
    const errors: Partial<CreateForm> = {}
    if (!form.name.trim()) errors.name = 'Queue name is required.'
    if (!form.maxCapacity || parseInt(form.maxCapacity, 10) < 1) {
      errors.maxCapacity = 'Capacity must be at least 1.'
    }
    if (!form.averageServiceTimeSeconds || parseInt(form.averageServiceTimeSeconds, 10) < 1) {
      errors.averageServiceTimeSeconds = 'Service time must be at least 1 second.'
    }
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
        name: form.name.trim(),
        maxCapacity: parseInt(form.maxCapacity, 10),
        averageServiceTimeSeconds: parseInt(form.averageServiceTimeSeconds, 10),
      })

      const newQueue: OrgQueueSummary = {
        queueId: result.queueId,
        name: form.name.trim(),
        maxCapacity: parseInt(form.maxCapacity, 10),
        averageServiceTimeSeconds: parseInt(form.averageServiceTimeSeconds, 10),
        status: 'Active',
        shareableLink: result.shareableLink,
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
    setAppointmentRules(prev =>
      prev.map(rule => (rule.dayOfWeek === dayOfWeek ? { ...rule, ...changes } : rule)),
    )
    setScheduleSuccess(null)
    setScheduleError(null)
  }

  async function saveSchedule() {
    if (!tenantId || !selectedAppointmentProfileId || appointmentRules.length !== 7) return

    setScheduleSaving(true)
    setScheduleError(null)
    setScheduleSuccess(null)

    try {
      const result = await configureAppointmentSchedule(
        tenantId,
        selectedAppointmentProfileId,
        appointmentRules,
      )
      setAppointmentShareLink(result.shareableLink)
      setScheduleSuccess('Appointment settings saved.')
    } catch (err) {
      const apiErr = err as ApiError
      setScheduleError(apiErr.detail ?? 'Could not save appointment settings.')
    } finally {
      setScheduleSaving(false)
    }
  }

  async function handleCreateAppointmentProfile(e: React.FormEvent) {
    e.preventDefault()
    if (!tenantId || !newAppointmentProfileName.trim()) return

    setProfileCreating(true)
    setProfileError(null)

    try {
      const created = await createAppointmentProfile(tenantId, newAppointmentProfileName.trim())
      setAppointmentProfiles(prev => [created, ...prev])
      setSelectedAppointmentProfileId(created.appointmentProfileId)
      setNewAppointmentProfileName('')
      setScheduleSuccess('Appointment profile created.')
    } catch (err) {
      const apiErr = err as ApiError
      setProfileError(apiErr.detail ?? 'Could not create appointment profile.')
    } finally {
      setProfileCreating(false)
    }
  }

  return (
    <div className={styles.page}>
      <nav className={styles.topNav}>
        <img src={logoImg} alt="NextTurn" className={styles.logo} />
        <button className={styles.logoutBtn} onClick={handleLogout} type="button">
          Logout
        </button>
      </nav>

      <main className={styles.main}>
        <section className={styles.toolbar}>
          <div className={styles.toolbarHeader}>
            <h1 className={styles.pageTitle}>Operations Control Center</h1>
            <p className={styles.pageSubtitle}>Manage queues and appointment capacity from one place.</p>
          </div>
          <div className={styles.tabs} role="tablist" aria-label="Admin sections">
            <button
              type="button"
              className={`${styles.tabBtn} ${activeTab === 'queues' ? styles.tabBtnActive : ''}`}
              onClick={() => setActiveTab('queues')}
              role="tab"
              aria-selected={activeTab === 'queues'}
            >
              Queue Management
            </button>
            <button
              type="button"
              className={`${styles.tabBtn} ${activeTab === 'appointments' ? styles.tabBtnActive : ''}`}
              onClick={() => setActiveTab('appointments')}
              role="tab"
              aria-selected={activeTab === 'appointments'}
            >
              Appointment Settings
            </button>
          </div>
        </section>

        {activeTab === 'queues' && (
          <>
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Create a New Queue</h2>
              <p className={styles.sectionHint}>Create and share queue links for customers to join in seconds.</p>

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

            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Your Queues</h2>
              <p className={styles.sectionHint}>Copy and share queue links with customers.</p>

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
                          Capacity: {queue.maxCapacity} &middot; Avg. {queue.averageServiceTimeSeconds}s &middot;{' '}
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
          </>
        )}

        {activeTab === 'appointments' && (
          <>
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Create Appointment Profile</h2>
              <p className={styles.sectionHint}>
                Create a dedicated appointment link for each service stream.
              </p>

              {profileError && (
                <div className={styles.errorBanner} role="alert">{profileError}</div>
              )}

              <form className={styles.createForm} onSubmit={handleCreateAppointmentProfile} noValidate>
                <div className={styles.formGrid}>
                  <div className={styles.formGroup}>
                    <label className={styles.label} htmlFor="appointment-profile-name">Appointment Profile Name</label>
                    <input
                      id="appointment-profile-name"
                      className={styles.input}
                      type="text"
                      placeholder="e.g. Haircut Bookings"
                      value={newAppointmentProfileName}
                      onChange={e => setNewAppointmentProfileName(e.target.value)}
                    />
                  </div>
                </div>
                <button className={styles.createBtn} type="submit" disabled={profileCreating || !newAppointmentProfileName.trim()}>
                  {profileCreating ? 'Creating…' : 'Create Appointment Profile'}
                </button>
              </form>
            </section>

            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>View and Configure Appointment Profiles</h2>
              <p className={styles.sectionHint}>
                Select a profile, copy its shareable link, and configure operating hours and slot duration.
              </p>

              {profileLoading && <p className={styles.emptyNote}>Loading appointment profiles...</p>}

              {!profileLoading && appointmentProfiles.length === 0 && (
                <p className={styles.emptyNote}>No appointment profiles yet. Create one above to start configuring.</p>
              )}

              {appointmentProfiles.length > 0 && (
                <div className={`${styles.formGroup} ${styles.profileSelectGroup}`}>
                  <label className={styles.label} htmlFor="appointment-profile-select">Active appointment profile</label>
                  <select
                    id="appointment-profile-select"
                    className={styles.input}
                    value={selectedAppointmentProfileId}
                    onChange={e => {
                      setSelectedAppointmentProfileId(e.target.value)
                      setScheduleSuccess(null)
                      setScheduleError(null)
                    }}
                  >
                    {appointmentProfiles.map(profile => (
                      <option key={profile.appointmentProfileId} value={profile.appointmentProfileId}>
                        {profile.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div className={styles.statsRow}>
                <article className={styles.statCard}>
                  <span className={styles.statLabel}>Enabled days</span>
                  <strong className={styles.statValue}>{appointmentSummary.enabledDays}/7</strong>
                </article>
                <article className={styles.statCard}>
                  <span className={styles.statLabel}>Total weekly capacity</span>
                  <strong className={styles.statValue}>{appointmentSummary.totalWeeklySlots}</strong>
                  <span className={styles.statHint}>appointments/week</span>
                </article>
              </div>

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
                <div className={styles.scheduleList}>
                  {appointmentRules
                    .slice()
                    .sort((a, b) => a.dayOfWeek - b.dayOfWeek)
                    .map(rule => {
                      const daySlots = slotsForRule(rule)
                      return (
                        <article
                          key={rule.dayOfWeek}
                          className={`${styles.scheduleCard} ${rule.isEnabled ? styles.scheduleCardEnabled : styles.scheduleCardDisabled}`}
                        >
                          <header className={styles.scheduleCardHeader}>
                            <div>
                              <h3 className={styles.scheduleDay}>{dayLabels[rule.dayOfWeek]}</h3>
                              <p className={styles.scheduleSummary}>
                                {rule.isEnabled
                                  ? `${toInputTime(rule.startTime)} - ${toInputTime(rule.endTime)} · every ${rule.slotDurationMinutes} min`
                                  : 'Closed'}
                              </p>
                            </div>
                            <span className={styles.capacityPill}>{daySlots} slots</span>
                          </header>

                          <div className={styles.scheduleGrid}>
                            <label className={styles.checkboxLabel}>
                              <input
                                type="checkbox"
                                checked={rule.isEnabled}
                                onChange={e => updateRule(rule.dayOfWeek, { isEnabled: e.target.checked })}
                              />
                              <span>Open for appointments</span>
                            </label>

                            <div className={styles.formGroup}>
                              <label className={styles.label}>Start time</label>
                              <input
                                className={styles.input}
                                type="time"
                                value={toInputTime(rule.startTime)}
                                disabled={!rule.isEnabled}
                                onChange={e => updateRule(rule.dayOfWeek, { startTime: `${e.target.value}:00` })}
                              />
                            </div>

                            <div className={styles.formGroup}>
                              <label className={styles.label}>End time</label>
                              <input
                                className={styles.input}
                                type="time"
                                value={toInputTime(rule.endTime)}
                                disabled={!rule.isEnabled}
                                onChange={e => updateRule(rule.dayOfWeek, { endTime: `${e.target.value}:00` })}
                              />
                            </div>

                            <div className={styles.formGroup}>
                              <label className={styles.label}>Duration per appointment (mins)</label>
                              <input
                                className={styles.input}
                                type="number"
                                min={5}
                                max={240}
                                value={rule.slotDurationMinutes}
                                disabled={!rule.isEnabled}
                                onChange={e => {
                                  const parsed = Number.parseInt(e.target.value || '30', 10)
                                  updateRule(rule.dayOfWeek, {
                                    slotDurationMinutes: Number.isNaN(parsed) ? 30 : parsed,
                                  })
                                }}
                              />
                            </div>
                          </div>
                        </article>
                      )
                    })}
                </div>
              )}

              <button
                className={styles.createBtn}
                type="button"
                onClick={saveSchedule}
                disabled={scheduleSaving || scheduleLoading || appointmentRules.length !== 7 || !selectedAppointmentProfileId}
              >
                {scheduleSaving ? 'Saving…' : 'Save Appointment Settings'}
              </button>
            </section>
          </>
        )}
      </main>
    </div>
  )
}
