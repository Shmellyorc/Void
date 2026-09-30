(() => {
  const script = document.currentScript;
  const siteRoot = script
    ? new URL("../../", script.src)
    : new URL("/", window.location.href);
  const urlFor = (path = "") => new URL(path, siteRoot).href;

  const header = document.querySelector('.site-header');
  const menuButton = document.querySelector('.menu-button');
  const navLinks = document.querySelector('.nav-links');
  const hero = document.querySelector('.hero-atmosphere');
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  const onScroll = () => {
    if (header) header.classList.toggle('scrolled', window.scrollY > 16);
    if (hero && !reduceMotion) {
      const fade = Math.max(0, 1 - window.scrollY / 620);
      hero.style.setProperty('--hero-fade', fade.toFixed(3));
    }
  };

  onScroll();
  window.addEventListener('scroll', onScroll, { passive: true });

  if (menuButton && navLinks) {
    menuButton.addEventListener('click', () => {
      const open = navLinks.classList.toggle('open');
      menuButton.setAttribute('aria-expanded', String(open));
    });
  }

  const scrollingElement = document.scrollingElement || document.documentElement;
  const scrollCue = document.createElement('button');
  scrollCue.type = 'button';
  scrollCue.className = 'page-scroll-cue';
  scrollCue.setAttribute('aria-label', 'More content below');
  scrollCue.title = 'More content below';
  document.body.appendChild(scrollCue);

  const updateScrollCue = () => {
    const pageHeight = scrollingElement.scrollHeight;
    const viewportBottom = window.scrollY + window.innerHeight;
    const hasMore = pageHeight > window.innerHeight + 140 && viewportBottom < pageHeight - 90;
    scrollCue.classList.toggle('visible', hasMore);
  };

  scrollCue.addEventListener('click', () => {
    window.scrollBy({
      top: Math.max(320, window.innerHeight * 0.72),
      behavior: reduceMotion ? 'auto' : 'smooth'
    });
  });

  updateScrollCue();
  window.addEventListener('scroll', updateScrollCue, { passive: true });
  window.addEventListener('resize', updateScrollCue, { passive: true });
  window.addEventListener('load', updateScrollCue, { once: true });

  document.querySelectorAll('[data-copy]').forEach((button) => {
    button.addEventListener('click', async () => {
      const target = document.getElementById(button.dataset.copy);
      if (!target) return;
      try {
        await navigator.clipboard.writeText(target.textContent.trim());
        const old = button.textContent;
        button.textContent = 'Copied';
        setTimeout(() => { button.textContent = old; }, 1200);
      } catch {
        button.textContent = 'Select & copy';
      }
    });
  });

  document.querySelectorAll('.footer-links').forEach((footer) => {
    if (footer.querySelector('[data-privacy-link]')) return;

    const privacy = document.createElement('a');
    privacy.href = urlFor('privacy/');
    privacy.textContent = 'Privacy';
    privacy.dataset.privacyLink = '';
    footer.appendChild(privacy);
  });

  if (script && !document.querySelector('script[data-void-preferences]')) {
    const preferences = document.createElement('script');
    preferences.src = new URL('preferences.js?v=2', script.src).href;
    preferences.dataset.voidPreferences = '';
    document.head.appendChild(preferences);
  }
})();
