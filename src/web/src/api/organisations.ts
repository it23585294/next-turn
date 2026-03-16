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
  loginPath?: string
}

export interface ResolveOrganisationLoginResult {
  organisationId: string
  organisationName: string
  loginPath: string
}

export interface ResolveOrganisationTenantResult {
  organisationId: string
  organisationName: string
  slug: string
}

export interface MemberWorkspaceOption {
  organisationId: string
  organisationName: string
  slug: string
  loginPath: string
  role: string
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

/**
 * POST /api/organisations/resolve-login
 * Resolves the tenant-scoped login path for an organisation admin email.
 */
export async function resolveOrganisationLogin(
  adminEmail: string,
): Promise<ResolveOrganisationLoginResult> {
  try {
    const { data } = await apiClient.post<ResolveOrganisationLoginResult>(
      '/organisations/resolve-login',
      { adminEmail },
    )
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * GET /api/organisations/resolve-tenant?slug={slug}
 * Resolves organisation tenant details from a public org slug.
 */
export async function resolveOrganisationTenant(
  slug: string,
): Promise<ResolveOrganisationTenantResult> {
  try {
    const { data } = await apiClient.get<ResolveOrganisationTenantResult>(
      '/organisations/resolve-tenant',
      {
        params: { slug },
      }
    )
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * POST /api/organisations/resolve-member-login
 * Returns workspace login candidates for staff/admin emails.
 */
export async function resolveMemberLogin(
  email: string,
): Promise<MemberWorkspaceOption[]> {
  try {
    const { data } = await apiClient.post<MemberWorkspaceOption[]>(
      '/organisations/resolve-member-login',
      { email },
    )
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}
