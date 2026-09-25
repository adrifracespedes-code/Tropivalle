(function () {
  'use strict';

  if ('serviceWorker' in navigator) {
    window.addEventListener('load', function () {
      navigator.serviceWorker.register('/sw.js').catch(function (err) {
        console.warn('SW register failed', err);
      });
    });
  }

  var deferredPrompt = null;
  var installBtn = null;

  window.addEventListener('beforeinstallprompt', function (e) {
    e.preventDefault();
    deferredPrompt = e;
    showInstallUi(true);
  });

  window.addEventListener('appinstalled', function () {
    deferredPrompt = null;
    showInstallUi(false);
  });

  function showInstallUi(show) {
    installBtn = document.getElementById('pwaInstallBtn');
    if (!installBtn) return;
    installBtn.style.display = show ? 'inline-flex' : 'none';
  }

  document.addEventListener('DOMContentLoaded', function () {
    installBtn = document.getElementById('pwaInstallBtn');
    if (!installBtn) return;
    installBtn.style.display = 'none';
    installBtn.addEventListener('click', async function () {
      if (!deferredPrompt) return;
      deferredPrompt.prompt();
      try {
        await deferredPrompt.userChoice;
      } catch (_) {}
      deferredPrompt = null;
      showInstallUi(false);
    });
  });
})();
