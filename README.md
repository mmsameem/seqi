# SEQI (School Educational Quality Index)

SEQI is a comprehensive School Educational Quality Index system featuring a React 19 web frontend, an Express + SQLite backend, a native Windows Desktop Installer builder, and an Android application wrapper.

---

## Table of Contents
- [Architecture & Overview](#architecture--overview)
- [Requirements](#requirements)
- [Local Setup & Running](#local-setup--running)
- [Environment Configuration](#environment-configuration)
- [Deployment](#deployment)
  - [cPanel / CloudLinux / Passenger](#cpanel--cloudlinux--passenger)
  - [Standard Node.js Host / VPS](#standard-nodejs-host--vps)
- [Desktop & Android Builds](#desktop--android-builds)
- [Security & Best Practices](#security--best-practices)

---

## Architecture & Overview

- **Frontend:** Built with React 19, Tailwind CSS, Lucide icons, and Recharts. Production assets reside in `dist/` and `assets/`.
- **Backend:** Node.js Express server (`server.js`), using `better-sqlite3` for high-performance embedded storage and `multer` for evidence photo/document uploads.
- **Database:** SQLite (`database.db`), structured with 8 evaluation fields, criteria, indicators, scoring, and user management.
- **Windows Desktop:** Standalone launcher and installer (`setup_builder/SetupProgram.cs`, `launcher.cs`, `CREATE_DESKTOP_SHORTCUT.bat`).
- **Mobile:** Capacitor Android project (`android/`).

---

## Requirements

- **Node.js:** `18.0.0` or newer (Node 20+ recommended)
- **NPM:** `9.0.0` or newer

---

## Local Setup & Running

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Configure environment:**
   Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```
   *On Windows PowerShell:*
   ```powershell
   Copy-Item .env.example .env
   ```

3. **Start the application:**
   ```bash
   npm start
   ```
   The application will start at `http://localhost:3000`.

4. **Default Credentials:**
   - **Admin:** `admin` / Password defined in `DEFAULT_ADMIN_PASSWORD` (or set during initialization)
   - **Field Users:** `group1` through `group8`

---

## Environment Configuration

Configure the following variables in `.env`:

| Variable | Description | Default |
| :--- | :--- | :--- |
| `NODE_ENV` | Application environment (`production` or `development`) | `production` |
| `PORT` | Listening port for Express | `3000` |
| `JWT_SECRET` | Secret key for signing JSON Web Tokens (**must be changed in production**) | — |
| `DATABASE_PATH` | Path to the SQLite database file | `database.db` |
| `UPLOAD_DIR` | Directory for evidence uploads | `uploads` |
| `DEFAULT_ADMIN_USERNAME` | Initial administrator username | `admin` |
| `DEFAULT_ADMIN_PASSWORD` | Initial administrator password (min 8 characters) | — |

---

## Deployment

### cPanel / CloudLinux / Passenger

The repository includes pre-configured `.htaccess` and `tmp/restart.txt` for CloudLinux Passenger hosting:

1. Upload the files from the deployment package (or clone this repo) to your application root (e.g., `public_html/your-subdomain`).
2. In cPanel **Setup Node.js App**:
   - **Application root:** path to your folder
   - **Application startup file:** `server.js`
   - **Node.js version:** Select `18.x`, `20.x`, or newer
3. Run `npm install` inside the Node.js application environment.
4. Set your production environment variables in cPanel or in `.env`.
5. Restart the application (or click **Restart** in cPanel, which updates `tmp/restart.txt`).
6. Verify endpoints:
   - `https://your-domain.com/` (loads SEQI UI)
   - `https://your-domain.com/manifest.webmanifest`
   - `https://your-domain.com/print.css`

### Standard Node.js Host / VPS

```bash
# Clone and enter directory
git clone <repository-url>
cd <repository-dir>

# Install production dependencies
npm install --omit=dev

# Start with PM2, Systemd, or Docker
npm start
```

---

## Desktop & Android Builds

- **Windows Installer:** Source is located in `setup_builder/SetupProgram.cs`. Run `desktop-execution/CREATE_DESKTOP_SHORTCUT.bat` to configure local shortcuts.
- **Android App:** Open `android/` in Android Studio or build with Gradle (`./gradlew assembleDebug`).

---

## Security & Best Practices

- Always ensure `.env` and `database.db` have proper file permissions on the server.
- Keep `JWT_SECRET` secret and at least 32 random characters long.
- Regularly back up `database.db` and the `uploads/` folder.

