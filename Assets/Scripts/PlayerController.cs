using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : NetworkBehaviour
{
    [Header("Network Sync Variables")]
    public NetworkVariable<int> health = new(100);
    public NetworkVariable<int> score = new(0);

    [Header("Movement Settings")]
    [SerializeField] private float moveInput;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private Rigidbody2D Rb2D;

    private void Start()
    {
        if (Rb2D == null) Rb2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        MovePlayer();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        float randomY = Random.Range(-3f, 3f);

        if (IsOwner)
        {
            transform.position = new Vector3(-4.5f, randomY, 0f);
            GetComponent<SpriteRenderer>().color = Color.blue;
        }
        else
        {
            transform.position = new Vector3(4.5f, randomY, 0f);
            GetComponent<SpriteRenderer>().color = Color.red;
        }
    }

    private void MovePlayer()
    {
        moveInput = InputManager.Instance.MoveInput.y;

        if (IsOwner)
            Rb2D.linearVelocity = new Vector2(0f, moveInput * moveSpeed);
        else
            Rb2D.linearVelocity = new Vector2(0f, moveInput * moveSpeed);
    }
}