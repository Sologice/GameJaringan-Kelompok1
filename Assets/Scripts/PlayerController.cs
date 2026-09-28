using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : NetworkBehaviour
{
    [Header("Network Sync Variables")]
    public NetworkVariable<int> health = new(5);
    public NetworkVariable<float> mana = new(0);
    [SerializeField] private HealthManaUI HMUI;

    [Header("Mana Settings")]
    [SerializeField] private float maxMana = 10f;
    [SerializeField] private float baseMana = 0.02f;
    [SerializeField] private float rateMana;

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
        if (!IsOwner || mana.Value >= maxMana) return;

        if (baseMana > maxMana - mana.Value)
            rateMana = maxMana - mana.Value;
        else rateMana = baseMana;

        UpdateManaServerRpc(rateMana);
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
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        health.OnValueChanged -= OnHealthChanged;
        mana.OnValueChanged -= OnManaChanged;
 
        inputs.OnTestA -= TestA;
        inputs.OnTestB -= TestB;
    }

    private void OnHealthChanged(int previousValue, int newHealth)
    {
        if (HMUI != null) HMUI.SetHealth(newHealth);
    }

    private void OnManaChanged(float previousValue, float newMana)
    {
        if (HMUI != null) HMUI.SetMana(newMana);
    }

    [ServerRpc]
    private void UpdateHealthServerRpc(int amount)
    {
        health.Value += amount;
    }

    [ServerRpc]
    private void UpdateManaServerRpc(float amount)
    {
        mana.Value += amount;
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

        moveInput = inputs.MoveInput.y;
        Rb2D.linearVelocity = new Vector2(0f, moveInput * moveSpeed);
    }
}