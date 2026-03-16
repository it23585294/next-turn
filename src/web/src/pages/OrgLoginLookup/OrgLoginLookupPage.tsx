import { useState } from 'react'
import { Link } from 'react-router-dom'
import { resolveOrganisationLogin } from '../../api/organisations'
import type { ApiError } from '../../types/api'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './OrgLoginLookupPage.module.css'

export function OrgLoginLookupPage() {
  const [adminEmail, setAdminEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<{ organisationName: string; loginPath: string } | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!adminEmail.trim()) return

    setLoading(true)
    setError(null)
    setResult(null)

    try {
      const resolved = await resolveOrganisationLogin(adminEmail.trim())
      setResult({
        organisationName: resolved.organisationName,
        loginPath: resolved.loginPath,
      })
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not resolve an organisation login link for this email.')
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
          <h1 className={styles.title}>Find Organisation Login</h1>
          <p className={styles.subtitle}>
            Enter your organisation admin email to get your workspace login link.
          </p>

          <form onSubmit={handleSubmit} className={styles.form} noValidate>
            <label className={styles.label} htmlFor="admin-email">Admin email</label>
            <input
              id="admin-email"
              className={styles.input}
              type="email"
              value={adminEmail}
              onChange={e => setAdminEmail(e.target.value)}
              placeholder="admin@yourorg.com"
              autoComplete="email"
            />

            <button
              type="submit"
              className={styles.submitBtn}
              disabled={loading || !adminEmail.trim()}
            >
              {loading ? 'Finding...' : 'Find Login Link'}
            </button>
          </form>

          {error && (
            <p className={styles.errorText} role="alert">{error}</p>
          )}

          {result && (
            <div className={styles.successBox} role="status">
              <p>
                <strong>{result.organisationName}</strong>
              </p>
              <p>
                Login URL: <strong>{window.location.origin}{result.loginPath}</strong>
              </p>
              <Link to={result.loginPath} className={styles.loginBtn}>
                Go to Organisation Login
              </Link>
            </div>
          )}

          <p className={styles.helpText}>
            If you still cannot access your account, contact your organisation owner.
          </p>
        </section>
      </main>
    </div>
  )
}
