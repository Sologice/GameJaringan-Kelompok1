using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : NetworkBehaviour
{
    [Header("Network Sync Variables")]
    public NetworkVariable<int> health = new(10);      // GDD: 10 HP
    public NetworkVariable<float> mana = new(0);
    [SerializeField] private HealthManaUI HMUI;

    [Header("Mana Settings")]
    [SerializeField] private float maxMana = 10f;
    [SerializeField] private float passiveManaPerSecond = 1f / 3f; // GDD: 1 mana / 3 seconds

    [Header("Movement Settings")]
    [SerializeField] private float moveInput;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D Rb2D;
    [SerializeField] private InputManager inputs;

    private void Update()
    {
        MovePlayer();
    }

    private void FixedUpdate()
    {
        // Passive mana is server-authoritative and only runs while the ball is live
        if (!IsServer) return;
        if (GameManager.Instance == null || !GameManager.Instance.ManaCanRegen) return;

        ServerAddMana(passiveManaPerSecond * Time.fixedDeltaTime);
    }

    public override void OnNetworkSpawn()
    {
        if (Rb2D == null) Rb2D = GetComponent<Rigidbody2D>();
        if (inputs == null) inputs = GetComponent<InputManager>();

        Player targetUI = IsOwnedByServer ? Player.Player1 : Player.Player2;
        foreach (var ui in FindObjectsByType<HealthManaUI>(FindObjectsSortMode.None))
        {
            if (ui.player == targetUI)
            {
                HMUI = ui;
                break;
            }
        }

        base.OnNetworkSpawn();
        health.OnValueChanged += OnHealthChanged;
        mana.OnValueChanged += OnManaChanged;

        inputs.OnTestA += TestA;
        inputs.OnTestB += TestB;
        inputs.OnServe += OnServePressed;

        if (HMUI != null)
        {
            HMUI.SetHealth(health.Value);
            HMUI.SetMana(mana.Value);
        }

        float randomY = Random.Range(-3f, 3f);

        if (IsOwnedByServer)
            transform.position = new Vector3(-4.5f, randomY, 0f);
        else
            transform.position = new Vector3(4.5f, randomY, 0f);

        if (IsOwner)
            GetComponent<SpriteRenderer>().color = Color.blue;
        else
            GetComponent<SpriteRenderer>().color = Color.red;

        // Tell the GameManager this player exists. Match starts once 2 are registered.
        if (IsServer && GameManager.Instance != null)
            GameManager.Instance.RegisterPlayer(OwnerClientId, this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        health.OnValueChanged -= OnHealthChanged;
        mana.OnValueChanged -= OnManaChanged;

        inputs.OnTestA -= TestA;
        inputs.OnTestB -= TestB;
        inputs.OnServe -= OnServePressed;

        if (IsServer && GameManager.Instance != null)
            GameManager.Instance.UnregisterPlayer(OwnerClientId);
    }

    private void OnHealthChanged(int previousValue, int newHealth)
    {
        if (HMUI != null) HMUI.SetHealth(newHealth);
    }

    private void OnManaChanged(float previousValue, float newMana)
    {
        if (HMUI != null) HMUI.SetMana(newMana);
    }

    // ---------------------------------------------------------------- Server-side helpers
    // Called by Ball / GameManager (they already run on the server)

    public void ServerAddMana(float amount)
    {
        if (!IsServer) return;
        mana.Value = Mathf.Clamp(mana.Value + amount, 0f, maxMana);
    }

    public void ServerTakeDamage(int amount)
    {
        if (!IsServer) return;
        health.Value = Mathf.Max(0, health.Value - amount);
    }

    // ---------------------------------------------------------------- RPCs

    [ServerRpc]
    private void UpdateHealthServerRpc(int amount)
    {
        health.Value += amount;
    }

    [ServerRpc]
    private void UpdateManaServerRpc(float amount)
    {
        mana.Value = Mathf.Clamp(mana.Value + amount, 0f, maxMana);
    }

    [ServerRpc]
    private void RequestServeServerRpc(float aim, ServerRpcParams rpcParams = default)
    {
        // Server validates that the sender really is the serving player
        GameManager.Instance.RequestServe(rpcParams.Receive.SenderClientId, aim);
    }

    // ---------------------------------------------------------------- Input handlers

    private void OnServePressed()
    {
        if (!IsOwner) return;
        RequestServeServerRpc(inputs.MoveInput.y); // W/S held at release = serve angle
    }

    public void TestA()
    {
        if (!IsOwner || health.Value < 1) return;
        UpdateHealthServerRpc(-1);
    }

    private void TestB()
    {
        if (!IsOwner || mana.Value < 5) return;
        UpdateManaServerRpc(-5);
    }

    private void MovePlayer()
    {
        if (!IsOwner || inputs == null) return;

        // Frozen until both players are in the match (and after game over)
        if (GameManager.Instance == null || !GameManager.Instance.CanMove)
        {
            Rb2D.linearVelocity = Vector2.zero;
            return;
        }

        moveInput = inputs.MoveInput.y;
        Rb2D.linearVelocity = new Vector2(0f, moveInput * moveSpeed);
    }
}