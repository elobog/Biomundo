// Service worker mínimo, solo para que el navegador considere la app instalable como PWA
// (requisito de Chrome/Android; iOS no lo exige, pero no está de más).
//
// Blazor Server no tiene un "app shell" estático útil para cachear offline (toda la UI se renderiza
// en el servidor vía SignalR); por eso no hay estrategia de caché todavía. Si más adelante se necesita
// soporte offline real, es acá donde se agregaría.
self.addEventListener("install", () => self.skipWaiting());
self.addEventListener("activate", event => event.waitUntil(self.clients.claim()));
self.addEventListener("fetch", () => {
    // Sin caché por ahora: deja pasar todas las peticiones a la red tal cual.
});
