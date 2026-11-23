import express from 'express';
import path from 'path';
import { fileURLToPath } from 'url';
import dotenv from 'dotenv';
import Database from 'better-sqlite3';
import multer from 'multer';
import fs from 'fs';
import jwt from 'jsonwebtoken';
import bcrypt from 'bcryptjs';
dotenv.config();
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const appRoot = process.cwd();
const isProduction = process.env.NODE_ENV === 'production';
const rawPort = Number(process.env.PORT || 3000);
const PORT = Number.isFinite(rawPort) && rawPort > 0 ? rawPort : 3000;
const databasePath = path.resolve(appRoot, process.env.DATABASE_PATH || 'database.db');
const uploadsDir = path.resolve(appRoot, process.env.UPLOAD_DIR || 'uploads');
const jwtSecretFromEnv = process.env.JWT_SECRET?.trim();
const JWT_SECRET = jwtSecretFromEnv || 'seqi-secret-key-change-in-production';
const defaultAdminUsername = (process.env.DEFAULT_ADMIN_USERNAME || 'admin').trim() || 'admin';
const defaultAdminPassword = process.env.DEFAULT_ADMIN_PASSWORD?.trim() || (isProduction ? '' : 'admin123');
const app = express();
if (isProduction && !jwtSecretFromEnv) {
    console.error('Missing JWT_SECRET. Set a strong JWT_SECRET before starting the production server.');
    process.exit(1);
}
fs.mkdirSync(path.dirname(databasePath), { recursive: true });
fs.mkdirSync(uploadsDir, { recursive: true });
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
const db = new Database(databasePath);
const authenticate = (req, res, next) => {
    const authHeader = req.headers.authorization;
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
        return res.status(401).json({ success: false, message: 'No token provided' });
    }
    const token = authHeader.split(' ')[1];
    try {
        const decoded = jwt.verify(token, JWT_SECRET);
        req.user = decoded;
        next();
    }
    catch (err) {
        return res.status(401).json({ success: false, message: 'Invalid token' });
    }
};
const requireAdmin = (req, res, next) => {
    const user = req.user;
    if (!user || user.role !== 'admin') {
        return res.status(403).json({ success: false, message: 'Admin access required' });
    }
    next();
};
// Initialize database
db.exec(`
  CREATE TABLE IF NOT EXISTS fields (
    id TEXT PRIMARY KEY,
    title TEXT
  );

  CREATE TABLE IF NOT EXISTS scores (
    field_id TEXT,
    indicator_id TEXT,
    score INTEGER,
    input_value REAL,
    notes TEXT,
    total_students INTEGER,
    achieved_count INTEGER,
    attachments TEXT,
    custom_data TEXT,
    PRIMARY KEY (field_id, indicator_id)
  );

  CREATE TABLE IF NOT EXISTS metadata (
    key TEXT PRIMARY KEY,
    value TEXT
  );

  CREATE TABLE IF NOT EXISTS groups (
    id TEXT PRIMARY KEY,
    name TEXT,
    field_id TEXT
  );

  CREATE TABLE IF NOT EXISTS teachers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT,
    role TEXT,
    group_id TEXT,
    academic_year TEXT
  );

  CREATE TABLE IF NOT EXISTS criteria (
    field_id TEXT,
    id TEXT,
    title TEXT,
    PRIMARY KEY (field_id, id)
  );

  CREATE TABLE IF NOT EXISTS indicators (
    id TEXT PRIMARY KEY,
    field_id TEXT,
    criteria_id TEXT,
    text TEXT
  );

  CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT UNIQUE,
    password TEXT,
    role TEXT,
    assigned_field_id TEXT
  );
`);
const legacySalt = 'seqi_salt';
const legacyHash = (pwd) => Buffer.from(pwd + legacySalt).toString('base64');
const isBcryptHash = (value) => typeof value === 'string' && (value.startsWith('$2a$') || value.startsWith('$2b$') || value.startsWith('$2y$'));
const hashPassword = (pwd) => bcrypt.hashSync(pwd, 10);
const verifyPassword = (pwd, storedHash) => isBcryptHash(storedHash) ? bcrypt.compareSync(pwd, storedHash) : storedHash === legacyHash(pwd);
// Seed default admin
try {
    const adminCheck = db.prepare("SELECT count(*) as count FROM users WHERE role = 'admin'").get();
    if (!adminCheck || adminCheck.count === 0) {
        if (defaultAdminPassword.length < 8) {
            console.error('No admin user exists. Set DEFAULT_ADMIN_USERNAME and DEFAULT_ADMIN_PASSWORD (minimum 8 characters) before first production start.');
            if (isProduction) {
                process.exit(1);
            }
        }
        db.prepare("INSERT INTO users (username, password, role, assigned_field_id) VALUES (?, ?, ?, ?)")
            .run(defaultAdminUsername, hashPassword(defaultAdminPassword), 'admin', null);
        if (isProduction) {
            console.log(`Bootstrap admin created: ${defaultAdminUsername}`);
        }
        else {
            console.log(`Default admin created: ${defaultAdminUsername} / ${defaultAdminPassword}`);
        }
    }
}
catch (err) {
    console.error("Database seeding error:", err);
    if (isProduction) {
        process.exit(1);
    }
}
// API Routes
app.get('/api/health', (_req, res) => {
    res.json({ success: true, status: 'ok' });
});
app.post('/api/login', (req, res) => {
    const { username, password } = req.body;
    const row = db.prepare('SELECT id, username, role, assigned_field_id, password FROM users WHERE username = ?')
        .get(String(username ?? '').trim());
    if (!row || typeof password !== 'string') {
        return res.status(401).json({ success: false, message: 'Invalid credentials' });
    }
    const ok = verifyPassword(password, row.password);
    if (ok) {
        if (!isBcryptHash(row.password)) {
            try {
                db.prepare('UPDATE users SET password = ? WHERE id = ?').run(hashPassword(password), row.id);
            }
            catch (err) {
                console.warn('Password migration failed for user:', row.username);
            }
        }
        const token = jwt.sign({ id: row.id, username: row.username, role: row.role, assignedFieldId: row.assigned_field_id ?? null }, JWT_SECRET, { expiresIn: '24h' });
        res.json({ success: true, user: {
                id: row.id, username: row.username, role: row.role, assignedFieldId: row.assigned_field_id
            }, token });
    }
    else {
        res.status(401).json({ success: false, message: 'Invalid credentials' });
    }
});
app.get('/api/users', authenticate, requireAdmin, (req, res) => {
    const users = db.prepare('SELECT id, username, role, assigned_field_id FROM users').all();
    res.json(users);
});
app.post('/api/users', authenticate, requireAdmin, (req, res) => {
    const { username, password, role, assignedFieldId } = req.body;
    const normalizedUsername = String(username ?? '').trim();
    if (!normalizedUsername) {
        return res.status(400).json({ success: false, message: 'Username is required' });
    }
    if (typeof password !== 'string' || password.length < 8) {
        return res.status(400).json({ success: false, message: 'Password must be at least 8 characters' });
    }
    if (!role || !['admin', 'field'].includes(String(role))) {
        return res.status(400).json({ success: false, message: 'Role must be admin or field' });
    }
    try {
        db.prepare('INSERT INTO users (username, password, role, assigned_field_id) VALUES (?, ?, ?, ?)')
            .run(normalizedUsername, hashPassword(password), String(role), assignedFieldId ?? null);
        res.json({ success: true });
    }
    catch (err) {
        res.status(400).json({ success: false, message: 'User initialization failed' });
    }
});
app.patch('/api/users/:id/password', authenticate, requireAdmin, (req, res) => {
    const { password } = req.body;
    if (typeof password !== 'string' || password.length < 8) {
        return res.status(400).json({ success: false, message: 'Password must be at least 8 characters' });
    }
    const user = db.prepare('SELECT id FROM users WHERE id = ?').get(req.params.id);
    if (!user) {
        return res.status(404).json({ success: false, message: 'User not found' });
    }
    db.prepare('UPDATE users SET password = ? WHERE id = ?').run(hashPassword(password), req.params.id);
    res.json({ success: true });
});
app.patch('/api/users/:id', authenticate, requireAdmin, (req, res) => {
    const { username, password } = req.body;
    const normalizedUsername = String(username ?? '').trim();
    if (!normalizedUsername) {
        return res.status(400).json({ success: false, message: 'Username is required' });
    }
    if (password !== undefined && (typeof password !== 'string' || password.length < 8)) {
        return res.status(400).json({ success: false, message: 'Password must be at least 8 characters' });
    }
    const user = db.prepare('SELECT id FROM users WHERE id = ?').get(req.params.id);
    if (!user) {
        return res.status(404).json({ success: false, message: 'User not found' });
    }
    try {
        if (typeof password === 'string' && password.length > 0) {
            db.prepare('UPDATE users SET username = ?, password = ? WHERE id = ?')
                .run(normalizedUsername, hashPassword(password), req.params.id);
        }
        else {
            db.prepare('UPDATE users SET username = ? WHERE id = ?').run(normalizedUsername, req.params.id);
        }
        res.json({ success: true });
    }
    catch (err) {
        res.status(400).json({ success: false, message: 'Username already exists. Try a different one.' });
    }
});
app.delete('/api/users/:id', authenticate, requireAdmin, (req, res) => {
    db.prepare('DELETE FROM users WHERE id = ? AND role != ?').run(req.params.id, 'admin');
    res.json({ success: true });
});
app.get('/api/scores', authenticate, (req, res) => {
    const user = req.user;
    const rows = user.role === 'admin'
        ? db.prepare('SELECT * FROM scores').all()
        : db.prepare('SELECT * FROM scores WHERE field_id = ?').all(user.assignedFieldId);
    const scores = {};
    rows.forEach(row => {
        if (!scores[row.field_id])
            scores[row.field_id] = {};
        scores[row.field_id][row.indicator_id] = {
            score: row.score, inputValue: row.input_value, notes: row.notes,
            totalStudents: row.total_students, achievedCount: row.achieved_count,
            classPercentages: row.custom_data ? JSON.parse(row.custom_data) : {},
            attachments: row.attachments ? JSON.parse(row.attachments) : []
        };
    });
    res.json(scores);
});
app.post('/api/scores', authenticate, (req, res) => {
    const user = req.user;
    const { fieldId, indicatorId, score, inputValue, notes, totalStudents, achievedCount, classPercentages, attachments } = req.body;
    if (user.role !== 'admin' && user.assignedFieldId !== fieldId) {
        return res.status(403).json({ success: false, message: 'Not allowed for this field' });
    }
    db.prepare(`INSERT OR REPLACE INTO scores (field_id, indicator_id, score, input_value, notes, total_students, achieved_count, custom_data, attachments) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`)
        .run(fieldId, indicatorId, score, inputValue || null, notes || null, totalStudents || null, achievedCount || null, classPercentages ? JSON.stringify(classPercentages) : null, attachments ? JSON.stringify(attachments) : null);
    res.json({ success: true });
});
app.get('/api/metadata', authenticate, (req, res) => {
    const rows = db.prepare('SELECT * FROM metadata').all();
    const meta = {};
    rows.forEach(row => {
        try {
            meta[row.key] = JSON.parse(row.value);
        }
        catch {
            meta[row.key] = row.value;
        }
    });
    res.json(meta);
});
app.post('/api/metadata', authenticate, (req, res) => {
    const { key, value } = req.body;
    db.prepare('INSERT OR REPLACE INTO metadata (key, value) VALUES (?, ?)')
        .run(key, typeof value === 'string' ? value : JSON.stringify(value));
    res.json({ success: true });
});
app.get('/api/teachers', authenticate, (req, res) => {
    const rows = db.prepare(`
    SELECT t.*, g.name as groupName 
    FROM teachers t 
    LEFT JOIN groups g ON t.group_id = g.id
  `).all();
    res.json(rows);
});
app.post('/api/teachers', authenticate, (req, res) => {
    const { name, role, groupId, academicYear } = req.body;
    const info = db.prepare('INSERT INTO teachers (name, role, group_id, academic_year) VALUES (?, ?, ?, ?)')
        .run(name, role, groupId, academicYear);
    res.json({ id: info.lastInsertRowid });
});
app.delete('/api/teachers/:id', authenticate, (req, res) => {
    db.prepare('DELETE FROM teachers WHERE id = ?').run(req.params.id);
    res.json({ success: true });
});
app.get('/api/groups', authenticate, (req, res) => {
    res.json(db.prepare('SELECT * FROM groups').all());
});
app.get('/api/structure', authenticate, (req, res) => {
    const fields = db.prepare('SELECT * FROM fields ORDER BY id').all();
    const structure = {};
    fields.forEach(f => {
        const criteria = db.prepare('SELECT * FROM criteria WHERE field_id = ? ORDER BY id').all(f.id);
        structure[f.id] = {
            title: f.title,
            criteria: criteria.map(c => ({
                ...c,
                indicators: db.prepare('SELECT * FROM indicators WHERE field_id = ? AND criteria_id = ? ORDER BY id').all(f.id, c.id)
            }))
        };
    });
    res.json(structure);
});
app.post('/api/initialize-structure', authenticate, requireAdmin, (req, res) => {
    const { fields } = req.body;
    const transaction = db.transaction((data) => {
        data.forEach((f) => {
            db.prepare('INSERT OR REPLACE INTO fields (id, title) VALUES (?, ?)').run(f.id, f.title);
            f.criteria.forEach((c) => {
                db.prepare('INSERT OR REPLACE INTO criteria (field_id, id, title) VALUES (?, ?, ?)').run(f.id, c.id, c.title);
                c.indicators.forEach((ind) => {
                    db.prepare('INSERT OR REPLACE INTO indicators (id, field_id, criteria_id, text) VALUES (?, ?, ?, ?)').run(ind.id, f.id, c.id, ind.text);
                });
            });
        });
    });
    transaction(fields);
    res.json({ success: true });
});
app.post('/api/reset', authenticate, requireAdmin, (req, res) => {
    try {
        db.exec('DELETE FROM scores; DELETE FROM metadata; DELETE FROM teachers; DELETE FROM users WHERE role != "admin";');
        res.json({ success: true });
    }
    catch (err) {
        console.error('Reset error:', err);
        res.status(500).json({ success: false, message: 'Reset failed' });
    }
});
// Uploads
const storage = multer.diskStorage({
    destination: (req, file, cb) => {
        fs.mkdirSync(uploadsDir, { recursive: true });
        cb(null, uploadsDir);
    },
    filename: (req, file, cb) => cb(null, Date.now() + path.extname(file.originalname))
});
const upload = multer({
    storage,
    limits: { fileSize: 25 * 1024 * 1024 }
});
app.post('/api/upload', authenticate, upload.single('file'), (req, res) => {
    if (!req.file) {
        return res.status(400).json({ success: false, message: 'No file uploaded' });
    }
    res.json({ url: `/uploads/${req.file.filename}` });
});
app.delete('/api/upload', authenticate, (req, res) => {
    try {
        const fileUrl = req.body?.url;
        if (!fileUrl || typeof fileUrl !== 'string') {
            return res.status(400).json({ success: false, message: 'File URL is required' });
        }
        const filename = path.basename(fileUrl);
        const filePath = path.resolve(uploadsDir, filename);

        // Security check: ensure target path is strictly within uploadsDir
        if (!filePath.startsWith(uploadsDir)) {
            return res.status(403).json({ success: false, message: 'Forbidden file path' });
        }

        if (fs.existsSync(filePath)) {
            fs.unlinkSync(filePath);
            console.log(`Deleted uploaded file: ${filename}`);
        }
        res.json({ success: true, message: 'File removed successfully' });
    } catch (err) {
        console.error('File deletion error:', err);
        res.status(500).json({ success: false, message: 'Failed to delete file' });
    }
});
app.use('/uploads', (req, res, next) => {
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Cross-Origin-Resource-Policy', 'cross-origin');
    next();
}, express.static(uploadsDir));
// Serve print stylesheet directly from `public/` so print fixes don't require a client rebuild.
app.use('/print.css', express.static(path.join(appRoot, 'public', 'print.css')));
app.get('/favicon.ico', (_req, res) => {
    res.type('image/svg+xml').sendFile(path.join(appRoot, 'public', 'favicon.svg'));
});
// Final catch-all for React
const distPath = path.join(appRoot, 'dist');
app.use(express.static(distPath));
app.get('*', (req, res) => {
    res.sendFile(path.join(distPath, 'index.html'));
});
app.listen(PORT, () => {
    console.log(`SEQI Server running at http://localhost:${PORT}`);
});
