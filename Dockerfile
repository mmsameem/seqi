# Production Dockerfile for SEQI Platform
FROM node:20-alpine AS runner

# Install python and build tools needed for better-sqlite3 native compilation
RUN apk add --no-cache python3 make g++

WORKDIR /app

# Copy package manifests
COPY package*.json ./

# Install production dependencies
RUN npm ci --omit=dev

# Copy server and static distributions
COPY server.js ./
COPY dist ./dist
COPY public ./public

# Prepare persistent data and upload mounts
RUN mkdir -p /app/data /app/uploads

# Default production environment variables
ENV NODE_ENV=production
ENV PORT=3000
ENV DATABASE_PATH=/app/data/database.db
ENV UPLOAD_DIR=/app/uploads

EXPOSE 3000

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:3000/favicon.ico || exit 1

CMD ["node", "server.js"]
