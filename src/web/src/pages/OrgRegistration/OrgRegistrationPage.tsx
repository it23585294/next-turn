/**
 * OrgRegistrationPage — Public single-page organisation registration form.
 *
 * Route: /register-org  (no tenantId — the organisation doesn't exist yet)
 *
 * Flow:
 *   idle → submitting → success (SuccessCard)
 *                     → error (error banner)
 *
 * On success, the SuccessCard replaces the form and instructs the admin to
 * check their email for temporary credentials. The organisation is in
 * PendingApproval status until a SystemAdmin approves it.
 *
 * Error handling:
 *  409 → duplicate org name banner
 *  400 → domain error banner
 *  422 → first field error as banner (defensive — Zod should catch first)
 *  else → generic banner
 */
import { useForm, type SubmitHandler } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { Link } from 'react-router-dom'

import { orgRegistrationSchema, ORG_TYPES } from '../../schemas/orgRegistrationSchema'
import type { OrgRegistrationFormValues } from '../../schemas/orgRegistrationSchema'
import { registerOrganisation, toOrgRegistrationPayload } from '../../api/organisations'
import type { ApiError } from '../../types/api'

import { FormField } from '../../components/ui/FormField'
import { TextInput } from '../../components/ui/TextInput'
import { Button } from '../../components/ui/Button'

import logoImg from '../../assets/nextTurn-logo.png'
import styles from './OrgRegistrationPage.module.css'

