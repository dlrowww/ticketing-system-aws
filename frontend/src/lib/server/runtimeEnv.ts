import { env } from '$env/dynamic/private';

const DEVELOPMENT_BACKEND_URL = 'http://localhost:5192';

export function getBackendUrl(): string {
	const configuredUrl = env.BACKEND_URL || env.LOOKUPS_API;

	if (!configuredUrl) {
		if (env.NODE_ENV === 'production') {
			throw new Error('BACKEND_URL is required in production');
		}

		return DEVELOPMENT_BACKEND_URL;
	}

	return configuredUrl.replace(/\/+$/, '');
}

export function getJwtSecret(): string {
	const jwtSecret = env.JWT_SECRET;

	if (!jwtSecret) {
		throw new Error('JWT_SECRET is required');
	}

	if (jwtSecret.length < 64) {
		throw new Error('JWT_SECRET must contain at least 64 characters');
	}

	return jwtSecret;
}
