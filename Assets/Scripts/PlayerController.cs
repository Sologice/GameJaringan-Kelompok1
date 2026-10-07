using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PaddleEffects))]
[RequireComponent(typeof(PlayerCards))]
public class PlayerController : NetworkBehaviour
{
    [Header("Network Sync Variables")]
    public const int StartHealth = 10;                 // GDD: 10 HP
    public NetworkVariable<int> health = new(StartHealth);
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

    /// <summary>Card state of this paddle (length / slow / stun / magnet).</summary>
    public PaddleEffects Effects { get; private set; }

    /// <summary>Hand / deck of this player.</summary>
    public PlayerCards Cards { get; private set; }

    /// <summary>x side of this paddle: -1 = left (host), +1 = right (client).</summary>
    public float SideSign => IsOwnedByServer ? -1f : 1f;

    private void Awake()
    {
        Effects = GetComponent<PaddleEffects>();
        Cards = GetComponent<PlayerCards>();

        if (Effects == null) Debug.LogError("[PlayerController] Add a PaddleEffects component to the Player prefab.");
        if (Cards == null) Debug.LogError("[PlayerController] Add a PlayerCards component to the Player prefab.");
    }

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
        if (inputs != null) inputs.ApplyDeviceFilter();

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
        inputs.OnCard += OnCardPressed;

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
        inputs.OnCard -= OnCardPressed;

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

    /// <summary>Rematch: full health, empty mana, paddle back to the middle.</summary>
    public void ServerResetForRematch()
    {
        if (!IsServer) return;
        health.Value = StartHealth;
        mana.Value = 0f;
        ResetPositionClientRpc();
    }

    [ClientRpc]
    private void ResetPositionClientRpc()
    {
        // The owner moves its own paddle (client authoritative), so every client just sets the y here
        // and the owner's NetworkTransform pushes it. Cheap and good enough for a rematch.
        if (IsOwner) transform.position = new Vector3(transform.position.x, 0f, 0f);
    }

    /// <summary>New match: clear paddle effects and rebuild the deck order.</summary>
    public void ServerResetCardState()
    {
        if (!IsServer) return;
        if (Effects != null) Effects.ServerResetAll();
        if (Cards != null) Cards.ServerResetForMatch();
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
        // Server validates that the sender really is the serving player (or the one holding a magnet-caught ball)
        GameManager.Instance.RequestServe(rpcParams.Receive.SenderClientId, aim);
    }

    // ---------------------------------------------------------------- Input handlers

    private void OnServePressed()
    {
        if (!IsOwner) return;
        RequestServeServerRpc(inputs.MoveInput.y); // W/S held at release = serve angle
    }

    private void OnCardPressed(int slot)
    {
        if (!IsOwner || Cards == null) return;
        Cards.RequestPlay(slot);
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

        // Heavy Ball stun
        if (Effects != null && Effects.Stunned.Value)
        {
            Rb2D.linearVelocity = Vector2.zero;
            return;
        }

        // Sticky Ice slow
        float speedMultiplier = Effects != null ? Effects.SpeedMultiplier.Value : 1f;

        moveInput = inputs.MoveInput.y;
        Rb2D.linearVelocity = new Vector2(0f, moveInput * moveSpeed * speedMultiplier);
    }
}