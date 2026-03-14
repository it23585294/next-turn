import { apiClient, parseApiError } from './client'
import { getToken } from '../utils/authToken'

export interface AvailableAppointmentSlot {
  slotStart: string
  slotEnd: string
}

export interface BookAppointmentResult {
  appointmentId: string
}

export interface BookAppointmentBody {
  organisationId: string
  slotStart: string
  slotEnd: string
}

export async function getAvailableAppointmentSlots(
  organisationId: string,
  date: string,
): Promise<AvailableAppointmentSlot[]> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<AvailableAppointmentSlot[]>('/appointments/slots', {
      params: {
        organisationId,
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
