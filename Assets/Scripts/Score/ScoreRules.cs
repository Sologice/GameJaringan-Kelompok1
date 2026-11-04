using UnityEngine;

/// <summary>
/// Semua nilai poin di SATU tempat. Score adalah efek samping gameplay yang murni
/// akumulatif — TIDAK menentukan menang/kalah. Menang/kalah tetap 100% ditentukan
/// oleh HP lawan di GameManager.OnGoal() (victim.health.Value <= 0).
///
/// Kalau designer ingin tuning poin, cukup ubah konstanta di sini — tidak perlu
/// menyentuh Ball.cs, GameManager.cs, PlayerCards.cs, dll.
/// </summary>
public static class ScoreRules
{
    // ================================================================
    //  KONSTANTA POIN (ubah di sini untuk tuning)
    // ================================================================

    /// <summary>Bola memantul dari paddle sendiri (deflect).</summary>
    public const int PaddleDeflect = 10;

    /// <summary>Berhasil memainkan kartu (skill) — hanya jika CanPlay lolos & mana cukup.</summary>
    public const int CardPlayed = 15;

    /// <summary>Mencetak goal (lawan kehilangan 1 HP).</summary>
    public const int GoalScored = 100;

    /// <summary>Bonus tambahan saat menang (HP lawan habis) — ditambahkan SETELAH GoalScored terakhir.</summary>
    public const int MatchWinBonus = 250;

    /// <summary>Skor minimum setelah match — untuk sanity check.</summary>
    public const int MinScore = 0;

    /// <summary>Skor maksimum wajar — untuk validasi client sebelum kirim ke backend.</summary>
    public const int MaxReasonableScore = 100_000;

    // ================================================================
    //  HELPER — baca nama poin untuk log / tooltip
    // ================================================================

    /// <summary>Nama event untuk log/analytics.</summary>
    public enum ScoreEvent
    {
        PaddleDeflect,
        CardPlayed,
        GoalScored,
        MatchWinBonus
    }

    /// <summary>Kembalikan poin untuk event tertentu. Berguna untuk UI/analytics.</summary>
    public static int For(ScoreEvent evt)
    {
        switch (evt)
        {
            case ScoreEvent.PaddleDeflect: return PaddleDeflect;
            case ScoreEvent.CardPlayed:    return CardPlayed;
            case ScoreEvent.GoalScored:    return GoalScored;
            case ScoreEvent.MatchWinBonus: return MatchWinBonus;
            default:                        return 0;
        }
    }

    /// <summary>Nama event dalam bahasa Indonesia untuk log yang mudah dibaca dosen/asisten.</summary>
    public static string Name(ScoreEvent evt)
    {
        switch (evt)
        {
            case ScoreEvent.PaddleDeflect: return "Deflect bola";
            case ScoreEvent.CardPlayed:    return "Main kartu";
            case ScoreEvent.GoalScored:    return "Goal";
            case ScoreEvent.MatchWinBonus: return "Bonus menang";
            default:                        return "?";
        }
    }

    // ================================================================
    //  VALIDASI — dipakai ScoreManager sebelum kirim ke backend
    // ================================================================

    /// <summary>
    /// Sanity check skor sebelum dikirim ke server. True = wajar.
    /// Bukan anti-cheat serius (server tetap harus validasi sendiri),
    /// hanya mencegah nilai negatif / overflow / NaN.
    /// </summary>
    public static bool IsPlausible(int score)
    {
        return score >= MinScore && score <= MaxReasonableScore;
    }

    /// <summary>Clamp skor ke rentang wajar sebelum dikirim ke backend.</summary>
    public static int Sanitize(int score)
    {
        return Mathf.Clamp(score, MinScore, MaxReasonableScore);
    }

    // ================================================================
    //  FORMAT TAMPILAN — dipakai GameOverUI / ScoreHUD
    // ================================================================

    /// <summary>Contoh: 1170 → "1,170".</summary>
    public static string Format(int score)
    {
        return score.ToString("N0");
    }

    /// <summary>Label skor untuk GameOverUI, contoh: "Match Score: 1,170".</summary>
    public static string FormatMatchScore(int score)
    {
        return $"Match Score: {Format(score)}";
    }

    /// <summary>
    /// Label high score untuk GameOverUI, contoh:
    /// "Personal High Score: 1,320 (NEW RECORD!)" jika memecahkan rekor,
    /// atau "Personal High Score: 1,500" jika belum.
    /// </summary>
    public static string FormatHighScore(int highScore, bool isNewRecord = false)
    {
        return isNewRecord
            ? $"Personal High Score: {Format(highScore)}  <color=#7CFF8A>(NEW RECORD!)</color>"
            : $"Personal High Score: {Format(highScore)}";
    }

    // ================================================================
    //  DEBUG LOGGING — matikan di build final via define atau flag
    // ================================================================

    /// <summary>Set true untuk log setiap penambahan poin (berguna saat demo/QA).</summary>
    public static bool VerboseLog { get; set; } = false;

    /// <summary>Log event penambahan skor ke Console (hanya jika VerboseLog = true).</summary>
    public static void Log(int playerClientId, ScoreEvent evt, int added, int totalAfter)
    {
        if (!VerboseLog) return;
        Debug.Log($"[Score] Client {playerClientId} +{added} ({Name(evt)}) → total {totalAfter}");
    }
}