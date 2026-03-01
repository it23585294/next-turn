/**
 * WelcomePage — Public landing page for NextTurn.
 *
 * Sections:
 *  1. Navbar
 *  2. Hero
 *  3. Stats bar
 *  4. How It Works (3 steps)
 *  5. Feature cards (4)
 *  6. Call-to-action banner
 *  7. Footer
 */
import { Link } from 'react-router-dom'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './WelcomePage.module.css'

const DEMO_TENANT = '00000000-0000-0000-0000-000000000001'

export function WelcomePage() {
  return (
    <div className={styles.page}>
      <Navbar />
      <main>
        <HeroSection />
        <StatsBar />
        <HowItWorksSection />
        <FeaturesSection />
        <CtaSection />
      </main>
      <Footer />
    </div>
  )
}

/* ============================================================
   Navbar
   ============================================================ */
function Navbar() {
  return (
    <header className={styles.navbar}>
      <div className={styles.navInner}>
        <Link to="/" className={styles.navLogo}>
          <img src={logoImg} alt="NextTurn" className={styles.navLogoImg} />
        </Link>

        <nav className={styles.navLinks} aria-label="Main navigation">
          <a href="#how-it-works" className={styles.navLink}>How it works</a>
          <a href="#features" className={styles.navLink}>Features</a>
        </nav>

        <div className={styles.navActions}>
          <Link
            to={`/login/${DEMO_TENANT}`}
            className={styles.btnOutline}
            style={{ marginRight: 'var(--space-2)' }}
          >
            Sign In
          </Link>
          <Link
            to={`/register/${DEMO_TENANT}`}
            className={styles.btnOutline}
          >
            Get Started
          </Link>
        </div>
      </div>
    </header>
  )
}

/* ============================================================
   Hero
   ============================================================ */
function HeroSection() {
  return (
    <section className={styles.hero}>
      {/* Background decoration */}
      <div className={styles.heroBlob1} aria-hidden="true" />
      <div className={styles.heroBlob2} aria-hidden="true" />

      <div className={styles.heroInner}>
        <div className={styles.heroText}>
          <div className={styles.heroBadge}>
            <span className={styles.heroBadgeDot} aria-hidden="true" />
            Queue & Appointment Management
          </div>

          <h1 className={styles.heroHeading}>
            Skip the Wait.<br />
            <span className={styles.heroHeadingAccent}>Take Control.</span>
          </h1>

          <p className={styles.heroDesc}>
            NextTurn is the smarter way to manage queues and appointments.
            Join from your phone, get notified when it's your turn, and
            never waste another minute standing in line.
          </p>

          <div className={styles.heroCtas}>
            <Link
              to={`/register/${DEMO_TENANT}`}
              className={styles.btnPrimary}
            >
              Create Free Account
              <ArrowRightIcon />
            </Link>
            <a href="#how-it-works" className={styles.btnGhost}>
              See how it works
            </a>
          </div>
        </div>

        {/* Visual — Queue card mockup */}
        <div className={styles.heroVisual} aria-hidden="true">
          <QueueMockup />
        </div>
      </div>
    </section>
  )
}

/* ============================================================
   Stats bar
   ============================================================ */
function StatsBar() {
  return (
    <div className={styles.stats}>
      <div className={styles.statsInner}>
        {[
          { value: '10,000+', label: 'People served daily' },
          { value: '200+',    label: 'Organizations onboarded' },
          { value: '< 30s',   label: 'Average join time' },
          { value: '99.9%',   label: 'Uptime SLA' },
        ].map(({ value, label }) => (
          <div key={label} className={styles.statItem}>
            <span className={styles.statValue}>{value}</span>
            <span className={styles.statLabel}>{label}</span>
          </div>
        ))}
      </div>
    </div>
  )
}

/* ============================================================
   How It Works
   ============================================================ */
