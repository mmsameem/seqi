# SEQI Webhost Deployment Guide

This guide provides instructions for deploying the SEQI application to a Node.js web host (cPanel, CloudLinux Passenger, VPS, or cloud container).

---

## 1. Prerequisites

- **Node.js** version `18+` (20+ LTS recommended)
- **Host Type:** Node.js-capable server (Express + SQLite requires a persistent Node process; static-only hosts like pure PHP/HTML hosting cannot run this backend).
- Native SQLite support (`better-sqlite3` includes precompiled binaries, with `@esbuild/linux-x64` configured for Linux server environments).

---

## 2. Production Files Checklist

When uploading a deployment package, ensure these essential components are present:

```
├── dist/                      # Compiled frontend assets and PWA files
├── dist-server/               # Compiled backend server scripts
├── public/                    # Static resources (favicon.svg, print.css)
├── assets/                    # Static assets fallback for Apache/LiteSpeed
├── uploads/                   # User evidence directory (must be writable)
│   └── .gitkeep
├── tmp/                       # Passenger process control
│   └── restart.txt
├── .htaccess                  # Apache/Passenger configuration
├── .env.example               # Template environment configuration
├── package.json               # Production dependencies
├── package-lock.json
├── server.js                  # Application entry point
├── database.db                # SQLite database (ensure write permissions)
├── index.html                 # Main frontend entry point
├── favicon.svg
└── print.css
```

---

## 3. Environment Variables Configuration

Create a `.env` file in the app root using `.env.example`:

```env
NODE_ENV=production
PORT=3000
JWT_SECRET=your-secure-random-secret-key-at-least-32-chars
DATABASE_PATH=database.db
UPLOAD_DIR=uploads
DEFAULT_ADMIN_USERNAME=admin
DEFAULT_ADMIN_PASSWORD=your-secure-admin-password
```

> **Important:**
> - `JWT_SECRET` is mandatory in production.
> - `DEFAULT_ADMIN_PASSWORD` must be at least 8 characters.

---

## 4. cPanel / CloudLinux Deployment Steps

1. **Extract Deployment Zip:**
   Extract `SEQI_Deployment_Package.zip` directly into your application root folder (e.g. `public_html/subdomain`).

2. **Configure Node.js App in cPanel:**
   - Go to **Setup Node.js App** in cPanel.
   - **Node.js version:** Select `18.x`, `20.x`, or `22.x`.
   - **Application mode:** `Production`.
   - **Application root:** Target directory path.
   - **Application startup file:** `server.js`.
   - Click **Save** / **Create**.

3. **Install Dependencies:**
   - In cPanel Node.js interface, click **Run NPM Install** (or enter the virtual environment via SSH/Terminal and run `npm install --omit=dev`).

4. **Verify Permissions:**
   - Ensure `database.db` and the `uploads/` folder have read/write permissions for the web server user (`chmod 755` or `chmod 775`).

5. **Restart & Test:**
   - Click **Restart Application** in cPanel.
   - Test endpoints:
     - Root URL: `https://your-domain.com/`
     - CSS: `https://your-domain.com/print.css`
     - Manifest: `https://your-domain.com/manifest.webmanifest`
