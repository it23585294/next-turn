/**
 * RegisterPage — Full-page split-screen user registration UI.
 *
 * Route: /register/:tenantId
 *   tenantId → sent as X-Tenant-Id header (multi-tenant isolation)
 *
 * UX states: idle → submitting → success | error
 */
import { useForm, type SubmitHandler } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useParams, Link } from 'react-router-dom'
import { useState } from 'react'

import { registerSchema, getPasswordStrength } from '../../schemas/registerSchema'
import type { RegisterFormValues } from '../../schemas/registerSchema'
import { registerUser } from '../../api/auth'
import type { ApiError } from '../../types/api'

import { FormField } from '../../components/ui/FormField'
import { TextInput } from '../../components/ui/TextInput'
import { PasswordInput } from '../../components/ui/PasswordInput'
import { PasswordStrengthBar } from '../../components/ui/PasswordStrengthBar'
import { Button } from '../../components/ui/Button'

import logoImg from '../../assets/nextTurn-logo.png'
import styles from './RegisterPage.module.css'

export function RegisterPage() {
  const { tenantId } = useParams<{ tenantId: string }>()
  const [serverError, setServerError] = useState<string | null>(null)
  const [submitted, setSubmitted] = useState(false)

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    mode: 'onTouched',
  })

  const passwordValue = watch('password', '')
  const passwordStrength = getPasswordStrength(passwordValue)

  const onSubmit: SubmitHandler<RegisterFormValues> = async (data) => {
    if (!tenantId) {
      setServerError('This registration link is invalid. Please contact your organization for a valid link.')
      return
    }

    setServerError(null)

    try {
      await registerUser(tenantId, {
        name: data.name,
        email: data.email,
        phone: data.phone || null,
        password: data.password,
      })
      setSubmitted(true)
    } catch (err) {
      const apiErr = err as ApiError

      if (apiErr.status === 400) {
        // Domain error — e.g. "email already in use"
        setServerError(apiErr.detail ?? 'Registration failed. Please try again.')
      } else if (apiErr.status === 422) {
        // Validation error from server (should be caught by Zod first, but be defensive)
        const firstError = apiErr.errors
          ? Object.values(apiErr.errors)[0]?.[0]
          : undefined
        setServerError(firstError ?? 'Please check your input and try again.')
      } else {
        setServerError(apiErr.detail ?? 'Something went wrong. Please try again later.')
      }
    }
  }

  if (!tenantId) {
    return (
      <div className={styles.page}>
        <HeroPanel />
        <div className={styles.formPanel}>
          <div className={styles.errorCard}>
            <span className={styles.errorCardIcon}>🔗</span>
            <h2 className={styles.errorCardTitle}>Invalid Registration Link</h2>
            <p className={styles.errorCardBody}>
              This link is missing an organization identifier.
              Please ask your organization for the correct registration URL.
            </p>
          </div>
        </div>
      </div>
    )
  }

  if (submitted) {
    return (
      <div className={styles.page}>
        <HeroPanel />
        <div className={styles.formPanel}>
          <div className={styles.successCard}>
            <div className={styles.successIcon} aria-label="Success">
              <CheckIcon />
            </div>
            <h2 className={styles.successTitle}>Account Created!</h2>
            <p className={styles.successBody}>
              Welcome to NextTurn. Your account has been created successfully.
              You can now sign in and start managing your place in the queue.
            </p>
            <Link to={`/login/${tenantId}`} className={styles.successLink}>
              Go to Sign In →
            </Link>
          </div>
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
            <h1 className={styles.formTitle}>Create your account</h1>
            <p className={styles.formSubtitle}>
              Join your organization's queue management system.
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
            {/* Full Name */}
            <FormField label="Full Name" htmlFor="name" error={errors.name?.message} required>
              <TextInput
                id="name"
                placeholder="e.g. Maria Santos"
                hasError={!!errors.name}
                autoComplete="name"
                autoFocus
                {...register('name')}
              />
            </FormField>

            {/* Email */}
            <FormField label="Email Address" htmlFor="email" error={errors.email?.message} required>
              <TextInput
                id="email"
                type="email"
                placeholder="you@example.com"
                hasError={!!errors.email}
                autoComplete="email"
                {...register('email')}
              />
            </FormField>

            {/* Phone (optional) */}
            <FormField
              label="Phone Number"
              htmlFor="phone"
              error={errors.phone?.message}
              hint="Optional — used for queue notifications"
            >
              <TextInput
                id="phone"
                type="tel"
                placeholder="+63 912 345 6789"
                hasError={!!errors.phone}
                autoComplete="tel"
                {...register('phone')}
              />
            </FormField>

            {/* Password */}
            <FormField label="Password" htmlFor="password" error={errors.password?.message} required>
              <PasswordInput
                id="password"
                placeholder="Create a strong password"
                hasError={!!errors.password}
                autoComplete="new-password"
                {...register('password')}
              />
              <PasswordStrengthBar score={passwordStrength} />
            </FormField>

            {/* Confirm Password */}
            <FormField
              label="Confirm Password"
              htmlFor="confirmPassword"
              error={errors.confirmPassword?.message}
              required
            >
              <PasswordInput
                id="confirmPassword"
                placeholder="Repeat your password"
                hasError={!!errors.confirmPassword}
                autoComplete="new-password"
                {...register('confirmPassword')}
              />
            </FormField>

            {/* Submit */}
            <div className={styles.actions}>
              <Button
                type="submit"
                variant="primary"
                size="lg"
                fullWidth
                loading={isSubmitting}
              >
                Create Account
              </Button>
            </div>
          </form>

          {/* Footer */}
          <p className={styles.formFooter}>
            Already have an account?{' '}
            <Link to={`/login/${tenantId}`} className={styles.link}>
              Sign in
            </Link>
          </p>

          <p className={styles.legal}>
            By creating an account, you agree to our{' '}
            <a href="#" className={styles.link}>Terms of Service</a> and{' '}
            <a href="#" className={styles.link}>Privacy Policy</a>.
          </p>
        </div>
      </div>
    </div>
  )
}

