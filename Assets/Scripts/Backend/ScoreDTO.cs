using System;

// =====================================================================
//  ScoreDTO — Data Transfer Objects untuk endpoint /api/score/*
//  Field name HARUS sama dengan JSON dari backend (case-sensitive).
// =====================================================================

/// <summary>Body request ke POST /api/score/save.</summary>
[Serializable]
public class SaveScoreRequest
{
    public int userId;
    public int score;
}

/// <summary>Response dari POST /api/score/save.</summary>
[Serializable]
public class SaveScoreResponse
{
    public string status;    // "success" | "error"
    public string message;
    public int scoreId;      // id baris di tabel scores, 0 kalau gagal
}

/// <summary>Response dari GET /api/score/highscore/:userId.</summary>
[Serializable]
public class HighScoreResponse
{
    public string status;
    public int userId;
    public int highScore;    // 0 kalau pemain belum pernah punya skor
}