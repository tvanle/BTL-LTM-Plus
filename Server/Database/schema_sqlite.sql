-- ============================================
-- Word Game Database Schema - SQLite
-- ============================================

-- Users Table
CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    display_name TEXT,
    avatar_url TEXT,
    is_online INTEGER DEFAULT 0,
    last_login_at TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    CHECK (LENGTH(username) >= 3)
);

CREATE INDEX IF NOT EXISTS idx_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_is_online ON users(is_online);

-- User Statistics Table
CREATE TABLE IF NOT EXISTS user_stats (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    total_score INTEGER DEFAULT 0,
    best_score INTEGER DEFAULT 0,
    best_streak INTEGER DEFAULT 0,
    games_played INTEGER DEFAULT 0,
    games_won INTEGER DEFAULT 0,
    total_words_found INTEGER DEFAULT 0,
    average_completion_time REAL DEFAULT 0,
    rank_position INTEGER DEFAULT 0,
    total_xp INTEGER DEFAULT 0,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_user_id ON user_stats(user_id);
CREATE INDEX IF NOT EXISTS idx_rank ON user_stats(rank_position);
CREATE INDEX IF NOT EXISTS idx_total_score ON user_stats(total_score DESC);

-- Game Invitations Table
CREATE TABLE IF NOT EXISTS game_invitations (
    id TEXT PRIMARY KEY,
    sender_id TEXT NOT NULL,
    receiver_id TEXT NOT NULL,
    room_code TEXT,
    category TEXT,
    status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'accepted', 'declined', 'expired')),
    expires_at TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    responded_at TEXT,
    FOREIGN KEY (sender_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (receiver_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_receiver_id ON game_invitations(receiver_id);
CREATE INDEX IF NOT EXISTS idx_sender_id ON game_invitations(sender_id);
CREATE INDEX IF NOT EXISTS idx_inv_status ON game_invitations(status);
CREATE INDEX IF NOT EXISTS idx_expires_at ON game_invitations(expires_at);

-- Match History Table
CREATE TABLE IF NOT EXISTS match_history (
    id TEXT PRIMARY KEY,
    room_code TEXT NOT NULL,
    category TEXT NOT NULL,
    level_duration INTEGER NOT NULL,
    total_levels INTEGER NOT NULL,
    num_questions INTEGER NOT NULL,
    host_id TEXT NOT NULL,
    winner_id TEXT,
    started_at TEXT DEFAULT CURRENT_TIMESTAMP,
    completed_at TEXT,
    total_duration_seconds INTEGER,
    FOREIGN KEY (host_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (winner_id) REFERENCES users(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_host_id ON match_history(host_id);
CREATE INDEX IF NOT EXISTS idx_winner_id ON match_history(winner_id);
CREATE INDEX IF NOT EXISTS idx_started_at ON match_history(started_at DESC);
CREATE INDEX IF NOT EXISTS idx_room_code ON match_history(room_code);

-- Match Player Results Table
CREATE TABLE IF NOT EXISTS match_player_results (
    id TEXT PRIMARY KEY,
    match_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    final_score INTEGER DEFAULT 0,
    best_streak INTEGER DEFAULT 0,
    total_words_found INTEGER DEFAULT 0,
    average_time_per_level REAL DEFAULT 0,
    completed_levels INTEGER DEFAULT 0,
    rank_position INTEGER DEFAULT 0,
    xp_gained INTEGER DEFAULT 0,
    FOREIGN KEY (match_id) REFERENCES match_history(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_match_id ON match_player_results(match_id);
CREATE INDEX IF NOT EXISTS idx_mpr_user_id ON match_player_results(user_id);
CREATE INDEX IF NOT EXISTS idx_final_score ON match_player_results(final_score DESC);

-- Session Tokens Table (for authentication)
CREATE TABLE IF NOT EXISTS session_tokens (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    token TEXT NOT NULL UNIQUE,
    device_info TEXT,
    ip_address TEXT,
    expires_at TEXT NOT NULL,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    last_used_at TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_st_user_id ON session_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_token ON session_tokens(token);
CREATE INDEX IF NOT EXISTS idx_st_expires_at ON session_tokens(expires_at);
