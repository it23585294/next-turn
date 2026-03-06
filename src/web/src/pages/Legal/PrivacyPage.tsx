import { Link } from 'react-router-dom'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './LegalPage.module.css'

export function PrivacyPage() {
  return (
    <div className={styles.page}>
      <nav className={styles.nav}>
        <div className={styles.navInner}>
          <Link to="/" className={styles.backLink}>
            <BackIcon /> Back to NextTurn
          </Link>
          <img src={logoImg} alt="NextTurn" className={styles.logoImg} />
        </div>
      </nav>

      <main className={styles.content}>
        <div className={styles.meta}>
          <span className={styles.badge}>Legal</span>
          <h1 className={styles.title}>Privacy Policy</h1>
          <p className={styles.subtitle}>
            We take your privacy seriously. Here's exactly what we collect and why.
          </p>
          <p className={styles.dates}>
            Effective date: 1 January 2026 &nbsp;·&nbsp; Last updated: 6 March 2026
          </p>
        </div>

        <Section title="1. Who We Are">
          <p>
            NextTurn ("we", "us", "our") operates the NextTurn queue and appointment
            management platform accessible at nextturn.app. This Privacy Policy
            explains how we collect, use, and protect your personal information when
            you use our Service.
          </p>
        </Section>

        <Section title="2. Information We Collect">
          <p>
            <strong>Account information</strong> — When you register, we collect your
            full name, email address, and optionally your phone number. This is used to
            identify you and contact you about your queue status.
          </p>
          <p>
            <strong>Queue activity</strong> — We record the queues you join, your
            ticket numbers, timestamps, and entry status (waiting, serving, completed).
            This data is used to operate the Service and display your queue history.
          </p>
          <p>
            <strong>Authentication data</strong> — Passwords are stored as
            one-way BCrypt hashes. We never store or transmit your plaintext password.
            Failed login attempts and temporary lockout timestamps are recorded to
            protect your account.
          </p>
          <p>
            <strong>Technical data</strong> — We may log your IP address and
            HTTP request metadata for security monitoring, rate limiting, and debugging.
            This data is retained for up to 30 days.
          </p>
        </Section>

        <Section title="3. How We Use Your Information">
          <ul>
            <li>To create and manage your account.</li>
            <li>To show you real-time queue position and estimated wait times.</li>
            <li>To send queue notifications (when you have opted in).</li>
            <li>To enforce rate limits and protect against abuse.</li>
            <li>To comply with legal obligations.</li>
          </ul>
          <p>
            We do <strong>not</strong> sell your personal data to third parties.
            We do <strong>not</strong> use your data for advertising.
          </p>
        </Section>

        <Section title="4. Multi-Tenancy and Data Isolation">
          <p>
            NextTurn is a multi-tenant platform. Each Organisation has access only to
            the data within their own tenant. Organisation staff and admins cannot
            view data from other Organisations. Your personal account data (name,
            email, phone) is visible only to you and is not shared with
            Organisations whose queues you join, beyond what is necessary to
            process your place in the queue.
          </p>
        </Section>

        <Section title="5. Data Sharing">
          <p>
            We may share your information only in the following limited circumstances:
          </p>
          <ul>
            <li>
              <strong>Service providers</strong> — We use trusted third-party
              infrastructure providers (hosting, email delivery) who process data
              only on our behalf and under strict data processing agreements.
            </li>
            <li>
              <strong>Legal requirements</strong> — If required by law, court order,
              or government authority, we may disclose information as necessary.
            </li>
            <li>
              <strong>Business transfers</strong> — In the event of a merger or
              acquisition, your data may be transferred as part of that transaction.
              We will notify you before your data becomes subject to a different
              privacy policy.
            </li>
          </ul>
        </Section>

        <Section title="6. Data Retention">
          <p>
            We retain your account data for as long as your account is active.
            If you delete your account, your personal data is removed from active
            databases within 30 days, and from backups within 90 days.
          </p>
          <p>
            Anonymised, aggregated queue statistics (no personal identifiers) may be
            retained indefinitely for analytics purposes.
          </p>
        </Section>

        <Section title="7. Security">
          <p>
            We implement industry-standard security measures including:
          </p>
          <ul>
            <li>HTTPS encryption for all data in transit.</li>
            <li>BCrypt password hashing with a cost factor of 12.</li>
            <li>Account lockout after repeated failed login attempts.</li>
            <li>Rate limiting on authentication endpoints.</li>
            <li>Tenant-level data isolation enforced at the database query layer.</li>
          </ul>
          <p>
            No system is 100% secure. If you believe your account has been
            compromised, contact us immediately.
          </p>
        </Section>

        <Section title="8. Your Rights">
          <p>You have the right to:</p>
          <ul>
            <li><strong>Access</strong> — Request a copy of the personal data we hold about you.</li>
            <li><strong>Correction</strong> — Ask us to correct inaccurate or incomplete data.</li>
            <li><strong>Deletion</strong> — Request that we delete your account and personal data.</li>
            <li><strong>Portability</strong> — Request your data in a machine-readable format.</li>
            <li><strong>Objection</strong> — Object to certain types of processing where applicable.</li>
          </ul>
          <p>
            To exercise any of these rights, contact us at{' '}
            <a href="mailto:privacy@nextturn.app">privacy@nextturn.app</a>.
            We will respond within 30 days.
          </p>
        </Section>

        <Section title="9. Cookies">
          <p>
            NextTurn uses only a single browser storage mechanism:{' '}
            <strong>localStorage</strong> to persist your authentication token
            across page reloads. We do not use tracking cookies or third-party
            analytics cookies.
          </p>
        </Section>

        <Section title="10. Changes to This Policy">
          <p>
            We may update this Privacy Policy periodically. We will notify you of
            significant changes by email or by a notice on the platform. The
            "Last updated" date at the top of this page reflects the most recent
            revision.
          </p>
        </Section>

        <div className={styles.contact}>
          <p>
            Privacy questions or data requests? Contact our privacy team at{' '}
            <a href="mailto:privacy@nextturn.app">privacy@nextturn.app</a>.
          </p>
        </div>
      </main>
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className={styles.section}>
      <h2 className={styles.sectionTitle}>{title}</h2>
      <div className={styles.body}>{children}</div>
    </section>
  )
}

function BackIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="15 18 9 12 15 6" />
    </svg>
  )
}
