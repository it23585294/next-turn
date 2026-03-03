/**
 * Organisation API calls.
 * POST /api/organisations is [AllowAnonymous] — no auth header required.
 */
import { apiClient, parseApiError } from './client'
import type { OrgRegistrationFormValues } from '../schemas/orgRegistrationSchema'
import type { ApiError } from '../types/api'

export interface OrgRegistrationPayload {
  orgName: string
  addressLine1: string
  city: string
  postalCode: string
  country: string
  orgType: string
  adminName: string
  adminEmail: string
}

export interface OrgRegistrationResult {
  organisationId: string
  adminUserId: string
}

/**
 * POST /api/organisations
 * Registers a new organisation and creates the initial OrgAdmin account.
 * Returns the IDs of the newly-created organisation and admin user.
 */
export async function registerOrganisation(
  payload: OrgRegistrationPayload
): Promise<OrgRegistrationResult> {
  try {
    const { data } = await apiClient.post<OrgRegistrationResult>('/organisations', payload)
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/** Maps the Zod form values (camelCase) to the API payload shape. */
export function toOrgRegistrationPayload(
  values: OrgRegistrationFormValues
): OrgRegistrationPayload {
  return {
    orgName:      values.orgName,
    addressLine1: values.addressLine1,
    city:         values.city,
    postalCode:   values.postalCode,
    country:      values.country,
    orgType:      values.orgType,
    adminName:    values.adminName,
    adminEmail:   values.adminEmail,
  }
}
