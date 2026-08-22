// Kurulabilir uygulama için service worker kaydı. Hata yutulur: kayıt başarısız olsa da site normal çalışır.
if ('serviceWorker' in navigator) {
  window.addEventListener('load', function () {
    navigator.serviceWorker.register('/sw.js', { updateViaCache: 'none' }).catch(function () {});
  });
}
