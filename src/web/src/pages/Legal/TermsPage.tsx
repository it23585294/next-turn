import { Link } from 'react-router-dom'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './LegalPage.module.css'

export function TermsPage() {
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
          <h1 className={styles.title}>Terms of Service</h1>
          <p className={styles.subtitle}>
            Please read these terms carefully before using NextTurn.
          </p>
          <p className={styles.dates}>
            Effective date: 1 January 2026 &nbsp;·&nbsp; Last updated: 6 March 2026
          </p>
        </div>

        <Section title="1. Acceptance of Terms">
          <p>
            By creating an account or using any part of the NextTurn platform
            ("Service"), you agree to be bound by these Terms of Service ("Terms").
            If you do not agree to these Terms, you may not use the Service.
          </p>
          <p>
            NextTurn reserves the right to update these Terms at any time. Continued
            use of the Service after changes are posted constitutes your acceptance
            of the updated Terms.
          </p>
        </Section>

        <Section title="2. Description of Service">
          <p>
            NextTurn is a queue and appointment management platform that allows
            organisations ("Organisations") to manage service queues, and allows
            end-users ("Users") to join and track their place in those queues
            remotely.
          </p>
          <p>
            The Service is provided "as is." Features, pricing, and availability
            may change at any time with reasonable notice.
          </p>
        </Section>

        <Section title="3. Account Registration">
          <p>
            To use the Service, you must register for an account. You agree to:
          </p>
          <ul>
            <li>Provide accurate, current, and complete information during registration.</li>
            <li>Keep your password confidential and not share it with others.</li>
            <li>Notify us immediately if you suspect unauthorised access to your account.</li>
            <li>Be responsible for all activity that occurs under your account.</li>
          </ul>
          <p>
            You must be at least 16 years of age to create an account. Accounts
            created on behalf of organisations may have additional requirements
            communicated separately.
          </p>
        </Section>

        <Section title="4. Acceptable Use">
          <p>You agree not to use the Service to:</p>
          <ul>
            <li>Violate any applicable law or regulation.</li>
            <li>Harass, abuse, or harm other users or organisation staff.</li>
            <li>Attempt to gain unauthorised access to any part of the platform.</li>
            <li>Submit false, misleading, or fraudulent information.</li>
            <li>Use automated scripts or bots to join queues on behalf of others
                without explicit permission from the Organisation.</li>
            <li>Interfere with the normal operation of the Service.</li>
          </ul>
          <p>
            NextTurn reserves the right to suspend or terminate accounts that
            violate these rules without prior notice.
          </p>
        </Section>

        <Section title="5. Queue Conduct">
          <p>
            When you join a queue, you agree to be present and ready when your
            ticket is called. Repeated no-shows may result in restrictions on your
            ability to join queues at the relevant Organisation.
          </p>
          <p>
            Organisations are responsible for managing their own queues and may
            set their own conduct policies, which you agree to follow when using
            their services through NextTurn.
          </p>
        </Section>

        <Section title="6. Intellectual Property">
          <p>
            The NextTurn name, logo, software, and all associated content are the
            intellectual property of NextTurn and its licensors. You may not copy,
            modify, distribute, or create derivative works without explicit written
            permission.
          </p>
        </Section>

        <Section title="7. Limitation of Liability">
          <p>
            To the fullest extent permitted by law, NextTurn shall not be liable
            for any indirect, incidental, special, consequential, or punitive damages
            arising from your use of the Service, including but not limited to:
          </p>
          <ul>
            <li>Loss of time resulting from queue delays or system downtime.</li>
            <li>Errors in estimated wait times.</li>
            <li>Actions taken by Organisations using the platform.</li>
          </ul>
        </Section>

        <Section title="8. Termination">
          <p>
            You may delete your account at any time by contacting us. NextTurn
            may terminate or suspend access to the Service immediately, without
            prior notice, for conduct that we believe violates these Terms or is
            harmful to other users, Organisations, or the platform.
          </p>
        </Section>

        <Section title="9. Governing Law">
          <p>
            These Terms are governed by the laws of the Republic of the Philippines.
            Any disputes arising from these Terms shall be subject to the exclusive
            jurisdiction of the courts located in Metro Manila.
          </p>
        </Section>

        <div className={styles.contact}>
          <p>
            Questions about these Terms? Contact us at{' '}
            <a href="mailto:legal@nextturn.app">legal@nextturn.app</a>.
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
