import { useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { acceptStaffInvite } from '../../api/auth'
import type { ApiError } from '../../types/api'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './StaffInviteAcceptPage.module.css'

export function StaffInviteAcceptPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = useMemo(() => searchParams.get('token') ?? '', [searchParams])

  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!token) {
      setError('Invite token is missing.')
      return
    }

    if (password !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    setLoading(true)
    setError(null)
    setSuccess(null)

    try {
      await acceptStaffInvite(token, password)
      setSuccess('Invite accepted. You can now sign in from your organisation login page.')
      setTimeout(() => navigate('/', { replace: true }), 1400)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not accept invite. The link may be invalid or expired.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <Link to="/" className={styles.logoLink}>
          <img src={logoImg} alt="NextTurn" className={styles.logo} />
        </Link>
      </header>

      <main className={styles.main}>
        <section className={styles.card}>
          <h1 className={styles.title}>Set Your Staff Password</h1>
          <p className={styles.subtitle}>
            Accept your invite by setting a secure password for your staff account.
          </p>

          <form onSubmit={handleSubmit} className={styles.form} noValidate>
            <label className={styles.label} htmlFor="password">Password</label>
            <input
              id="password"
              className={styles.input}
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="At least 8 chars, uppercase, number, symbol"
              autoComplete="new-password"
            />

            <label className={styles.label} htmlFor="confirm-password">Confirm password</label>
            <input
              id="confirm-password"
              className={styles.input}
              type="password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              placeholder="Repeat your password"
              autoComplete="new-password"
            />

            <button
              type="submit"
              className={styles.submitBtn}
              disabled={loading || !token || !password || !confirmPassword}
            >
              {loading ? 'Accepting...' : 'Accept Invite'}
            </button>
          </form>

          {error && <p className={styles.errorText} role="alert">{error}</p>}
          {success && <p className={styles.successText} role="status">{success}</p>}
        </section>
      </main>
    </div>
  )
}