function HowItWorksSection() {
  const steps = [
    {
      number: '01',
      icon: <ScanIcon />,
      title: 'Find your organization',
      desc: 'Your organization gives you a unique link or QR code. Scan it or click it to reach their NextTurn portal.',
    },
    {
      number: '02',
      icon: <UserPlusIcon />,
      title: 'Create your account',
      desc: 'Register in under a minute. Your data stays isolated to your organization — no cross-tenant sharing, ever.',
    },
    {
      number: '03',
      icon: <TicketIcon />,
      title: 'Join the queue',
      desc: 'Tap to join a queue or book an appointment. Step away and do something useful — we\'ll notify you when it\'s your turn.',
    },
  ]

  return (
    <section id="how-it-works" className={styles.howItWorks}>
      <div className={styles.sectionInner}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionBadge}>Simple by design</span>
          <h2 className={styles.sectionTitle}>How it works</h2>
          <p className={styles.sectionDesc}>
            Three steps from registration to a confirmed spot in any queue.
          </p>
        </div>

        <div className={styles.steps}>
          {steps.map((step, i) => (
            <div key={step.number} className={styles.step}>
              <div className={styles.stepNumber}>{step.number}</div>
              <div className={styles.stepIconWrap}>{step.icon}</div>
              <h3 className={styles.stepTitle}>{step.title}</h3>
              <p className={styles.stepDesc}>{step.desc}</p>
              {i < steps.length - 1 && (
                <div className={styles.stepConnector} aria-hidden="true" />
              )}
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

/* ============================================================
   Features
   ============================================================ */
function FeaturesSection() {
  const features = [
    {
      icon: <SmartphoneIcon />,
      title: 'Mobile-first experience',
      desc: 'Built for phones first. Join queues, check wait times, and manage appointments from any device, anywhere.',
      accent: false,
    },
    {
      icon: <BellRingIcon />,
      title: 'Real-time notifications',
      desc: 'Get notified when your turn is approaching. No more anxiously watching a screen or losing your place.',
      accent: false,
    },
    {
      icon: <ShieldCheckIcon />,
      title: 'Tenant isolation',
      desc: 'Each organization\'s data is fully isolated. Your information never bleeds across organizational boundaries.',
      accent: true,
    },
    {
      icon: <ChartIcon />,
      title: 'Staff dashboards',
      desc: 'Counters and clerks get a live view of the queue, can serve, skip, or call queued customers with one tap.',
      accent: false,
    },
  ]

  return (
    <section id="features" className={styles.features}>
      <div className={styles.sectionInner}>
        <div className={styles.sectionHeader}>
          <span className={styles.sectionBadge}>What you get</span>
          <h2 className={styles.sectionTitle}>Everything you need</h2>
          <p className={styles.sectionDesc}>
            From citizens joining a queue to staff managing service counters —
            NextTurn handles every side of the experience.
          </p>
        </div>

        <div className={styles.featureGrid}>
          {features.map((f) => (
            <div
              key={f.title}
              className={`${styles.featureCard} ${f.accent ? styles.featureCardAccent : ''}`}
            >
              <div className={`${styles.featureIconWrap} ${f.accent ? styles.featureIconWrapAccent : ''}`}>
                {f.icon}
              </div>
              <h3 className={styles.featureTitle}>{f.title}</h3>
              <p className={styles.featureDesc}>{f.desc}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

/* ============================================================
   CTA Banner
   ============================================================ */
function CtaSection() {
  return (
    <section className={styles.cta}>
      <div className={styles.ctaInner}>
        <div className={styles.ctaBlob} aria-hidden="true" />
        <div className={styles.ctaContent}>
          <h2 className={styles.ctaTitle}>Ready to skip the wait?</h2>
          <p className={styles.ctaDesc}>
            Create your account in under a minute and join your first queue today.
          </p>
          <Link
            to={`/register/${DEMO_TENANT}`}
            className={styles.ctaBtn}
          >
            Get Started — It's Free
            <ArrowRightIcon />
          </Link>
        </div>
      </div>
    </section>
  )
}

/* ============================================================
   Footer
   ============================================================ */
function Footer() {
  return (
    <footer className={styles.footer}>
      <div className={styles.footerInner}>
        <div className={styles.footerBrand}>
          <img src={logoImg} alt="NextTurn" className={styles.footerLogo} />
          <p className={styles.footerTagline}>Skip the wait. Take control.</p>
        </div>
        <p className={styles.footerCopy}>
          © {new Date().getFullYear()} NextTurn. Capstone project — not for commercial use.
        </p>
      </div>
    </footer>
  )
}

/* ============================================================
   Queue Mockup card (decorative illustration)
   ============================================================ */
function QueueMockup() {
  const items = [
    { initials: 'MS', name: 'Maria Santos',  ticket: 'A-047', active: true  },
    { initials: 'JD', name: 'Juan Dela Cruz', ticket: 'A-048', active: false },
    { initials: 'AC', name: 'Ana Cruz',       ticket: 'A-049', active: false },
  ]
  return (
    <div className={styles.mockup}>
      <div className={styles.mockupHeader}>
        <span className={styles.mockupDot} style={{ background: '#ff5f57' }} />
        <span className={styles.mockupDot} style={{ background: '#ffbd2e' }} />
        <span className={styles.mockupDot} style={{ background: '#28c940' }} />
        <span className={styles.mockupTitle}>Live Queue — Window 3</span>
      </div>
      <div className={styles.mockupBody}>
        <div className={styles.mockupNowServing}>
          <span className={styles.mockupNowLabel}>NOW SERVING</span>
          <span className={styles.mockupNowTicket}>A-046</span>
        </div>
        <div className={styles.mockupQueue}>
          {items.map((item) => (
            <div
              key={item.ticket}
              className={`${styles.mockupItem} ${item.active ? styles.mockupItemActive : ''}`}
            >
              <div className={styles.mockupAvatar}>{item.initials}</div>
              <div className={styles.mockupItemInfo}>
                <span className={styles.mockupItemName}>{item.name}</span>
                <span className={styles.mockupItemTicket}>{item.ticket}</span>
              </div>
              {item.active && (
                <div className={styles.mockupBadge}>
                  <span className={styles.mockupBadgePulse} />
                  Next
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

/* ============================================================
   Inline SVG icons
   ============================================================ */
function ArrowRightIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"
      aria-hidden="true">
      <line x1="5" y1="12" x2="19" y2="12"/>
      <polyline points="12 5 19 12 12 19"/>
    </svg>
  )
}
function ScanIcon() {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 7V5a2 2 0 012-2h2M17 3h2a2 2 0 012 2v2M21 17v2a2 2 0 01-2 2h-2M7 21H5a2 2 0 01-2-2v-2"/>
      <rect x="7" y="7" width="10" height="10" rx="1"/>
    </svg>
  )
}
function UserPlusIcon() {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M16 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2"/>
      <circle cx="9" cy="7" r="4"/>
      <line x1="19" y1="8" x2="19" y2="14"/>
      <line x1="22" y1="11" x2="16" y2="11"/>
    </svg>
  )
}
function TicketIcon() {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2 9a3 3 0 010-6h20a3 3 0 010 6"/>
      <path d="M2 15a3 3 0 000 6h20a3 3 0 000-6"/>
      <line x1="2" y1="12" x2="22" y2="12"/>
    </svg>
  )
}
function SmartphoneIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="5" y="2" width="14" height="20" rx="2" ry="2"/>
      <line x1="12" y1="18" x2="12.01" y2="18"/>
    </svg>
  )
}
function BellRingIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
      <path d="M13.73 21a2 2 0 01-3.46 0"/>
      <path d="M21 5a9.05 9.05 0 00-2.56-3"/>
      <path d="M3 5a9.05 9.05 0 012.56-3"/>
    </svg>
  )
}
function ShieldCheckIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
      <polyline points="9 12 11 14 15 10"/>
    </svg>
  )
}
function ChartIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <line x1="18" y1="20" x2="18" y2="10"/>
      <line x1="12" y1="20" x2="12" y2="4"/>
      <line x1="6"  y1="20" x2="6"  y2="14"/>
    </svg>
  )
}
