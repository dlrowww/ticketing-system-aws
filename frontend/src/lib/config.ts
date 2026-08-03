// Read PUBLIC_* at runtime so Kubernetes-injected values are not baked into the image.
import { env } from '$env/dynamic/public';

// Export typed constants for app-wide use.
export const API_BASE = env.PUBLIC_API_BASE || '/api';
export const DEFAULT_LOCALE = env.PUBLIC_DEFAULT_LOCALE || 'en-US';
