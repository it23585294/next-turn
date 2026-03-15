import { apiClient, parseApiError } from './client'
import { getToken } from '../utils/authToken'

export interface AvailableAppointmentSlot {
  slotStart: string
  slotEnd: string
  isBooked: boolean
}

export interface AppointmentDayRule {
  dayOfWeek: number
  isEnabled: boolean
  startTime: string
  endTime: string
  slotDurationMinutes: number
}

export interface AppointmentScheduleConfig {
  shareableLink: string
  dayRules: AppointmentDayRule[]
}

export interface AppointmentProfileSummary {
  appointmentProfileId: string
  name: string
  isActive: boolean
  shareableLink: string
}

export interface BookAppointmentResult {
  appointmentId: string
}

export interface RescheduleAppointmentResult {
  appointmentId: string
  slotStart: string
  slotEnd: string
}

export interface CancelAppointmentResult {
  appointmentId: string
  lateCancellation: boolean
}

export interface ConfigureAppointmentScheduleResult {
  shareableLink: string
}

export interface BookAppointmentBody {
  organisationId: string
  appointmentProfileId: string
  slotStart: string
  slotEnd: string
}

export async function getAvailableAppointmentSlots(
  organisationId: string,
  appointmentProfileId: string,
  date: string,
): Promise<AvailableAppointmentSlot[]> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<AvailableAppointmentSlot[]>('/appointments/slots', {
      params: {
        organisationId,
        appointmentProfileId,
        date,
      },
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': organisationId,
      },
    })

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function bookAppointment(body: BookAppointmentBody): Promise<BookAppointmentResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<BookAppointmentResult>('/appointments', body, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': body.organisationId,
      },
    })

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function rescheduleAppointment(
  appointmentId: string,
  body: { newSlotStart: string; newSlotEnd: string },
  organisationId: string,
): Promise<RescheduleAppointmentResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.put<RescheduleAppointmentResult>(
      `/appointments/${appointmentId}/reschedule`,
      body,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': organisationId,
        },
      }
    )

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function cancelAppointment(
  appointmentId: string,
  organisationId: string,
): Promise<CancelAppointmentResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<CancelAppointmentResult>(
      `/appointments/${appointmentId}/cancel`,
      null,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': organisationId,
        },
      }
    )

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function getAppointmentSchedule(
  organisationId: string,
  appointmentProfileId: string,
): Promise<AppointmentScheduleConfig> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<AppointmentScheduleConfig>('/appointments/config', {
      params: { appointmentProfileId },
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': organisationId,
      },
    })

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function configureAppointmentSchedule(
  organisationId: string,
  appointmentProfileId: string,
  dayRules: AppointmentDayRule[],
): Promise<ConfigureAppointmentScheduleResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.put<ConfigureAppointmentScheduleResult>(
      '/appointments/config',
      { dayRules },
      {
        params: { appointmentProfileId },
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': organisationId,
        },
      }
    )

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function listAppointmentProfiles(
  organisationId: string,
): Promise<AppointmentProfileSummary[]> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<AppointmentProfileSummary[]>('/appointments/profiles', {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': organisationId,
      },
    })

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function createAppointmentProfile(
  organisationId: string,
  name: string,
): Promise<AppointmentProfileSummary> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<AppointmentProfileSummary>(
      '/appointments/profiles',
      { name },
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': organisationId,
        },
      },
    )

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}
