// Spike técnico del módulo de Control de Vendedores: envoltorio sobre la Geolocation API del navegador,
// invocado desde Blazor Server vía JS interop. Pensado para probarse dentro del webview de Teams Mobile.
window.biomundoGeo = {
    getPosition: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error("Este navegador/webview no soporta geolocalización (navigator.geolocation no existe)."));
                return;
            }
            navigator.geolocation.getCurrentPosition(
                pos => resolve({
                    latitude: pos.coords.latitude,
                    longitude: pos.coords.longitude,
                    accuracy: pos.coords.accuracy,
                    timestamp: pos.timestamp
                }),
                err => reject(new Error(`${err.message} (código ${err.code}: 1=permiso denegado, 2=posición no disponible, 3=timeout)`)),
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
            );
        });
    }
};
