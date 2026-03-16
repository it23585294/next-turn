/**
 * WelcomePage — Public landing page for NextTurn.
 *
 * Audience: end users (people joining queues / booking appointments).
 * Organisation registration is intentionally de-emphasised — it lives in
 * a small dedicated section at the very bottom of the page.
 *
 * Sections:
 *  1. Navbar
 *  2. Hero  (with join-queue widget)
 *  3. How It Works (3 steps)
 *  4. Feature cards (4)
 *  5. Call-to-action banner
 *  6. For Organisations (subtle)
 *  7. Footer
 */
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import logoImg from '../../assets/nextTurn-logo.png'
import { isAuthenticated } from '../../utils/authGuard'
import { getTokenPayload } from '../../utils/authToken'
import styles from './WelcomePage.module.css'

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
        <ForOrgsSection />
      </main>
      <Footer />
    </div>
  )
}

/* ============================================================
   Navbar
   ============================================================ */
function Navbar() {
  const loggedIn = isAuthenticated()
  const payload  = loggedIn ? getTokenPayload() : null
  const isAdmin  = payload?.role === 'OrgAdmin' || payload?.role === 'SystemAdmin'
  const dashHref = payload
    ? (isAdmin ? `/admin/${payload.tid}` : `/dashboard/${payload.tid}`)
    : null

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
          {loggedIn && dashHref ? (
            <Link to={dashHref} className={styles.forOrgsLink}>
              My Dashboard
            </Link>
          ) : (
            <Link to="/login" className={styles.forOrgsLink}>
              Sign In
            </Link>
          )}
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
    <section id="hero" className={styles.hero}>
      {/* Background decoration */}
      <div className={styles.heroBlob1} aria-hidden="true" />
      <div className={styles.heroBlob2} aria-hidden="true" />

      <div className={styles.heroInner}>
        <div className={styles.heroText}>
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
              to="/register"
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

        <div className={styles.heroVisual}>
          <JoinQueueWidget />
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
          { value: '100% free',  label: 'No cost to join a queue' },
          { value: 'No install', label: 'Works in any browser' },
          { value: 'Real-time',  label: 'Live queue position updates' },
          { value: 'Any device', label: 'Mobile-first design' },
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
      desc: 'Sign up in under a minute with just your name and email. No credit card, no lengthy forms.',
    },
    {
      number: '03',
      icon: <TicketIcon />,
      title: 'Join the queue',
      desc: 'Tap to join a queue or book an appointment. Step away and do something useful, we\'ll notify you when it\'s your turn.',
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
                <div className={styles.stepConnector} aria-hidden="true">
                  <ChevronRightIcon />
                </div>
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
      accent: true,
    },
    {
      icon: <TicketIcon />,
      title: 'Wait from anywhere',
      desc: 'Join virtually and carry on with your day. Run errands, grab a coffee,  we\'ll bring you back when it\'s nearly your turn.',
      accent: true,
    },
    {
      icon: <CalendarIcon />,
      title: 'Book appointments too',
      desc: 'Prefer a set time? Book an appointment slot instead of joining a live queue, same easy experience, your choice of format.',
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
            Skip the wait, track your spot, and get served, without ever standing in a line.
          </p>
        </div>

        <div className={styles.featureGrid}>
          {features.map((f) => (
            <div key={f.title} className={`${styles.featureCard} ${f.accent ? styles.featureCardAccent : ''}`}>
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
            to="/register"
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
   For Organisations — subtle bottom section
   ============================================================ */
function ForOrgsSection() {
  return (
    <section className={styles.forOrgs} aria-labelledby="for-orgs-heading">
      <div className={styles.forOrgsInner}>
        <div className={styles.forOrgsIcon} aria-hidden="true">
          <BuildingIcon />
        </div>
        <div className={styles.forOrgsBody}>
          <p className={styles.forOrgsEyebrow}>For organisations</p>
          <h2 id="for-orgs-heading" className={styles.forOrgsTitle}>
            Offer queue &amp; appointment services to your clients
          </h2>
          <p className={styles.forOrgsDesc}>
            Register your organisation on NextTurn to manage service queues,
            appointment slots, and staff counters, all in one place. Your
            account is reviewed and activated by our team, usually within 24 hours.
          </p>
        </div>
        <div className={styles.forOrgsCta}>
          <div className={styles.forOrgsCtaStack}>
            <Link to="/register-org" className={styles.forOrgsLink}>
              Register your organisation
              <ArrowRightIcon />
            </Link>
            <Link to="/find-org-login" className={styles.forOrgsLinkMuted}>
              Already an admin? Find login link
            </Link>
          </div>
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
   Join Queue Widget
   ============================================================ */
function JoinQueueWidget() {
  const [value, setValue] = useState('')
  const navigate = useNavigate()

  function handleJoin(e: React.FormEvent) {
    e.preventDefault()
    const raw = value.trim()
    if (!raw) return

    try {
      // Accept full URLs — extract the pathname so we stay in-app
      const url = new URL(raw)
      navigate(url.pathname + url.search)
    } catch {
      // Treat as a relative path (e.g. pasted from a share card)
      navigate(raw.startsWith('/') ? raw : `/${raw}`)
    }
  }

  return (
    <div className={styles.joinWidget}>
      <p className={styles.joinWidgetLabel}>Have a queue link?</p>
      <h2 className={styles.joinWidgetTitle}>Join your queue instantly</h2>
      <p className={styles.joinWidgetHint}>
        Your organisation shared a link with you, paste it below and
        we'll take you straight there.
      </p>
      <form className={styles.joinForm} onSubmit={handleJoin} noValidate>
        <div className={styles.joinInputWrap}>
          <LinkIcon />
          <input
            type="url"
            className={styles.joinInput}
            placeholder="Paste your queue link here…"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            aria-label="Queue link"
            autoComplete="off"
            spellCheck={false}
          />
        </div>
        <button type="submit" className={styles.joinBtn} disabled={!value.trim()}>
          Join Queue
          <ArrowRightIcon />
        </button>
      </form>
      <p className={styles.joinWidgetSub}>
        Don't have a link yet?{' '}
        <a href="#how-it-works" className={styles.joinWidgetSubLink}>
          See how it works
        </a>
      </p>
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
function CalendarIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2"/>
      <line x1="16" y1="2" x2="16" y2="6"/>
      <line x1="8"  y1="2" x2="8"  y2="6"/>
      <line x1="3"  y1="10" x2="21" y2="10"/>
    </svg>
  )
}
function ChevronRightIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="9 18 15 12 9 6"/>
    </svg>
  )
}
function BuildingIcon() {
  return (
    <svg width="28" height="28" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"
      aria-hidden="true">
      <rect x="3" y="3" width="18" height="18" rx="2"/>
      <path d="M3 9h18"/>
      <path d="M9 21V9"/>
    </svg>
  )
}
function LinkIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      aria-hidden="true">
      <path d="M10 13a5 5 0 007.54.54l3-3a5 5 0 00-7.07-7.07l-1.72 1.71"/>
      <path d="M14 11a5 5 0 00-7.54-.54l-3 3a5 5 0 007.07 7.07l1.71-1.71"/>
    </svg>
  )
}