/* ------------------------------------------------------------------ */
/* Hero panel (left side)                                               */
/* ------------------------------------------------------------------ */
function HeroPanel() {
  return (
    <div className={styles.hero} aria-hidden="true">
      <div className={styles.heroContent}>
        {/* Logo */}
        <div className={styles.logo}>
          <img src={logoImg} alt="NextTurn" className={styles.logoImg} />
        </div>

        {/* Tagline */}
        <h2 className={styles.heroTagline}>
          Skip the Wait.<br />Take Control.
        </h2>
        <p className={styles.heroDesc}>
          A smarter queue and appointment platform built for organizations that
          value your time as much as their own.
        </p>

        {/* Feature highlights */}
        <ul className={styles.features}>
          <FeatureItem icon={<QueueIcon />} text="Join queues from anywhere, anytime" />
          <FeatureItem icon={<CalendarIcon />} text="Book, reschedule, or cancel appointments" />
          <FeatureItem icon={<BellIcon />} text="Get notified when it's your turn" />
          <FeatureItem icon={<ShieldIcon />} text="Your data is securely isolated per organization" />
        </ul>
      </div>

      {/* Decorative blobs */}
      <div className={styles.blobTop} aria-hidden="true" />
      <div className={styles.blobBottom} aria-hidden="true" />
    </div>
  )
}

function FeatureItem({ icon, text }: { icon: React.ReactNode; text: string }) {
  return (
    <li className={styles.featureItem}>
      <span className={styles.featureIcon}>{icon}</span>
      <span>{text}</span>
    </li>
  )
}

/* ------------------------------------------------------------------ */
/* SVG icon set                                                         */
/* ------------------------------------------------------------------ */
function QueueIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="8" y1="6" x2="21" y2="6"/>
      <line x1="8" y1="12" x2="21" y2="12"/>
      <line x1="8" y1="18" x2="21" y2="18"/>
      <line x1="3" y1="6" x2="3.01" y2="6"/>
      <line x1="3" y1="12" x2="3.01" y2="12"/>
      <line x1="3" y1="18" x2="3.01" y2="18"/>
    </svg>
  )
}

function CalendarIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
      <line x1="16" y1="2" x2="16" y2="6"/>
      <line x1="8"  y1="2" x2="8"  y2="6"/>
      <line x1="3"  y1="10" x2="21" y2="10"/>
    </svg>
  )
}

function BellIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
      <path d="M13.73 21a2 2 0 01-3.46 0"/>
    </svg>
  )
}

function ShieldIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
    </svg>
  )
}

function CheckIcon() {
  return (
    <svg width="32" height="32" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="20 6 9 17 4 12"/>
    </svg>
  )
}

function ErrorBannerIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      style={{ flexShrink: 0 }}>
      <circle cx="12" cy="12" r="10"/>
      <line x1="12" y1="8" x2="12" y2="12"/>
      <line x1="12" y1="16" x2="12.01" y2="16"/>
    </svg>
  )
}
