const express = require('express');
const cors    = require('cors');
const crypto  = require('crypto');
const db      = require('./database');

const app  = express();
const PORT = process.env.PORT || 5000;

// ================================================================
//  MIDDLEWARE
// ================================================================
app.use(cors());           // izinkan Unity (port berbeda) akses API
app.use(express.json());   // parse JSON body

// Request logger sederhana
app.use((req, res, next) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.originalUrl}`);
  next();
});

// ================================================================
//  PASSWORD HELPERS (scrypt, tanpa dependency tambahan)
// ================================================================
function hashPassword(password, salt = crypto.randomBytes(16).toString('hex')) {
  const hash = crypto.scryptSync(password, salt, 32).toString('hex');
  return `${salt}:${hash}`;
}

function verifyPassword(password, stored) {
  if (typeof stored !== 'string' || !stored.includes(':')) return false;
  const [salt, hash] = stored.split(':');
  const check = crypto.scryptSync(password, salt, 32).toString('hex');
  const a = Buffer.from(hash, 'hex');
  const b = Buffer.from(check, 'hex');
  return a.length === b.length && crypto.timingSafeEqual(a, b);
}

// ================================================================
//  HEALTH CHECK (Modul 8)
// ================================================================
app.get('/api/health', (req, res) => {
  res.status(200).json({
    status: 'success',
    message: 'Game Backend Server is running successfully!',
    serverTime: new Date().toISOString()
  });
});

// ================================================================
//  AUTH (Modul 8 draft → Modul 11 implementasi)
// ================================================================

app.post('/api/register', (req, res) => {
  const { username, password } = req.body || {};
  if (!username || !password) {
    return res.status(400).json({ status: 'error', message: 'username dan password wajib diisi!' });
  }

  const sql = 'INSERT INTO users (username, password_hash) VALUES (?, ?)';
  db.run(sql, [username, hashPassword(password)], function (err) {
    if (err) {
      if (String(err.message).includes('UNIQUE')) {
        return res.status(409).json({ status: 'error', message: 'Username sudah dipakai.' });
      }
      console.error('[REGISTER ERROR]', err.message);
      return res.status(500).json({ status: 'error', message: 'Gagal mendaftarkan user.' });
    }
    console.log(`[REGISTER OK] userId=${this.lastID} username=${username}`);
    res.status(201).json({ status: 'success', message: 'Registrasi berhasil!', userId: this.lastID });
  });
});

app.post('/api/login', (req, res) => {
  const { username, password } = req.body || {};
  if (!username || !password) {
    return res.status(400).json({ status: 'error', message: 'username dan password wajib diisi!' });
  }

  const sql = 'SELECT id, username, password_hash FROM users WHERE username = ?';
  db.get(sql, [username], (err, row) => {
    if (err) {
      console.error('[LOGIN ERROR]', err.message);
      return res.status(500).json({ status: 'error', message: 'Gagal login.' });
    }
    if (!row || !verifyPassword(password, row.password_hash)) {
      return res.status(401).json({ status: 'error', message: 'Username atau password salah.' });
    }
    const token = crypto.randomBytes(24).toString('hex'); // placeholder; ganti JWT kalau perlu
    console.log(`[LOGIN OK] userId=${row.id} username=${row.username}`);
    res.status(200).json({ status: 'success', message: 'Login berhasil!', token, userId: row.id });
  });
});

// ================================================================
//  SCORE (Modul 11)
// ================================================================

// --- SIMPAN SKOR PERTANDINGAN ---
app.post('/api/score/save', (req, res) => {
  const { userId, score } = req.body || {};

  if (userId === undefined || score === undefined) {
    return res.status(400).json({ status: 'error', message: 'userId dan score wajib diisi!' });
  }

  const uid = parseInt(userId, 10);
  const val = parseInt(score, 10);
  if (Number.isNaN(uid) || Number.isNaN(val) || val < 0) {
    return res.status(400).json({ status: 'error', message: 'userId/score tidak valid.' });
  }

  const sql = 'INSERT INTO scores (user_id, score) VALUES (?, ?)';
  db.run(sql, [uid, val], function (err) {
    if (err) {
      // FK constraint: user_id tidak ada di tabel users
      if (String(err.message).includes('FOREIGN KEY')) {
        return res.status(404).json({ status: 'error', message: 'User tidak ditemukan.' });
      }
      console.error('[SCORE SAVE ERROR]', err.message);
      return res.status(500).json({ status: 'error', message: 'Gagal menyimpan skor ke database.' });
    }
    console.log(`[SCORE SAVED] UserID: ${uid}, Score: ${val}, ScoreID: ${this.lastID}`);
    res.status(201).json({ status: 'success', message: 'Skor berhasil disimpan!', scoreId: this.lastID });
  });
});

// --- PERSONAL HIGH SCORE (SQL MAX) ---
app.get('/api/score/highscore/:userId', (req, res) => {
  const userId = parseInt(req.params.userId, 10);
  if (Number.isNaN(userId)) {
    return res.status(400).json({ status: 'error', message: 'userId tidak valid.' });
  }

  const sql = 'SELECT MAX(score) AS highScore FROM scores WHERE user_id = ?';
  db.get(sql, [userId], (err, row) => {
    if (err) {
      console.error('[HIGH SCORE ERROR]', err.message);
      return res.status(500).json({ status: 'error', message: 'Gagal mengambil data dari database.' });
    }
    const personalHighScore = (row && row.highScore != null) ? row.highScore : 0;
    console.log(`[HIGH SCORE FETCH] UserID: ${userId}, HighScore: ${personalHighScore}`);
    res.status(200).json({ status: 'success', userId, highScore: personalHighScore });
  });
});

// --- LEADERBOARD GLOBAL (opsional) ---
app.get('/api/leaderboard', (req, res) => {
  const sql = `
    SELECT u.username, MAX(s.score) AS score
    FROM scores s
    JOIN users u ON u.id = s.user_id
    GROUP BY s.user_id
    ORDER BY score DESC
    LIMIT 10
  `;
  db.all(sql, [], (err, rows) => {
    if (err) {
      console.error('[LEADERBOARD ERROR]', err.message);
      return res.status(500).json({ status: 'error', message: 'Gagal mengambil leaderboard.' });
    }
    res.status(200).json({ status: 'success', data: rows });
  });
});

// ================================================================
//  404 & GLOBAL ERROR HANDLER
// ================================================================
app.use((req, res) => {
  res.status(404).json({ status: 'error', message: `Endpoint ${req.method} ${req.originalUrl} tidak ditemukan.` });
});

app.use((err, req, res, next) => {
  console.error('[UNHANDLED ERROR]', err);
  res.status(500).json({ status: 'error', message: 'Internal server error.' });
});

// ================================================================
//  START
// ================================================================
app.listen(PORT, () => {
  console.log('============================');
  console.log(' Server Game Backend Berjalan!');
  console.log(` URL: http://localhost:${PORT}`);
  console.log(` Health: http://localhost:${PORT}/api/health`);
  console.log('============================');
});

// Graceful shutdown
process.on('SIGINT', () => {
  console.log('\n[SERVER] Shutting down...');
  db.close(() => process.exit(0));
});