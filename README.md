# SEQI Platform — Deployment Guide for GitHub & Vercel

SEQI is an institutional educational quality evaluation platform consisting of a high-performance React client and an Express.js backend with an SQLite persistence layer.

---

## 🏗 System Architecture on Vercel

Vercel is a **serverless edge platform** designed for fast frontend hosting and stateless APIs. Because SQLite (`database.db`) and uploaded files require **persistent storage** (which serverless functions wipe on cold starts), SEQI is configured for the **industry-standard hybrid deployment**:

```
[ User Browser ]
       │
       ▼
[ Vercel Edge CDN ] ────► Serves React Frontend (dist/) instantaneously
       │
       ▼ (/api/* and /uploads/* proxied via api/proxy.js)
[ Render / Railway / VPS ] ────► Runs server.js with Persistent SQLite database
```

- **Frontend on Vercel:** Ultra-fast, global CDN, automated deployments on git push.
- **Backend on Render / Railway / VPS:** Keeps your SQLite database (`database.db`) safe and persistent across restarts.
- **Seamless Proxy:** `api/proxy.js` automatically forwards all API calls from Vercel to your backend based on the `BACKEND_URL` environment variable.

---

## 🚀 Step 1: Push Repository to GitHub

Open a terminal in the project directory (`SEQI_Final - Copy`) and run:

```bash
# 1. Add all prepared files (sensitive credentials and heavy binaries are excluded by .gitignore)
git add .

# 2. Commit the changes
git commit -m "Configure Vercel proxy and deployment settings"

# 3. Link to your GitHub repository (replace with your repo URL)
git branch -M main
git remote add origin https://github.com/<YOUR_USERNAME>/<YOUR_REPOSITORY>.git

# 4. Push to GitHub
git push -u origin main
```

---

## ⚡ Step 2: Deploy Frontend to Vercel

1. Log in to [Vercel](https://vercel.com).
2. Click **Add New...** > **Project**.
3. Import your GitHub repository (`<YOUR_REPOSITORY>`).
4. In the project configuration:
   - **Framework Preset:** `Other`
   - **Root Directory:** `./`
   - **Build Command:** `npm run build`
   - **Output Directory:** `dist`
5. Click **Deploy**.

> 💡 Your React web app is now live on Vercel!

---

## 🗄 Step 3: Deploy the Persistent Backend (Render or Railway)

To enable database persistence (user login, score saving, teacher evaluations, uploads):

### Option A: Render (Free & 1-Click with `render.yaml`)
1. Log in to [Render.com](https://render.com).
2. Click **New +** > **Blueprint**.
3. Connect your GitHub repository.
4. Render automatically reads [`render.yaml`](file:///i:/SEQI_Final%20-%20Copy/render.yaml), attaches a persistent disk for `database.db`, and deploys the backend.
5. In Render's dashboard, set `DEFAULT_ADMIN_PASSWORD` (min 8 characters).
6. Copy your Render backend service URL (e.g., `https://seqi-backend.onrender.com`).

### Option B: Railway
1. Go to [Railway.app](https://railway.app) > **New Project** > **Deploy from GitHub repo**.
2. Add a persistent volume mounted to `/app/data`.
3. In **Variables**, set:
   - `NODE_ENV=production`
   - `JWT_SECRET=your-32-char-random-jwt-secret`
   - `DEFAULT_ADMIN_USERNAME=admin`
   - `DEFAULT_ADMIN_PASSWORD=your-strong-password`
   - `DATABASE_PATH=/app/data/database.db`
4. Copy your public service URL.

---

## 🔗 Step 4: Connect Vercel to Your Backend

Once your backend is running:
1. In your **Vercel Project Dashboard**, go to **Settings** > **Environment Variables**.
2. Add a new variable:
   - **Key:** `BACKEND_URL`
   - **Value:** `https://your-backend-url.onrender.com` (your backend URL without trailing slash)
3. Click **Save** and trigger a **Redeploy** on Vercel.

Your Vercel deployment will now proxy all authentication, scoring, and file uploads directly to your persistent database.

---

## 💻 Local Running

```bash
# Install dependencies
npm install

# Start server locally
npm start
```
Server runs on [http://localhost:3000](http://localhost:3000).
