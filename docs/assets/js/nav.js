(() => {
  "use strict";

  const script = document.currentScript;
  const mount = document.getElementById("site-nav");

  if (!script || !mount) return;

  // nav.js always lives at assets/js/nav.js.
  // ../../ resolves to the real site root on both:
  //   http://localhost:8000/
  //   https://voidengine.net/
  const siteRoot = new URL("../../", script.src);
  const urlFor = (path = "") => new URL(path, siteRoot).href;

  const rootPath = siteRoot.pathname.endsWith("/")
    ? siteRoot.pathname
    : `${siteRoot.pathname}/`;

  let pagePath = window.location.pathname;

  if (pagePath.startsWith(rootPath)) {
    pagePath = pagePath.slice(rootPath.length);
  } else {
    pagePath = pagePath.replace(/^\/+/, "");
  }

  pagePath = pagePath.replace(/^\/+|\/+$/g, "");
  const currentPage = pagePath.split("/")[0] || "home";

  const current = (page) =>
    currentPage === page ? ' aria-current="page"' : "";

  mount.outerHTML = `
<header class="site-header">
  <div class="container nav">
    <a class="brand" href="${urlFor()}" aria-label="VOID home">
      <img src="${urlFor("assets/images/void-logo.png")}" alt="VOID">
    </a>

    <button
      class="menu-button"
      type="button"
      aria-label="Open navigation"
      aria-expanded="false"
    >☰</button>

    <nav class="nav-links" aria-label="Main navigation">
      <a href="${urlFor()}"${current("home")}>Home</a>
      <a href="${urlFor("features/")}"${current("features")}>Features</a>
      <a href="${urlFor("philosophy/")}"${current("philosophy")}>Philosophy</a>
      <a href="${urlFor("rendering/")}"${current("rendering")}>Rendering</a>
      <a href="${urlFor("articles/")}"${current("articles")}>Articles</a>
      <a href="https://github.com/Shmellyorc/Void/wiki">Docs</a>
      <a href="https://github.com/Shmellyorc/Void">GitHub</a>
      <a class="nav-install" href="${urlFor("install/")}"${current("install")}>Install</a>
    </nav>
  </div>
</header>`;
})();
