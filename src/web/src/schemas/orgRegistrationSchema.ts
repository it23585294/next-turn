/**
 * Zod schema for organisation registration.
 * Mirrors RegisterOrganisationCommandValidator on the backend.
 *
 * Rules (keeping parity with FluentValidation rules in the handler):
 *  - orgName:      required, max 200 chars
 *  - addressLine1: required, max 300 chars
 *  - city:         required, max 100 chars
 *  - postalCode:   required, max 20 chars
 *  - country:      required, max 100 chars
 *  - orgType:      one of the five valid OrganisationType enum values
 *  - adminName:    required, max 200 chars
 *  - adminEmail:   required, valid email format, max 320 chars
 */
import { z } from 'zod'

export const ORG_TYPES = [
  'Healthcare',
  'Retail',
  'Government',
  'Education',
  'Other',
] as const

export type OrgType = (typeof ORG_TYPES)[number]

export const orgRegistrationSchema = z.object({
  orgName: z
    .string()
    .min(1, 'Organisation name is required')
    .max(200, 'Organisation name must not exceed 200 characters'),

  addressLine1: z
    .string()
    .min(1, 'Address line 1 is required')
    .max(300, 'Address line 1 must not exceed 300 characters'),

  city: z
    .string()
    .min(1, 'City is required')
    .max(100, 'City must not exceed 100 characters'),

  postalCode: z
    .string()
    .min(1, 'Postal code is required')
    .max(20, 'Postal code must not exceed 20 characters'),

  country: z
    .string()
    .min(1, 'Country is required')
    .max(100, 'Country must not exceed 100 characters'),

  orgType: z.enum(ORG_TYPES, {
    message: 'Please select a valid organisation type',
  }),

  adminName: z
    .string()
    .min(1, 'Admin name is required')
    .max(200, 'Admin name must not exceed 200 characters'),

  adminEmail: z
    .string()
    .min(1, 'Admin email is required')
    .email('Enter a valid email address')
    .max(320, 'Admin email must not exceed 320 characters'),
})

export type OrgRegistrationFormValues = z.infer<typeof orgRegistrationSchema>
