-- ============================================
-- Word Game Database Schema - MySQL
-- ============================================

-- Users Table
CREATE TABLE users (
    id CHAR(36) PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(100),
    avatar_url VARCHAR(500),
    is_online BOOLEAN DEFAULT FALSE,
    last_login_at DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT chk_username_length CHECK (CHAR_LENGTH(username) >= 3),
    CONSTRAINT chk_email_format CHECK (email REGEXP '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'),
    INDEX idx_username (username),
    INDEX idx_email (email),
    INDEX idx_is_online (is_online)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- User Statistics Table
CREATE TABLE user_stats (
    id CHAR(36) PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    total_score INT DEFAULT 0,
    best_score INT DEFAULT 0,
    best_streak INT DEFAULT 0,
    games_played INT DEFAULT 0,
    games_won INT DEFAULT 0,
    total_words_found INT DEFAULT 0,
    average_completion_time FLOAT DEFAULT 0,
    rank_position INT DEFAULT 0,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_rank (rank_position),
    INDEX idx_total_score (total_score DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Friendships Table (bidirectional)
CREATE TABLE friendships (
    id CHAR(36) PRIMARY KEY,
    user_id1 CHAR(36) NOT NULL,
    user_id2 CHAR(36) NOT NULL,
    status ENUM('pending', 'accepted', 'blocked') DEFAULT 'pending',
    requester_id CHAR(36) NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    accepted_at DATETIME,
    FOREIGN KEY (user_id1) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id2) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (requester_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT chk_different_users CHECK (user_id1 != user_id2),
    UNIQUE KEY unique_friendship (user_id1, user_id2),
    INDEX idx_user_id1 (user_id1),
    INDEX idx_user_id2 (user_id2),
    INDEX idx_status (status),
    INDEX idx_requester (requester_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Game Invitations Table
CREATE TABLE game_invitations (
    id CHAR(36) PRIMARY KEY,
    sender_id CHAR(36) NOT NULL,
    receiver_id CHAR(36) NOT NULL,
    room_code VARCHAR(6),
    category VARCHAR(50),
    status ENUM('pending', 'accepted', 'declined', 'expired') DEFAULT 'pending',
    expires_at DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    responded_at DATETIME,
    FOREIGN KEY (sender_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (receiver_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_receiver_id (receiver_id),
    INDEX idx_sender_id (sender_id),
    INDEX idx_status (status),
    INDEX idx_expires_at (expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Match History Table
CREATE TABLE match_history (
    id CHAR(36) PRIMARY KEY,
    room_code VARCHAR(6) NOT NULL,
    category VARCHAR(50) NOT NULL,
    level_duration INT NOT NULL,
    total_levels INT NOT NULL,
    num_questions INT NOT NULL,
    host_id CHAR(36) NOT NULL,
    winner_id CHAR(36),
    started_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    completed_at DATETIME,
    total_duration_seconds INT,
    FOREIGN KEY (host_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (winner_id) REFERENCES users(id) ON DELETE SET NULL,
    INDEX idx_host_id (host_id),
    INDEX idx_winner_id (winner_id),
    INDEX idx_started_at (started_at DESC),
    INDEX idx_room_code (room_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Match Player Results Table
CREATE TABLE match_player_results (
    id CHAR(36) PRIMARY KEY,
    match_id CHAR(36) NOT NULL,
    user_id CHAR(36) NOT NULL,
    final_score INT DEFAULT 0,
    best_streak INT DEFAULT 0,
    total_words_found INT DEFAULT 0,
    average_time_per_level FLOAT DEFAULT 0,
    completed_levels INT DEFAULT 0,
    rank_position INT DEFAULT 0,
    FOREIGN KEY (match_id) REFERENCES match_history(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_match_id (match_id),
    INDEX idx_user_id (user_id),
    INDEX idx_final_score (final_score DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Session Tokens Table (for authentication)
CREATE TABLE session_tokens (
    id CHAR(36) PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    token VARCHAR(500) NOT NULL UNIQUE,
    device_info VARCHAR(200),
    ip_address VARCHAR(50),
    expires_at DATETIME NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_used_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_token (token),
    INDEX idx_expires_at (expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Stored Procedure: Get User Friends List
DELIMITER $$
CREATE PROCEDURE GetUserFriends(IN p_user_id CHAR(36))
BEGIN
    SELECT
        u.id,
        u.username,
        u.display_name,
        u.avatar_url,
        u.is_online,
        u.last_login_at,
        f.created_at as friend_since
    FROM friendships f
    INNER JOIN users u ON (
        CASE
            WHEN f.user_id1 = p_user_id THEN f.user_id2
            ELSE f.user_id1
        END = u.id
    )
    WHERE (f.user_id1 = p_user_id OR f.user_id2 = p_user_id)
    AND f.status = 'accepted'
    ORDER BY u.is_online DESC, u.username ASC;
END$$
DELIMITER ;

-- Stored Procedure: Get User Match History
DELIMITER $$
CREATE PROCEDURE GetUserMatchHistory(
    IN p_user_id CHAR(36),
    IN p_limit INT,
    IN p_offset INT
)
BEGIN
    SELECT
        mh.id,
        mh.room_code,
        mh.category,
        mh.started_at,
        mh.completed_at,
        mh.total_duration_seconds,
        mpr.final_score,
        mpr.best_streak,
        mpr.total_words_found,
        mpr.rank_position,
        (SELECT COUNT(*) FROM match_player_results WHERE match_id = mh.id) as total_players,
        CASE WHEN mh.winner_id = p_user_id THEN 1 ELSE 0 END as is_winner
    FROM match_history mh
    INNER JOIN match_player_results mpr ON mh.id = mpr.match_id
    WHERE mpr.user_id = p_user_id
    ORDER BY mh.started_at DESC
    LIMIT p_limit OFFSET p_offset;
END$$
DELIMITER ;

-- Stored Procedure: Get Global Leaderboard
DELIMITER $$
CREATE PROCEDURE GetGlobalLeaderboard(
    IN p_limit INT,
    IN p_offset INT
)
BEGIN
    SELECT
        u.id,
        u.username,
        u.display_name,
        u.avatar_url,
        us.total_score,
        us.best_score,
        us.best_streak,
        us.games_played,
        us.games_won,
        us.rank_position,
        CASE WHEN us.games_played > 0
            THEN ROUND((us.games_won * 100.0 / us.games_played), 2)
            ELSE 0
        END as win_rate
    FROM users u
    INNER JOIN user_stats us ON u.id = us.user_id
    ORDER BY us.total_score DESC, us.best_streak DESC
    LIMIT p_limit OFFSET p_offset;
END$$
DELIMITER ;

-- Stored Procedure: Clean Expired Tokens and Invitations
DELIMITER $$
CREATE PROCEDURE CleanExpiredData()
BEGIN
    -- Delete expired session tokens
    DELETE FROM session_tokens WHERE expires_at < NOW();

    -- Update expired game invitations
    UPDATE game_invitations
    SET status = 'expired'
    WHERE status = 'pending' AND expires_at < NOW();
END$$
DELIMITER ;
