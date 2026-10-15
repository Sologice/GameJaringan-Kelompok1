# Aturan Pengembangan C# Unity  

- **Target Engine:** Unity 6 / URP. Gunakan API modern (misal: FindFirstObjectByType<T>(), BUKAN FindObjectOfType<T>()). 
- **Input System:** Selalu gunakan package UnityEngine.InputSystem baru, bukan legacy Input.GetKeyDown. - **Enkapsulasi Field:** Gunakan [SerializeField] private untuk variabel yang perlu tampil di Inspector. Hindari variabel public terbuka. 
- **Performa Garbage Collection (GC):**   
- DILARANG MEMANGGIL GetComponent<T>(), GameObject.Find(), atau konkatenasi string di dalam method Update() / FixedUpdate().   - Cache semua referensi komponen pada Awake() atau Start(). 
- **Pengolahan Async:** Utamakan async/await atau UniTask dibandingkan IEnumerator (Coroutine) legacy.	