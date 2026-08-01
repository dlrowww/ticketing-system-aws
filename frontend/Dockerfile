FROM node:20-alpine AS build
WORKDIR /app

# Copy package files first for better caching
COPY frontend/package.json frontend/pnpm-lock.yaml ./
RUN npm install -g pnpm && pnpm install --frozen-lockfile

# Copy source files
COPY frontend/svelte.config.js frontend/vite.config.ts frontend/tsconfig.json ./
COPY frontend/src ./src
COPY frontend/static ./static

# Set build environment variables
ENV CI=true
ENV LOOKUPS_API=http://api:8080
ENV JWT_SECRET=dev-local-key-64-chars-minimum-for-testing-purposes-only-1234567
ENV PUBLIC_API_BASE=/api
ENV PUBLIC_DEFAULT_LOCALE=en-US
ENV BACKEND_URL=http://api:8080

# Build the application
RUN pnpm run build

# Production stage
FROM node:20-alpine
WORKDIR /app

# Copy package files
COPY --from=build /app/package.json /app/pnpm-lock.yaml ./

# Install pnpm and production dependencies
RUN npm install -g pnpm && pnpm install --prod --frozen-lockfile

# Copy built app (SvelteKit adapter-node output)
COPY --from=build /app/build ./build

EXPOSE 3000

# Set runtime environment variables
ENV PORT=3000
ENV HOST=0.0.0.0
ENV NODE_ENV=production
ENV LOOKUPS_API=http://api:8080

# Run the SvelteKit Node server
CMD ["node", "build"]