using System;

// =====================================================================
//  AuthDTO — Data Transfer Objects untuk /api/register & /api/login.
// =====================================================================

// ---------- REGISTER ----------

[Serializable]
public class RegisterRequest
{
    public string username;
    public string password;
}

[Serializable]
public class RegisterResponse
{
    public string status;    // "success" | "error"
    public string message;
    public int userId;       // id user yang baru dibuat, 0 kalau gagal
}

// ---------- LOGIN ----------

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public string status;
    public string message;
    public string token;     // placeholder, bisa diganti JWT nanti
    public int userId;       // 0 kalau gagal
}