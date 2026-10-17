// Spike técnico del módulo de Control de Vendedores.
//
// Dentro de una Tab de Teams, el iframe bloquea navigator.geolocation directo mediante una
// Permissions-Policy del propio host (confirmado en pruebas reales: "Geolocation has been disabled
// in this document by permissions policy"). Es un comportamiento deliberado de Microsoft: la ubicación
// hay que pedirla a través del SDK de Teams (@microsoft/teams-js), que se la solicita al host nativo
// (la app de Teams, no al webview), y el host nativo es quien maneja el permiso del sistema operativo.
//
// IMPORTANTE (confirmado en pruebas reales): el SDK de Teams lanza "The library has not yet been
// initialized" si se llama a CUALQUIER otra función (incluido geoLocation.isSupported()) antes de que
// microsoftTeams.app.initialize() haya terminado. Por eso toda llamada pasa primero por
// _ensureTeamsInitialized(), que inicializa una sola vez (cachea la promesa) y nunca más de una vez.
window.biomundoGeo = {
    _teamsInitPromise: null,

    _ensureTeamsInitialized: function () {
        if (typeof window.microsoftTeams === "undefined") {
            return Promise.resolve(false); // SDK no cargado (fuera de Teams, o falló la carga del script)
        }
        if (!this._teamsInitPromise) {
            this._teamsInitPromise = microsoftTeams.app.initialize()
                .then(() => true)
                .catch(e => {
                    console.error("Error inicializando el SDK de Teams:", e);
                    return false;
                });
        }
        return this._teamsInitPromise;
    },

    // Diagnóstico expuesto para depurar sin consola del navegador (difícil de abrir dentro de Teams).
    // Devuelve un string ya serializado (JSON.stringify), no un objeto, para no depender de cómo
    // Blazor interprete la serialización automática de JS interop.
    diagnostico: async function () {
        try {
            const sdkCargado = typeof window.microsoftTeams !== "undefined";
            const inicializado = sdkCargado ? await this._ensureTeamsInitialized() : false;
            const soportado = inicializado && !!(window.microsoftTeams.geoLocation
                && window.microsoftTeams.geoLocation.isSupported
                && window.microsoftTeams.geoLocation.isSupported());
            return JSON.stringify({
                sdkTeamsCargado: sdkCargado,
                teamsInicializado: inicializado,
                geoLocationSoportado: soportado,
                userAgent: navigator.userAgent
            });
        } catch (e) {
            return "ERROR en diagnostico(): " + (e.message || e);
        }
    },

    getPosition: async function () {
        const sdkCargado = typeof window.microsoftTeams !== "undefined";

        if (sdkCargado) {
            const inicializado = await this._ensureTeamsInitialized();
            if (inicializado && microsoftTeams.geoLocation && microsoftTeams.geoLocation.isSupported && microsoftTeams.geoLocation.isSupported()) {
                try {
                    const location = await microsoftTeams.geoLocation.getCurrentLocation();
                    return {
                        latitude: location.latitude,
                        longitude: location.longitude,
                        accuracy: location.accuracy,
                        timestamp: Date.now(),
                        via: "teams-sdk"
                    };
                } catch (teamsError) {
                    throw new Error(`Error pidiendo ubicación vía SDK de Teams: ${teamsError.message || teamsError}`);
                }
            }
        }

        // Fuera de Teams, o el SDK no cargó/no soporta geoLocation: geolocalización HTML5 estándar.
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
                    timestamp: pos.timestamp,
                    via: "navigator.geolocation"
                }),
                err => reject(new Error(`${err.message} (código ${err.code}: 1=permiso denegado, 2=posición no disponible, 3=timeout)`)),
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
            );
        });
    }
};
