(() => {
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
})();
