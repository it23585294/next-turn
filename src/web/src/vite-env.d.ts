/// <reference types="vite/client" />

// VITE_API_URL: set to the full Azure API base URL in production (e.g. https://<app>.azurewebsites.net/api)
// Undefined in development — falls back to '/api' via the Vite dev-server proxy.

interface ImportMetaEnv {
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
