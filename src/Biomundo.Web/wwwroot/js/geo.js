// Spike técnico del módulo de Control de Vendedores.
//
// Dentro de una Tab de Teams, el iframe bloquea navigator.geolocation directo mediante una
// Permissions-Policy del propio host (confirmado en pruebas reales: "Geolocation has been disabled
// in this document by permissions policy"). Es un comportamiento deliberado de Microsoft: la ubicación
// hay que pedirla a través del SDK de Teams (@microsoft/teams-js), que se la solicita al host nativo
// (la app de Teams, no al webview), y el host nativo es quien maneja el permiso del sistema operativo.
//
// Por eso: si el SDK de Teams está disponible y soporta geolocalización, se usa esa vía. Si no
// (por ejemplo, abriendo esta página directo en un navegador normal, fuera de Teams, para desarrollo),
// cae de vuelta a navigator.geolocation estándar.
window.biomundoGeo = {
    _teamsInitialized: false,

    getPosition: async function () {
        if (window.microsoftTeams) {
            try {
                if (!this._teamsInitialized) {
                    await microsoftTeams.app.initialize();
                    this._teamsInitialized = true;
                }
                if (microsoftTeams.geoLocation && microsoftTeams.geoLocation.isSupported()) {
                    const location = await microsoftTeams.geoLocation.getCurrentLocation();
                    return {
                        latitude: location.latitude,
                        longitude: location.longitude,
                        accuracy: location.accuracy,
                        timestamp: Date.now(),
                        via: "teams-sdk"
                    };
                }
            } catch (teamsError) {
                throw new Error(`Error pidiendo ubicación vía SDK de Teams: ${teamsError.message || teamsError}`);
            }
        }

        // Fuera de Teams (navegador normal, para desarrollo): geolocalización HTML5 estándar.
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
