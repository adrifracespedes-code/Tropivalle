(function () {
  "use strict";

  const nav = document.querySelector(".navbar-tropivalle");
  if (nav) {
    const onScroll = () => {
      nav.classList.toggle("scrolled", window.scrollY > 40);
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();
  }

  document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
    anchor.addEventListener("click", function (e) {
      const id = this.getAttribute("href");
      if (!id || id.length < 2) return;
      const target = document.querySelector(id);
      if (!target) return;
      e.preventDefault();
      const top = target.getBoundingClientRect().top + window.pageYOffset - 80;
      window.scrollTo({ top, behavior: "smooth" });
    });
  });
})();

/* Nosotros: tarjeta grande que cambia sola */
(function () {
  var root = document.getElementById("nosotrosHeroCard");
  if (!root) return;
  if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

  var slides = Array.prototype.slice.call(root.querySelectorAll(".nosotros-slide"));
  var dots = Array.prototype.slice.call(root.querySelectorAll(".nosotros-dot"));
  if (slides.length < 2) return;

  var i = 0;
  var timer = null;
  var INTERVAL = 3200;

  function go(n) {
    if (n === i) return;
    var prev = slides[i];
    prev.classList.remove("is-active");
    prev.classList.add("is-exit");
    setTimeout(function () {
      prev.classList.remove("is-exit");
    }, 560);

    i = (n + slides.length) % slides.length;
    slides[i].classList.add("is-active");
    dots.forEach(function (d, idx) {
      d.classList.toggle("is-active", idx === i);
    });
  }

  function next() {
    go(i + 1);
  }

  function start() {
    stop();
    timer = setInterval(next, INTERVAL);
  }

  function stop() {
    if (timer) clearInterval(timer);
    timer = null;
  }

  dots.forEach(function (d) {
    d.addEventListener("click", function () {
      var n = parseInt(d.getAttribute("data-go"), 10);
      if (!isNaN(n)) {
        go(n);
        start();
      }
    });
  });

  root.addEventListener("mouseenter", stop);
  root.addEventListener("mouseleave", start);
  root.addEventListener("touchstart", stop, { passive: true });

  start();
})();



/* Inicio: paneles como ventanas al scrollear */
(function () {
  if (!document.body.classList.contains("page-home-panels")) return;
  var panels = document.querySelectorAll(".panel-window:not(.panel-hero)");
  if (!panels.length || !("IntersectionObserver" in window)) {
    panels.forEach(function (p) { p.classList.add("in-view"); });
    return;
  }
  var io = new IntersectionObserver(
    function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) e.target.classList.add("in-view");
      });
    },
    { threshold: 0.18, rootMargin: "0px 0px -8% 0px" }
  );
  panels.forEach(function (p) { io.observe(p); });
})();

/* Hero: solo colisión — la caída la hace el CSS */
(function () {
  var hero = document.querySelector(".hero-mock");
  var rain = document.querySelector(".hero-rain");
  var headline = document.getElementById("heroHeadline");
  if (!hero || !rain || !headline) return;
  if (hero.classList.contains("hero-mock--no-rain")) return;
  if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

  var drops = Array.prototype.slice.call(rain.querySelectorAll(".drop"));
  if (!drops.length) return;

  // cooldown por gota para no spamear splash
  var lastHit = new WeakMap();

  function splashAt(x, y) {
    var root = document.createElement("span");
    root.className = "hero-splash";
    root.style.left = x + "px";
    root.style.top = y + "px";

    var ring = document.createElement("span");
    ring.className = "rip";
    root.appendChild(ring);

    var n = 4 + Math.floor(Math.random() * 3);
    for (var i = 0; i < n; i++) {
      var b = document.createElement("span");
      b.className = "bead";
      var ang = (-80 + Math.random() * 160) * Math.PI / 180;
      var dist = 8 + Math.random() * 14;
      b.style.setProperty("--bx", (Math.cos(ang) * dist).toFixed(1) + "px");
      b.style.setProperty("--by", (Math.sin(ang) * dist - 4).toFixed(1) + "px");
      root.appendChild(b);
    }
    hero.appendChild(root);
    setTimeout(function () { root.remove(); }, 520);
  }

  function tick() {
    var hr = hero.getBoundingClientRect();
    var tr = headline.getBoundingClientRect();
    // caja del título en coords del viewport
    var tTop = tr.top;
    var tBot = tr.bottom;
    var tLeft = tr.left;
    var tRight = tr.right;
    var now = performance.now();

    for (var i = 0; i < drops.length; i++) {
      var d = drops[i];
      if (d.style.display === "none") continue;
      var r = d.getBoundingClientRect();
      // centro de la gota
      var cx = r.left + r.width / 2;
      var cy = r.top + r.height / 2;
      if (cy < tTop || cy > tBot || cx < tLeft || cx > tRight) continue;

      var prev = lastHit.get(d) || 0;
      if (now - prev < 900) continue; // una vez por caída aprox.
      lastHit.set(d, now);

      // splash relativo al hero
      splashAt(cx - hr.left, Math.min(cy, tTop + 12) - hr.top);

      // ocultar gota un instante (parece que se deshizo)
      d.classList.add("is-hit");
      setTimeout(function (el) {
        el.classList.remove("is-hit");
      }, 400, d);
    }
    requestAnimationFrame(tick);
  }

  requestAnimationFrame(tick);
})();

