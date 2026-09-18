using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{

    [Header("Player Settings")]
    [SerializeField] private GameObject Player1;
    [SerializeField] private GameObject Player2;
    [SerializeField] private Rigidbody2D Rb2D1;
    [SerializeField] private Rigidbody2D Rb2D2;

    [Header("Movement Settings")]
    [SerializeField] private float moveInput;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;

    private void Start()
    {
        if (Rb2D1 == null) Rb2D1 = Player1.GetComponent<Rigidbody2D>();
        if (Rb2D2 == null) Rb2D2 = Player2.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        MovePlayer();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            float randomY = Random.Range(-3f, 3f);
            Player1.transform.position = new Vector3(-4.5f, randomY, 0f);
            Player2.transform.position = new Vector3(4.5f, randomY, 0f);
            Player1.GetComponent<SpriteRenderer>().color = Color.blue;
            Player2.GetComponent<SpriteRenderer>().color = Color.red;
        }
        else
        {
            Player2.GetComponent<SpriteRenderer>().color = Color.blue;
            Player1.GetComponent<SpriteRenderer>().color = Color.red;
        }
    }

    private void MovePlayer()
    {
        moveInput = InputManager.Instance.MoveInput.y;

        if (IsOwner)
            Rb2D1.linearVelocity = new Vector2(0f, moveInput * moveSpeed);
        else
            Rb2D2.linearVelocity = new Vector2(0f, moveInput * moveSpeed);
    }
}