using System;
using System.Collections.Generic;

// =====================================================================
//  LeaderboardDTO — JSON wrapper for GET /api/leaderboard
//  Mirrors ScoreDTO.cs: shape of the JSON must match the fields exactly
//  (case-sensitive for JsonUtility).
// =====================================================================

/// <summary>One row of the leaderboard as returned by the backend.</summary>
[Serializable]
public class LeaderboardEntry
{
    public string username;
    public int    highScore;
}

/// <summary>Wrapper because JsonUtility cannot parse a top-level JSON array.</summary>
[Serializable]
public class LeaderboardResponse
{
    public string status;                              // "success" | "error"
    public List<LeaderboardEntry> data = new();        // populated by FromJson
}