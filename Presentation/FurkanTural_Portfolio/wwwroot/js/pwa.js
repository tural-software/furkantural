// Service worker kaydı (kurulabilir PWA). Hata sessizce yutulur.
if ('serviceWorker' in navigator) {
  window.addEventListener('load', function () {
    navigator.serviceWorker.register('/sw.js', { updateViaCache: 'none' }).catch(function () {});
  });
}