export function OrgRegistrationPage() {
  const [serverError, setServerError] = useState<string | null>(null)
  const [submitted, setSubmitted] = useState(false)
  const [loginPath, setLoginPath] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<OrgRegistrationFormValues>({
    resolver: zodResolver(orgRegistrationSchema),
    mode: 'onTouched',
  })

  const onSubmit: SubmitHandler<OrgRegistrationFormValues> = async (data) => {
    setServerError(null)

    try {
      const result = await registerOrganisation(toOrgRegistrationPayload(data))
      setLoginPath(result.loginPath ?? `/login/${result.organisationId}`)
      setSubmitted(true)
    } catch (err) {
      const apiErr = err as ApiError

      if (apiErr.status === 409) {
        setServerError(
          apiErr.detail ??
          'An organisation with that name is already registered. Please use a different name.'
        )
      } else if (apiErr.status === 422) {
        const firstError = apiErr.errors
          ? Object.values(apiErr.errors)[0]?.[0]
          : undefined
        setServerError(firstError ?? 'Please check your input and try again.')
      } else if (apiErr.status === 400) {
        setServerError(apiErr.detail ?? 'Registration failed. Please check your details and try again.')
      } else {
        setServerError(apiErr.detail ?? 'Something went wrong. Please try again later.')
      }
    }
  }

  if (submitted) {
    return (
      <div className={styles.page}>
        <HeroPanel />
        <div className={styles.formPanel}>
          <SuccessCard loginPath={loginPath} />
        </div>
      </div>
    )
  }

  return (
    <div className={styles.page}>
      <HeroPanel />

      <div className={styles.formPanel}>
          <div className={styles.formContainer}>

            {/* Header */}
            <div className={styles.formHeader}>
              <h1 className={styles.formTitle}>Register your organisation</h1>
              <p className={styles.formSubtitle}>
                Set up your NextTurn account. You'll receive admin credentials by email once submitted.
              </p>
            </div>

            {/* Server-level error banner */}
            {serverError && (
              <div className={styles.serverError} role="alert">
                <ErrorBannerIcon />
                <span>{serverError}</span>
              </div>
            )}

            <form
              className={styles.form}
              onSubmit={handleSubmit(onSubmit)}
              noValidate
            >
              {/* ── Organisation details ── */}
              <fieldset className={styles.fieldset}>
                <legend className={styles.fieldsetLegend}>Organisation details</legend>

                <FormField
                  label="Organisation Name"
                  htmlFor="orgName"
                  error={errors.orgName?.message}
                  required
                >
                  <TextInput
                    id="orgName"
                    placeholder="e.g. City Health Clinic"
                    hasError={!!errors.orgName}
                    autoFocus
                    autoComplete="organization"
                    {...register('orgName')}
                  />
                </FormField>

                <FormField
                  label="Organisation Type"
                  htmlFor="orgType"
                  error={errors.orgType?.message}
                  required
                >
                  <select
                    id="orgType"
                    className={`${styles.select} ${errors.orgType ? styles.selectError : ''}`}
                    defaultValue=""
                    {...register('orgType')}
                  >
                    <option value="" disabled>Select a type…</option>
                    {ORG_TYPES.map((t) => (
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </select>
                </FormField>
              </fieldset>

              {/* ── Registered address ── */}
              <fieldset className={styles.fieldset}>
                <legend className={styles.fieldsetLegend}>Registered address</legend>

                <FormField
                  label="Address Line 1"
                  htmlFor="addressLine1"
                  error={errors.addressLine1?.message}
                  required
                >
                  <TextInput
                    id="addressLine1"
                    placeholder="e.g. 123 Main Street"
                    hasError={!!errors.addressLine1}
                    autoComplete="street-address"
                    {...register('addressLine1')}
                  />
                </FormField>

                <div className={styles.row}>
                  <FormField
                    label="City"
                    htmlFor="city"
                    error={errors.city?.message}
                    required
                  >
                    <TextInput
                      id="city"
                      placeholder="e.g. London"
                      hasError={!!errors.city}
                      autoComplete="address-level2"
                      {...register('city')}
                    />
                  </FormField>

                  <FormField
                    label="Postal Code"
                    htmlFor="postalCode"
                    error={errors.postalCode?.message}
                    required
                  >
                    <TextInput
                      id="postalCode"
                      placeholder="e.g. SW1A 1AA"
                      hasError={!!errors.postalCode}
                      autoComplete="postal-code"
                      {...register('postalCode')}
                    />
                  </FormField>
                </div>

                <FormField
                  label="Country"
                  htmlFor="country"
                  error={errors.country?.message}
                  required
                >
                  <TextInput
                    id="country"
                    placeholder="e.g. United Kingdom"
                    hasError={!!errors.country}
                    autoComplete="country-name"
                    {...register('country')}
                  />
                </FormField>
              </fieldset>

              {/* ── Admin account ── */}
              <fieldset className={styles.fieldset}>
                <legend className={styles.fieldsetLegend}>Admin account</legend>
                <p className={styles.fieldsetHint}>
                  A temporary password will be sent to this email address.
                </p>

                <FormField
                  label="Admin Full Name"
                  htmlFor="adminName"
                  error={errors.adminName?.message}
                  required
                >
                  <TextInput
                    id="adminName"
                    placeholder="e.g. Jane Smith"
                    hasError={!!errors.adminName}
                    autoComplete="name"
                    {...register('adminName')}
                  />
                </FormField>

                <FormField
                  label="Admin Email"
                  htmlFor="adminEmail"
                  error={errors.adminEmail?.message}
                  required
                >
                  <TextInput
                    id="adminEmail"
                    type="email"
                    placeholder="admin@yourorg.com"
                    hasError={!!errors.adminEmail}
                    autoComplete="email"
                    {...register('adminEmail')}
                  />
                </FormField>
              </fieldset>

              <div className={styles.actions}>
                <Button
                  type="submit"
                  variant="primary"
                  fullWidth
                  loading={isSubmitting}
                >
                  {isSubmitting ? 'Submitting…' : 'Register Organisation'}
                </Button>
              </div>
            </form>

            <p className={styles.formFooter}>
              Already registered?{' '}
              <Link to="/" className={styles.link}>Back to home</Link>
            </p>
          </div>
      </div>
    </div>
  )
}

/* ── Sub-components ─────────────────────────────────────────────────────────── */

function HeroPanel() {
  return (
    <aside className={styles.hero} aria-hidden="true">
      <div className={styles.blobTop} />
      <div className={styles.blobBottom} />
      <div className={styles.heroContent}>
        <div className={styles.logo}>
          <img src={logoImg} alt="NextTurn" className={styles.logoImg} />
        </div>
        <h2 className={styles.heroTagline}>
          Bring your organisation online.
        </h2>
        <p className={styles.heroDesc}>
          Register your business on NextTurn and start managing queues and
          appointments for every customer — from day one.
        </p>
        <ul className={styles.heroFeatures}>
          <li><CheckmarkIcon /> No setup fees in Sprint 1</li>
          <li><CheckmarkIcon /> Admin credentials emailed instantly</li>
          <li><CheckmarkIcon /> Your data stays fully isolated</li>
          <li><CheckmarkIcon /> Pending review — approved within 24 h</li>
        </ul>
      </div>
    </aside>
  )
}

function SuccessCard({ loginPath }: { loginPath: string | null }) {
  const loginUrl = loginPath

  return (
    <div className={styles.successCard}>
      <div className={styles.successIcon} aria-label="Success">
        <CheckIcon />
      </div>
      <h2 className={styles.successTitle}>Registration submitted!</h2>
      <p className={styles.successBody}>
        Your organisation is pending approval and will be reviewed within 24 hours.
      </p>
      {loginUrl && (
        <div className={styles.loginDetails}>
          <p className={styles.loginDetailsLabel}>Your admin login link:</p>
          <code className={styles.loginDetailsUrl}>{window.location.origin}{loginUrl}</code>
          <p className={styles.loginDetailsNote}>
            Your temporary password has been logged to the API console (dev mode).
            Use it with your admin email at the link above.
          </p>
          <Link to={loginUrl} className={styles.loginBtn}>
            Go to login →
          </Link>
        </div>
      )}
      <Link to="/" className={styles.successLink}>
        Back to home →
      </Link>
    </div>
  )
}

/* ── Icon components ─────────────────────────────────────────────────────────── */

function CheckmarkIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <path d="M3 8l3.5 3.5L13 4.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

function CheckIcon() {
  return (
    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M5 13l4 4L19 7" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

function ErrorBannerIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true" style={{ flexShrink: 0, marginTop: '1px' }}>
      <circle cx="8" cy="8" r="7" stroke="currentColor" strokeWidth="1.5" />
      <path d="M8 5v3.5M8 11h.01" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  )
}
