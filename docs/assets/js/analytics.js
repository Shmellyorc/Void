(() => {
  "use strict";

  if (window.__voidAnalyticsBootstrapped) return;
  window.__voidAnalyticsBootstrapped = true;

  const MeasurementId = "G-Q17H2TPKEV";
  const ConsentKey = "void-analytics-consent";
  const script = document.currentScript;
  const siteRoot = script
    ? new URL("../../", script.src)
    : new URL("/", window.location.href);
  const privacyUrl = new URL("privacy/", siteRoot).href;
  const host = window.location.hostname.toLowerCase();
  const isProduction = host === "voidengine.net" || host === "www.voidengine.net";

  const readConsent = () => {
    try {
      return window.localStorage.getItem(ConsentKey);
    } catch {
      return null;
    }
  };

  const writeConsent = (value) => {
    try {
      if (value === null)
        window.localStorage.removeItem(ConsentKey);
      else
        window.localStorage.setItem(ConsentKey, value);
    } catch {
      // If storage is unavailable, the current choice still applies to this page.
    }
  };

  const clearAnalyticsCookies = () => {
    document.cookie.split(';').forEach((item) => {
      const name = item.split('=')[0].trim();
      if (name !== '_ga' && !name.startsWith('_ga_')) return;

      document.cookie = `${name}=; Max-Age=0; path=/`;
      document.cookie = `${name}=; Max-Age=0; path=/; domain=.voidengine.net`;
      document.cookie = `${name}=; Max-Age=0; path=/; domain=voidengine.net`;
    });
  };

  const loadAnalytics = () => {
    if (!isProduction || window[`ga-disable-${MeasurementId}`]) return;
    if (document.querySelector(`script[data-ga4="${MeasurementId}"]`)) return;

    window.dataLayer = window.dataLayer || [];
    window.gtag = window.gtag || function () {
      window.dataLayer.push(arguments);
    };

    window.gtag('js', new Date());
    window.gtag('config', MeasurementId, {
      allow_google_signals: false,
      allow_ad_personalization_signals: false
    });

    const ga = document.createElement('script');
    ga.async = true;
    ga.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(MeasurementId)}`;
    ga.dataset.ga4 = MeasurementId;
    document.head.appendChild(ga);
  };

  const addStyles = () => {
    if (document.getElementById('void-analytics-styles')) return;

    const style = document.createElement('style');
    style.id = 'void-analytics-styles';
    style.textContent = `
      .analytics-consent {
        position: fixed;
        z-index: 1000;
        left: 20px;
        right: 20px;
        bottom: 20px;
        width: min(calc(100% - 40px), 760px);
        margin-inline: auto;
        padding: 18px;
        border: 1px solid rgba(181, 145, 232, .26);
        border-radius: 8px;
        background: rgba(13, 11, 17, .98);
        box-shadow: 0 18px 60px rgba(0, 0, 0, .42);
        color: #f3eff8;
        font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      }

      .analytics-consent p {
        margin: 0;
        color: #aaa3b4;
        line-height: 1.55;
        font-size: .94rem;
      }

      .analytics-consent a {
        color: #c9a9ef;
      }

      .analytics-consent-actions {
        display: flex;
        gap: 10px;
        flex-wrap: wrap;
        margin-top: 14px;
      }

      .analytics-consent button {
        min-height: 40px;
        padding: 8px 13px;
        border-radius: 6px;
        border: 1px solid rgba(181, 145, 232, .26);
        background: rgba(255, 255, 255, .025);
        color: #eee8f5;
        font: 700 .9rem/1 Inter, ui-sans-serif, system-ui, sans-serif;
        cursor: pointer;
      }

      .analytics-consent button[data-consent="accept"] {
        border-color: rgba(191, 153, 241, .42);
        background: linear-gradient(180deg, #8d58cf, #7140aa);
        color: #fff;
      }

      .analytics-consent button:hover {
        border-color: rgba(183, 138, 237, .48);
      }

      .analytics-consent button:focus-visible {
        outline: 2px solid #b78aed;
        outline-offset: 3px;
      }

      @media (max-width: 640px) {
        .analytics-consent {
          left: 14px;
          right: 14px;
          bottom: 14px;
          width: calc(100% - 28px);
        }
      }
    `;
    document.head.appendChild(style);
  };

  const removePrompt = () => {
    document.getElementById('analytics-consent')?.remove();
  };

  const showPrompt = () => {
    if (document.getElementById('analytics-consent')) return;

    addStyles();

    const prompt = document.createElement('section');
    prompt.id = 'analytics-consent';
    prompt.className = 'analytics-consent';
    prompt.setAttribute('role', 'dialog');
    prompt.setAttribute('aria-label', 'Analytics preference');
    prompt.innerHTML = `
      <p>
        VOID uses Google Analytics to understand site traffic and improve the website.
        Analytics only loads if you accept. <a href="${privacyUrl}">Privacy details</a>.
      </p>
      <div class="analytics-consent-actions">
        <button type="button" data-consent="accept">Accept analytics</button>
        <button type="button" data-consent="decline">Decline</button>
      </div>
    `;

    prompt.querySelector('[data-consent="accept"]')?.addEventListener('click', () => {
      writeConsent('granted');
      removePrompt();
      loadAnalytics();
    });

    prompt.querySelector('[data-consent="decline"]')?.addEventListener('click', () => {
      writeConsent('denied');
      removePrompt();
    });

    document.body.appendChild(prompt);
  };

  document.querySelectorAll('[data-analytics-reset]').forEach((button) => {
    button.addEventListener('click', () => {
      writeConsent(null);
      window[`ga-disable-${MeasurementId}`] = true;
      clearAnalyticsCookies();
      window.location.reload();
    });
  });

  const consent = readConsent();

  if (consent === 'granted') {
    loadAnalytics();
  } else if (consent !== 'denied') {
    showPrompt();
  }
})();
