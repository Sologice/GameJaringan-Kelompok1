using UnityEngine;

// =====================================================================
//  PlayerSession — sumber tunggal "siapa yang sedang login".
//  Disimpan di PlayerPrefs, jadi otomatis pindah scene (AuthScene →
//  MainScene) dan persist setelah game ditutup.
// =====================================================================
public static class PlayerSession
{
    private const string UserIdKey   = "ACTIVE_USER_ID";
    private const string UsernameKey = "ACTIVE_USERNAME";
    private const string TokenKey    = "ACTIVE_TOKEN";

    /// <summary>userId dari backend. -1 = belum login.</summary>
    public static int UserId => PlayerPrefs.GetInt(UserIdKey, -1);

    /// <summary>Username yang dipakai login. "" = belum login.</summary>
    public static string Username => PlayerPrefs.GetString(UsernameKey, "");

    /// <summary>Token login (placeholder, dari backend). "" = belum login.</summary>
    public static string Token => PlayerPrefs.GetString(TokenKey, "");

    /// <summary>True kalau UserId > 0.</summary>
    public static bool IsLoggedIn => UserId > 0;

    /// <summary>Simpan sesi setelah login/register berhasil.</summary>
    public static void Save(int userId, string username, string token = "")
    {
        PlayerPrefs.SetInt(UserIdKey, userId);
        PlayerPrefs.SetString(UsernameKey, username ?? "");
        PlayerPrefs.SetString(TokenKey, token ?? "");
        PlayerPrefs.Save();
    }

    /// <summary>Logout / reset sesi.</summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(UserIdKey);
        PlayerPrefs.DeleteKey(UsernameKey);
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.Save();
    }
}