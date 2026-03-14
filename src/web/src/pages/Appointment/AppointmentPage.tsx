import { useEffect, useMemo, useState } from 'react'
import { DayPicker } from 'react-day-picker'
import { useNavigate, useParams } from 'react-router-dom'
import { getAvailableAppointmentSlots, bookAppointment, type AvailableAppointmentSlot } from '../../api/appointments'
import { getTokenPayload } from '../../utils/authToken'
import type { ApiError } from '../../types/api'
import logoImg from '../../assets/nextTurn-logo.png'
import 'react-day-picker/style.css'
import styles from './AppointmentPage.module.css'

const EMPTY_TENANT = '00000000-0000-0000-0000-000000000000'
const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

type BookingState =
  | { status: 'idle' }
  | { status: 'booking' }
  | { status: 'success'; appointmentId: string }
  | { status: 'error'; detail: string }

function isGuid(value: string): boolean {
  return GUID_REGEX.test(value.trim())
}

function toDateOnly(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function formatSlotLabel(slot: AvailableAppointmentSlot): string {
  const start = new Date(slot.slotStart)
  const end = new Date(slot.slotEnd)

  return `${start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - ${end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
}

export function AppointmentPage() {
  const { tenantId } = useParams<{ tenantId?: string }>()
  const navigate = useNavigate()

  const tokenPayload = getTokenPayload()

  const initialOrganisationId = useMemo(() => {
    if (tenantId && tenantId !== EMPTY_TENANT) return tenantId
    if (tokenPayload?.tid && tokenPayload.tid !== EMPTY_TENANT) return tokenPayload.tid
    return ''
  }, [tenantId, tokenPayload?.tid])

  const [organisationId, setOrganisationId] = useState(initialOrganisationId)
  const [selectedDate, setSelectedDate] = useState<Date>(new Date())
  const [slots, setSlots] = useState<AvailableAppointmentSlot[]>([])
  const [slotsLoading, setSlotsLoading] = useState(false)
  const [slotsError, setSlotsError] = useState<string | null>(null)
  const [selectedSlot, setSelectedSlot] = useState<AvailableAppointmentSlot | null>(null)
  const [booking, setBooking] = useState<BookingState>({ status: 'idle' })

  useEffect(() => {
    if (!isGuid(organisationId)) {
      setSlots([])
      setSelectedSlot(null)
      return
    }

    const date = toDateOnly(selectedDate)
    setSlotsLoading(true)
    setSlotsError(null)
    setSelectedSlot(null)

    getAvailableAppointmentSlots(organisationId, date)
      .then(data => {
        setSlots(data)
      })
      .catch((err: ApiError) => {
        setSlotsError(err.detail ?? 'Could not load available slots.')
      })
      .finally(() => {
        setSlotsLoading(false)
      })
  }, [organisationId, selectedDate])

  function handleBack() {
    const role = tokenPayload?.role
    if (tenantId && (role === 'OrgAdmin' || role === 'SystemAdmin')) {
      navigate(`/admin/${tenantId}`)
      return
    }

    if (tenantId) {
      navigate(`/dashboard/${tenantId}`)
      return
    }

    navigate('/dashboard')
  }

  async function handleConfirmBooking() {
    if (!selectedSlot || !isGuid(organisationId)) return

    setBooking({ status: 'booking' })

    try {
      const result = await bookAppointment({
        organisationId,
        slotStart: selectedSlot.slotStart,
        slotEnd: selectedSlot.slotEnd,
      })

      setBooking({ status: 'success', appointmentId: result.appointmentId })

      setSlots(prev =>
        prev.filter(s => s.slotStart !== selectedSlot.slotStart || s.slotEnd !== selectedSlot.slotEnd)
      )
      setSelectedSlot(null)
    } catch (err) {
      const apiErr = err as ApiError
      setBooking({
        status: 'error',
        detail: apiErr.detail ?? 'Could not complete booking. Please choose another slot.',
      })
    }
  }

  const hasValidOrg = isGuid(organisationId)

  return (
    <div className={styles.page}>
      <header className={styles.topBar}>
        <button className={styles.backBtn} type="button" onClick={handleBack}>
          <span aria-hidden="true">&larr;</span>
          <span>Back</span>
        </button>
        <img src={logoImg} alt="NextTurn" className={styles.logo} />
      </header>

      <main className={styles.main}>
        <section className={styles.leftCol}>
          <h1 className={styles.heading}>Book an appointment</h1>
          <p className={styles.subheading}>
            Choose a date, view available slots for your selected organisation, then confirm.
          </p>

          <label htmlFor="organisationId" className={styles.label}>Organisation ID</label>
          <input
            id="organisationId"
            className={styles.input}
            value={organisationId}
            onChange={e => {
              setOrganisationId(e.target.value)
              setBooking({ status: 'idle' })
            }}
            placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
          />

          {!hasValidOrg && organisationId.trim().length > 0 && (
            <p className={styles.validation}>Enter a valid organisation ID to load slots.</p>
          )}

          <div className={styles.calendarWrap}>
            <DayPicker
              mode="single"
              selected={selectedDate}
              onSelect={(date) => {
                if (!date) return
                setSelectedDate(date)
                setBooking({ status: 'idle' })
              }}
              disabled={{ before: new Date() }}
              className={styles.dayPicker}
            />
          </div>
        </section>

        <section className={styles.rightCol}>
          <div className={styles.slotHeader}>
            <h2>Available slots</h2>
            <span>{toDateOnly(selectedDate)}</span>
          </div>

          {slotsLoading && (
            <div className={styles.stateCard}>Loading slots...</div>
          )}

          {!slotsLoading && slotsError && (
            <div className={`${styles.stateCard} ${styles.errorCard}`}>{slotsError}</div>
          )}

          {!slotsLoading && !slotsError && !hasValidOrg && (
            <div className={styles.stateCard}>Enter an organisation ID to view slots.</div>
          )}

          {!slotsLoading && !slotsError && hasValidOrg && slots.length === 0 && (
            <div className={styles.stateCard}>No slots available for this date.</div>
          )}

          {!slotsLoading && !slotsError && slots.length > 0 && (
            <div className={styles.slotGrid}>
              {slots.map(slot => {
                const selected = selectedSlot?.slotStart === slot.slotStart && selectedSlot?.slotEnd === slot.slotEnd
                return (
                  <button
                    key={`${slot.slotStart}-${slot.slotEnd}`}
                    type="button"
                    className={`${styles.slotBtn} ${selected ? styles.slotBtnSelected : ''}`}
                    onClick={() => {
                      setSelectedSlot(slot)
                      setBooking({ status: 'idle' })
                    }}
                  >
                    {formatSlotLabel(slot)}
                  </button>
                )
              })}
            </div>
          )}

          <div className={styles.confirmBar}>
            <button
              type="button"
              className={styles.confirmBtn}
              disabled={!selectedSlot || booking.status === 'booking'}
              onClick={handleConfirmBooking}
            >
              {booking.status === 'booking' ? 'Booking...' : 'Confirm appointment'}
            </button>

            {selectedSlot && (
              <p className={styles.selectedHint}>
                Selected: {formatSlotLabel(selectedSlot)}
              </p>
            )}
          </div>

          {booking.status === 'success' && (
            <div className={`${styles.stateCard} ${styles.successCard}`}>
              Appointment confirmed. ID: <strong>{booking.appointmentId}</strong>
            </div>
          )}

          {booking.status === 'error' && (
            <div className={`${styles.stateCard} ${styles.errorCard}`}>
              {booking.detail}
            </div>
          )}
        </section>
      </main>
    </div>
  )
}
