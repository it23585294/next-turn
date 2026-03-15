import { useEffect, useMemo, useState } from 'react'
import { DayPicker } from 'react-day-picker'
import { useNavigate, useParams } from 'react-router-dom'
import {
  getAvailableAppointmentSlots,
  getAppointmentBookingContext,
  getMyAppointmentBookings,
  bookAppointment,
  cancelAppointment,
  rescheduleAppointment,
  type AvailableAppointmentSlot,
  type AppointmentBookingContext,
  type MyAppointmentBooking,
} from '../../api/appointments'
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
  | { status: 'success'; appointmentId: string; message: string }
  | { status: 'error'; detail: string }

type CurrentAppointment = {
  appointmentId: string
  organisationId: string
  slotStart: string
  slotEnd: string
}

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

function sameSlot(a: { slotStart: string; slotEnd: string }, b: { slotStart: string; slotEnd: string }): boolean {
  return a.slotStart === b.slotStart && a.slotEnd === b.slotEnd
}

function formatSlotRange(slotStart: string, slotEnd: string): string {
  return formatSlotLabel({ slotStart, slotEnd, isBooked: false })
}

export function AppointmentPage() {
  const { tenantId, appointmentProfileId: routeAppointmentProfileId } = useParams<{ tenantId?: string; appointmentProfileId?: string }>()
  const navigate = useNavigate()

  const tokenPayload = getTokenPayload()

  const initialOrganisationId = useMemo(() => {
    if (tenantId && tenantId !== EMPTY_TENANT) return tenantId
    if (tokenPayload?.tid && tokenPayload.tid !== EMPTY_TENANT) return tokenPayload.tid
    return ''
  }, [tenantId, tokenPayload?.tid])

  const [organisationId, setOrganisationId] = useState(initialOrganisationId)
  const [appointmentProfileId] = useState(routeAppointmentProfileId ?? '')
  const [selectedDate, setSelectedDate] = useState<Date>(new Date())
  const [slots, setSlots] = useState<AvailableAppointmentSlot[]>([])
  const [slotsLoading, setSlotsLoading] = useState(false)
  const [slotsError, setSlotsError] = useState<string | null>(null)
  const [bookingContext, setBookingContext] = useState<AppointmentBookingContext | null>(null)
  const [bookingContextLoading, setBookingContextLoading] = useState(false)
  const [bookingContextError, setBookingContextError] = useState<string | null>(null)
  const [selectedSlot, setSelectedSlot] = useState<AvailableAppointmentSlot | null>(null)
  const [currentAppointment, setCurrentAppointment] = useState<CurrentAppointment | null>(null)
  const [currentAppointmentLoading, setCurrentAppointmentLoading] = useState(false)
  const [currentAppointmentError, setCurrentAppointmentError] = useState<string | null>(null)
  const [booking, setBooking] = useState<BookingState>({ status: 'idle' })
  const [showCancelModal, setShowCancelModal] = useState(false)
  const [isCancelling, setIsCancelling] = useState(false)

  useEffect(() => {
    setOrganisationId(initialOrganisationId)
  }, [initialOrganisationId])

  useEffect(() => {
    if (!isGuid(organisationId) || !isGuid(appointmentProfileId)) {
      setBookingContext(null)
      setBookingContextError('Open this page from a valid appointment booking link.')
      return
    }

    setBookingContextLoading(true)
    setBookingContextError(null)

    getAppointmentBookingContext(organisationId, appointmentProfileId)
      .then(data => {
        setBookingContext(data)
      })
      .catch((err: ApiError) => {
        setBookingContext(null)
        setBookingContextError(err.detail ?? 'Could not load appointment details.')
      })
      .finally(() => {
        setBookingContextLoading(false)
      })
  }, [organisationId, appointmentProfileId])

  useEffect(() => {
    if (!isGuid(organisationId) || !isGuid(appointmentProfileId)) {
      setCurrentAppointment(null)
      setCurrentAppointmentError(null)
      return
    }

    setCurrentAppointmentLoading(true)
    setCurrentAppointmentError(null)

    getMyAppointmentBookings()
      .then(bookings => {
        const matchingBooking = bookings
          .filter((b: MyAppointmentBooking) =>
            b.organisationId === organisationId &&
            b.appointmentProfileId === appointmentProfileId)
          .sort((a, b) => a.slotStart.localeCompare(b.slotStart))[0]

        if (!matchingBooking) {
          setCurrentAppointment(null)
          return
        }

        setCurrentAppointment({
          appointmentId: matchingBooking.appointmentId,
          organisationId: matchingBooking.organisationId,
          slotStart: matchingBooking.slotStart,
          slotEnd: matchingBooking.slotEnd,
        })

        setSelectedDate(new Date(matchingBooking.slotStart))
      })
      .catch((err: ApiError) => {
        setCurrentAppointment(null)
        setCurrentAppointmentError(err.detail ?? 'Could not load your existing appointment.')
      })
      .finally(() => {
        setCurrentAppointmentLoading(false)
      })
  }, [organisationId, appointmentProfileId])

  useEffect(() => {
    if (!isGuid(organisationId) || !isGuid(appointmentProfileId)) {
      setSlots([])
      setSelectedSlot(null)
      return
    }

    const date = toDateOnly(selectedDate)
    setSlotsLoading(true)
    setSlotsError(null)
    setSelectedSlot(null)

    getAvailableAppointmentSlots(organisationId, appointmentProfileId, date)
      .then(data => {
        setSlots(data)
      })
      .catch((err: ApiError) => {
        setSlotsError(err.detail ?? 'Could not load available slots.')
      })
      .finally(() => {
        setSlotsLoading(false)
      })
  }, [organisationId, appointmentProfileId, selectedDate])

  function handleBack() {
    const role = tokenPayload?.role
    const destinationTenantId = tenantId ?? organisationId

    if (destinationTenantId && (role === 'OrgAdmin' || role === 'SystemAdmin')) {
      navigate(`/admin/${destinationTenantId}`)
      return
    }

    if (destinationTenantId) {
      navigate(`/dashboard/${destinationTenantId}`)
      return
    }

    navigate('/dashboard')
  }

  async function handleConfirmBooking() {
    if (!selectedSlot || selectedSlot.isBooked || !isGuid(organisationId) || !isGuid(appointmentProfileId)) return

    setBooking({ status: 'booking' })

    try {
      if (currentAppointment) {
        const result = await rescheduleAppointment(
          currentAppointment.appointmentId,
          {
            newSlotStart: selectedSlot.slotStart,
            newSlotEnd: selectedSlot.slotEnd,
          },
          organisationId,
        )

        setCurrentAppointment({
          appointmentId: result.appointmentId,
          organisationId,
          slotStart: result.slotStart,
          slotEnd: result.slotEnd,
        })

        setBooking({
          status: 'success',
          appointmentId: result.appointmentId,
          message: 'Appointment rescheduled. New appointment ID:',
        })
      } else {
        const result = await bookAppointment({
          organisationId,
          appointmentProfileId,
          slotStart: selectedSlot.slotStart,
          slotEnd: selectedSlot.slotEnd,
        })

        setCurrentAppointment({
          appointmentId: result.appointmentId,
          organisationId,
          slotStart: selectedSlot.slotStart,
          slotEnd: selectedSlot.slotEnd,
        })

        setBooking({
          status: 'success',
          appointmentId: result.appointmentId,
          message: 'Appointment confirmed. ID:',
        })
      }

      setSlots(prev => prev.map(s => sameSlot(s, selectedSlot) ? { ...s, isBooked: true } : s))
      setSelectedSlot(null)
    } catch (err) {
      const apiErr = err as ApiError
      setBooking({
        status: 'error',
        detail: apiErr.detail ?? 'Could not complete booking. Please choose another slot.',
      })
    }
  }

  async function handleConfirmCancel() {
    if (!currentAppointment || !isGuid(organisationId)) return

    setIsCancelling(true)

    try {
      const result = await cancelAppointment(currentAppointment.appointmentId, organisationId)

      if (toDateOnly(new Date(currentAppointment.slotStart)) === toDateOnly(selectedDate)) {
        setSlots(prev => {
          const exists = prev.some(s =>
            s.slotStart === currentAppointment.slotStart && s.slotEnd === currentAppointment.slotEnd)

          if (exists) {
            return prev.map(s =>
              s.slotStart === currentAppointment.slotStart && s.slotEnd === currentAppointment.slotEnd
                ? { ...s, isBooked: false }
                : s)
          }

          return [...prev, {
            slotStart: currentAppointment.slotStart,
            slotEnd: currentAppointment.slotEnd,
            isBooked: false,
          }].sort((a, b) => a.slotStart.localeCompare(b.slotStart))
        })
      }

      setCurrentAppointment(null)
      setSelectedSlot(null)
      setShowCancelModal(false)

      setBooking({
        status: 'success',
        appointmentId: result.appointmentId,
        message: result.lateCancellation
          ? 'Appointment cancelled (late cancellation recorded). ID:'
          : 'Appointment cancelled. ID:',
      })
    } catch (err) {
      const apiErr = err as ApiError
      setBooking({
        status: 'error',
        detail: apiErr.detail ?? 'Could not cancel appointment.',
      })
    } finally {
      setIsCancelling(false)
    }
  }

  const hasValidOrg = isGuid(organisationId)
  const hasValidProfile = isGuid(appointmentProfileId)
  const isRescheduleMode = currentAppointment !== null
  const hasValidContext = hasValidOrg && hasValidProfile

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
          <h1 className={styles.heading}>{isRescheduleMode ? 'Reschedule your appointment' : 'Book your appointment'}</h1>
          <p className={styles.subheading}>
            {isRescheduleMode
              ? 'Pick a new available slot and confirm to move your appointment.'
              : 'Choose a date and time slot, then confirm your appointment in one step.'}
          </p>

          <div className={styles.detailsCard}>
            <p className={styles.detailsEyebrow}>Appointment details</p>
            {bookingContextLoading && <p className={styles.detailsMuted}>Loading appointment details...</p>}
            {!bookingContextLoading && bookingContextError && (
              <p className={styles.detailsError}>{bookingContextError}</p>
            )}
            {!bookingContextLoading && !bookingContextError && bookingContext && (
              <>
                <div className={styles.detailRow}>
                  <span className={styles.detailLabel}>Organisation</span>
                  <strong>{bookingContext.organisationName}</strong>
                </div>
                <div className={styles.detailRow}>
                  <span className={styles.detailLabel}>Appointment</span>
                  <strong>{bookingContext.appointmentProfileName}</strong>
                </div>
                <div className={styles.detailRow}>
                  <span className={styles.detailLabel}>Status</span>
                  <strong>{bookingContext.isProfileActive ? 'Open for booking' : 'Currently unavailable'}</strong>
                </div>
              </>
            )}
          </div>

          <div className={styles.policyBanner}>
            <strong>Booking policy:</strong> one appointment per day per user.
          </div>

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
          {currentAppointment && (
            <div className={styles.currentAppointmentCard} data-testid="current-appointment-card">
              <p className={styles.currentAppointmentTitle}>Current appointment</p>
              <p className={styles.currentAppointmentLine}>
                Appointment ID: <strong>{currentAppointment.appointmentId}</strong>
              </p>
              <p className={styles.currentAppointmentLine}>
                Slot: <strong>{formatSlotRange(currentAppointment.slotStart, currentAppointment.slotEnd)}</strong>
              </p>
              <button
                type="button"
                className={styles.cancelTriggerBtn}
                onClick={() => setShowCancelModal(true)}
                data-testid="open-cancel-modal-btn"
              >
                Cancel appointment
              </button>
            </div>
          )}

          <div className={styles.slotHeader}>
            <h2>{isRescheduleMode ? 'New available slots' : 'Available slots'}</h2>
            <span>{toDateOnly(selectedDate)}</span>
          </div>

          {currentAppointmentLoading && !currentAppointment && (
            <div className={styles.stateCard}>Checking your existing booking...</div>
          )}

          {!currentAppointmentLoading && currentAppointmentError && (
            <div className={`${styles.stateCard} ${styles.errorCard}`}>{currentAppointmentError}</div>
          )}

          {slotsLoading && (
            <div className={styles.stateCard}>Loading slots...</div>
          )}

          {!slotsLoading && slotsError && (
            <div className={`${styles.stateCard} ${styles.errorCard}`}>{slotsError}</div>
          )}

          {!slotsLoading && !slotsError && (!hasValidOrg || !hasValidProfile) && (
            <div className={styles.stateCard}>Open this page using a valid appointment link to view slots.</div>
          )}

          {!slotsLoading && !slotsError && hasValidOrg && hasValidProfile && slots.length === 0 && (
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
                    className={`${styles.slotBtn} ${slot.isBooked ? styles.slotBtnBooked : ''} ${selected ? styles.slotBtnSelected : ''}`}
                    disabled={slot.isBooked}
                    onClick={() => {
                      setSelectedSlot(slot)
                      setBooking({ status: 'idle' })
                    }}
                  >
                    {formatSlotLabel(slot)}{slot.isBooked ? ' (Booked)' : ''}
                  </button>
                )
              })}
            </div>
          )}

          <div className={styles.confirmBar}>
            <button
              type="button"
              className={styles.confirmBtn}
              disabled={
                !selectedSlot ||
                booking.status === 'booking' ||
                !bookingContext?.isProfileActive ||
                !hasValidContext
              }
              onClick={handleConfirmBooking}
            >
              {booking.status === 'booking'
                ? (isRescheduleMode ? 'Rescheduling...' : 'Booking...')
                : (isRescheduleMode ? 'Confirm reschedule' : 'Confirm appointment')}
            </button>

            {selectedSlot && (
              <p className={styles.selectedHint}>
                {isRescheduleMode ? 'New slot:' : 'Selected:'} {formatSlotLabel(selectedSlot)}
              </p>
            )}

            {!bookingContextLoading && bookingContext && !bookingContext.isProfileActive && (
              <p className={styles.validation}>This appointment profile is currently not accepting bookings.</p>
            )}
          </div>

          {booking.status === 'success' && (
            <div className={`${styles.stateCard} ${styles.successCard}`}>
              {booking.message} <strong>{booking.appointmentId}</strong>
            </div>
          )}

          {booking.status === 'error' && (
            <div className={`${styles.stateCard} ${styles.errorCard}`}>
              {booking.detail}
            </div>
          )}
        </section>
      </main>

      {showCancelModal && currentAppointment && (
        <div className={styles.modalOverlay} onClick={() => setShowCancelModal(false)} role="presentation">
          <div className={styles.modalCard} onClick={e => e.stopPropagation()}>
            <h2 className={styles.modalTitle}>Cancel appointment?</h2>
            <p className={styles.modalBody}>
              This will free the slot for other users.
            </p>
            <p className={styles.modalBody}>
              Slot: <strong>{formatSlotRange(currentAppointment.slotStart, currentAppointment.slotEnd)}</strong>
            </p>
            <div className={styles.modalActions}>
              <button
                type="button"
                className={styles.modalSecondaryBtn}
                onClick={() => setShowCancelModal(false)}
                disabled={isCancelling}
              >
                Keep appointment
              </button>
              <button
                type="button"
                className={styles.modalDangerBtn}
                onClick={handleConfirmCancel}
                disabled={isCancelling}
                data-testid="confirm-cancel-btn"
              >
                {isCancelling ? 'Cancelling...' : 'Confirm cancellation'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
