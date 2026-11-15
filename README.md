# SEQI Platform — Web Application

SEQI is a full-stack educational quality evaluation platform built with Node.js, Express, Better-SQLite3, and a modern React client interface.

---

## 📌 Architecture Overview

- **Backend:** Node.js (ESM), Express.js REST API (`server.js`)
- **Database:** SQLite (`database.db`) using `better-sqlite3` (self-initializing schema)
- **Frontend:** React SPA (pre-compiled in `dist/`, served by Express)
- **Authentication:** JWT tokens with bcrypt password hashing
- **File Uploads:** Multer with persistent storage

---

## ⚠️ Important Note: GitHub Pages vs Full-Stack Hosting

> [!IMPORTANT]
> **GitHub Pages only hosts static files (HTML, CSS, JS).**
> Because SEQI requires a Node.js backend (`server.js`) and an SQLite database to handle authentication, scoring calculations, and data persistence, **SEQI cannot run exclusively on GitHub Pages**.
>
> To deploy SEQI from GitHub, push your code to a GitHub repository, then link that repository to a hosting platform supporting Node.js and persistent storage (such as **Render**, **Railway**, **Fly.io**, or any **Docker / VPS** host).

---

## 🚀 Quick Setup & Pushing to GitHub

### 1. Initialize Git & Create Initial Commit
Run these commands in the project directory:

```bash
# Initialize git repository
git init

# Stage all files (sensitive files and large binaries are excluded by .gitignore)
git add .

# Create your first commit
git commit -m "Initial commit: prepare SEQI platform for GitHub deployment"
```

### 2. Push to GitHub
1. Create a new repository on [GitHub](https://github.com/new) (e.g. `seqi-webapp`).
2. Run:
```bash
git branch -M main
git remote add origin https://github.com/<YOUR_USERNAME>/<YOUR_REPOSITORY>.git
git push -u origin main
```

---

## 🌐 Deployment Options

### Option 1: Deploy on Render (Recommended)

1. Sign up on [Render.com](https://render.com).
2. Click **New +** > **Blueprint**.
3. Connect your GitHub repository.
4. Render will automatically detect `render.yaml` and configure:
   - Node.js runtime
   - Build command: `npm install`
   - Start command: `node server.js`
   - Persistent Disk attached for SQLite database and uploads
5. Set your `DEFAULT_ADMIN_PASSWORD` in the Render environment variables dashboard.
6. Click **Apply** to deploy!

---

### Option 2: Deploy with Docker & Docker Compose (Any VPS)

This repository includes a production-ready `Dockerfile` and `docker-compose.yml`.

To deploy on any server with Docker installed:

```bash
# Clone the repository
git clone https://github.com/<YOUR_USERNAME>/<YOUR_REPOSITORY>.git
cd <YOUR_REPOSITORY>

# Set environment variables in .env
cp .env.example .env
# Edit .env with your JWT secret and admin password:
nano .env

# Build and start container in background
docker compose up -d --build
```

The application will start on `http://YOUR_SERVER_IP:3000`.

---

### Option 3: Deploy on Railway

1. Sign in to [Railway.app](https://railway.app).
2. Click **New Project** > **Deploy from GitHub repo**.
3. Select your repository.
4. Add a **Persistent Volume** mounted to `/app` (or configure `DATABASE_PATH` and `UPLOAD_DIR` to a mounted volume).
5. In **Variables**, set:
   - `NODE_ENV=production`
   - `JWT_SECRET=<strong-random-key-32-chars>`
   - `DEFAULT_ADMIN_USERNAME=admin`
   - `DEFAULT_ADMIN_PASSWORD=<your-strong-password>`
6. Railway will automatically build and deploy the app.

---

### Option 4: Deploy on Ubuntu / Debian VPS with PM2

```bash
# Install Node.js 20 & build tools
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt install -y nodejs build-essential

# Clone and enter project
git clone https://github.com/<YOUR_USERNAME>/<YOUR_REPOSITORY>.git
cd <YOUR_REPOSITORY>

# Install dependencies
npm ci --omit=dev

# Configure environment
cp .env.example .env
nano .env

# Install PM2 process manager
sudo npm install -g pm2

# Start application
pm2 start server.js --name "seqi"
pm2 save
pm2 startup
```

---

## 🔐 Environment Variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `NODE_ENV` | Yes | `production` | Environment mode (`production` or `development`) |
| `PORT` | No | `3000` | HTTP port the server listens on |
| `JWT_SECRET` | Yes | None | Secret key for signing authentication tokens (min 32 chars) |
| `DATABASE_PATH` | No | `database.db` | File path for SQLite database |
| `UPLOAD_DIR` | No | `uploads` | Directory for uploaded attachments |
| `DEFAULT_ADMIN_USERNAME` | No | `admin` | Initial administrator username |
| `DEFAULT_ADMIN_PASSWORD` | Yes (in prod) | None | Initial administrator password (min 8 chars) |

---

## 🛠 Local Development

```bash
# Install dependencies
npm install

# Start server locally
npm start
```
Open [http://localhost:3000](http://localhost:3000) in your browser.
