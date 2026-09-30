using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum GameState
{
    WaitingForPlayers, // host is in, waiting for player 2
    Serving,           // ball is stuck to the serving paddle
    Playing,           // ball is live
    GameOver
}

/// <summary>
/// Place ONE of these in the scene (with a NetworkObject) - it is spawned automatically on StartHost.
/// The server owns all match logic; clients only read State / ServingClientId.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Ball ballPrefab;

    [Header("Match Settings")]
    [SerializeField] private int requiredPlayers = 2;
    [SerializeField] private float serveTime = 3f;      // GDD: max 3 seconds to aim & shoot
    [SerializeField] private int manaOnConcede = 2;     // GDD: +2 mana when scored on

    public NetworkVariable<GameState> State = new(GameState.WaitingForPlayers);
    public NetworkVariable<ulong> ServingClientId = new(0);

    // Server only
    private readonly Dictionary<ulong, PlayerController> players = new();
    private Ball ball;
    private float serveTimer;

    /// <summary>Paddles may move while waiting = false. Used by PlayerController.</summary>
    public bool CanMove => State.Value == GameState.Serving || State.Value == GameState.Playing;
    public bool ManaCanRegen => State.Value == GameState.Playing;

    private void Awake()
    {
        Instance = this;
    }

    // ---------------------------------------------------------------- Registration

    public void RegisterPlayer(ulong clientId, PlayerController player)
    {
        if (!IsServer) return;
        players[clientId] = player;
        TryStartMatch();
    }

    public void UnregisterPlayer(ulong clientId)
    {
        if (!IsServer) return;
        players.Remove(clientId);

        // someone left mid-match -> go back to waiting
        if (players.Count < requiredPlayers && State.Value != GameState.WaitingForPlayers)
        {
            if (ball != null) ball.Stop();
            State.Value = GameState.WaitingForPlayers;
        }
    }

    // ---------------------------------------------------------------- Match flow

    private void TryStartMatch()
    {
        if (!IsSpawned || State.Value != GameState.WaitingForPlayers) return;
        if (players.Count < requiredPlayers) return;

        if (ball == null)
        {
            ball = Instantiate(ballPrefab);
            ball.NetworkObject.Spawn();
        }

        // Host serves first
        BeginServe(NetworkManager.ServerClientId);
    }

    private void BeginServe(ulong clientId)
    {
        ServingClientId.Value = clientId;
        State.Value = GameState.Serving;
        serveTimer = serveTime;
        ball.AttachTo(players[clientId].transform);
    }

    private void Update()
    {
        if (!IsServer || State.Value != GameState.Serving) return;

        serveTimer -= Time.deltaTime;
        if (serveTimer <= 0f) ReleaseBall(0f); // auto-serve straight when time runs out
    }

    /// <summary>Called from PlayerController's ServerRpc when the serving player presses Space.</summary>
    public void RequestServe(ulong senderClientId, float aim)
    {
        if (!IsServer) return;
        if (State.Value != GameState.Serving || senderClientId != ServingClientId.Value) return;
        ReleaseBall(aim);
    }

    private void ReleaseBall(float aim)
    {
        State.Value = GameState.Playing;
        ball.Launch(aim);
    }

    /// <summary>Called by Ball when it enters a goal. leftSide = host's goal.</summary>
    public void OnGoal(bool leftSide)
    {
        if (!IsServer || State.Value != GameState.Playing) return;

        ulong scoredOn = leftSide
            ? NetworkManager.ServerClientId
            : players.Keys.First(id => id != NetworkManager.ServerClientId);

        var victim = players[scoredOn];
        victim.ServerTakeDamage(1);
        victim.ServerAddMana(manaOnConcede);

        if (victim.health.Value <= 0)
        {
            ball.Stop();
            State.Value = GameState.GameOver;
            return;
        }

        // Ball respawns on the paddle of the player who just conceded
        BeginServe(scoredOn);
    }
}