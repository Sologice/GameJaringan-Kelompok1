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

    [Header("Arena (used by Bouncer Net and Portal Trap)")]
    [Tooltip("Distance from the center to the top/bottom wall. Sets the net height and the portal spawn range.")]
    [SerializeField] private float arenaHalfHeight = 4.5f;
    [Tooltip("Portals spawn at random x between -this and +this (keep it inside the paddles).")]
    [SerializeField] private float portalHalfWidth = 3f;

    public NetworkVariable<GameState> State = new(GameState.WaitingForPlayers);
    public NetworkVariable<ulong> ServingClientId = new(0);

    // Server only
    private readonly Dictionary<ulong, PlayerController> players = new();
    private readonly List<Ball> decoys = new();
    private Ball ball;
    private float serveTimer;

    /// <summary>Paddles may move while waiting = false. Used by PlayerController.</summary>
    public bool CanMove => State.Value == GameState.Serving || State.Value == GameState.Playing;
    public bool ManaCanRegen => State.Value == GameState.Playing;

    /// <summary>GDD: no cards during the serve window, only while the ball is live.</summary>
    public bool CardsPlayable => State.Value == GameState.Playing;

    /// <summary>The real ball (server only).</summary>
    public Ball MainBall => ball;

    public bool HasDecoys => decoys.Count > 0;

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
            ResetRoundEffects();
            if (ball != null) ball.Stop();
            State.Value = GameState.WaitingForPlayers;
        }
    }

    public PlayerController GetOpponent(PlayerController caster)
    {
        return players.Values.FirstOrDefault(p => p != null && p != caster);
    }

    /// <summary>Everything a card effect needs, built on the server when a card is played.</summary>
    public CardContext BuildContext(PlayerController caster)
    {
        return new CardContext(this, caster, GetOpponent(caster), ball);
    }

    // ---------------------------------------------------------------- Match flow

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[GameManager] Spawned. IsServer={IsServer}");
        if (IsServer)
        {
            CardEffectFactory.ValidateAll();
            TryStartMatch(); // retry in case players registered before this spawned
        }
    }

    private void TryStartMatch()
    {

        if (!IsSpawned || State.Value != GameState.WaitingForPlayers) return;
        if (players.Count < requiredPlayers) return;

        if (ballPrefab == null)
        {
            Debug.LogError("[GameManager] Ball Prefab is not assigned!");
            return;
        }

        if (ball == null)
        {
            ball = Instantiate(ballPrefab);
            ball.NetworkObject.Spawn();
        }

        // fresh cards / paddle effects for the new match
        foreach (var p in players.Values)
            if (p != null) p.ServerResetCardState();

        BeginServe(NetworkManager.ServerClientId);
    }

    private void BeginServe(ulong clientId)
    {
        ResetRoundEffects();

        ServingClientId.Value = clientId;
        State.Value = GameState.Serving;
        serveTimer = serveTime;
        ball.AttachTo(players[clientId].transform);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (State.Value == GameState.WaitingForPlayers)
        {
            SyncPlayersFromNetworkManager();
            TryStartMatch();
            return;
        }

        if (State.Value != GameState.Serving) return;

        serveTimer -= Time.deltaTime;
        if (serveTimer <= 0f) ReleaseBall(0f);
    }

    private void SyncPlayersFromNetworkManager()
    {
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            if (players.ContainsKey(client.ClientId)) continue;
            if (client.PlayerObject == null) continue;

            if (client.PlayerObject.TryGetComponent(out PlayerController pc))
            {
                players[client.ClientId] = pc;
                Debug.Log($"[GameManager] Found player for client {client.ClientId}. Total={players.Count}");
            }
        }
    }

    /// <summary>
    /// Called from PlayerController's ServerRpc when a player presses Space.
    /// Serving: the serving player shoots. Playing: the player whose Magnet Shield caught the ball shoots.
    /// </summary>
    public void RequestServe(ulong senderClientId, float aim)
    {
        if (!IsServer) return;

        if (State.Value == GameState.Serving)
        {
            if (senderClientId != ServingClientId.Value) return;
            ReleaseBall(aim);
            return;
        }

        if (State.Value == GameState.Playing && ball != null
            && players.TryGetValue(senderClientId, out var player) && ball.IsHeldBy(player))
        {
            ball.Launch(aim);
        }
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
            ResetRoundEffects();
            ball.Stop();
            State.Value = GameState.GameOver;
            return;
        }

        // Ball respawns on the paddle of the player who just conceded
        BeginServe(scoredOn);
    }

    // ---------------------------------------------------------------- Card support (server)

    /// <summary>Removes everything cards created during a rally: ball modifiers, decoys, net, portals, slow/stun.</summary>
    private void ResetRoundEffects()
    {
        ClearDecoys();
        if (ball != null) ball.ClearModifiers();

        ArenaEffects.ServerClear();
        if (IsSpawned) ClearArenaVisualsClientRpc();

        foreach (var p in players.Values)
            if (p != null && p.Effects != null) p.Effects.ServerClearTimedDebuffs();
    }

    /// <summary>Multi-Ball: spawns decoy balls that fan out from the real ball and vanish after lifetime.</summary>
    public void ServerSpawnDecoys(int count, float lifetime)
    {
        if (!IsServer || ball == null || ballPrefab == null) return;

        Vector2 heading = ball.Heading;
        float decoySpeed = ball.EffectiveSpeed;

        for (int i = 0; i < count; i++)
        {
            // +20, -20, +40, -40 ...
            float angle = CardBalance.DecoySpreadDegrees * (i % 2 == 0 ? 1f : -1f) * (1 + i / 2);
            Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)heading);

            Ball decoy = Instantiate(ballPrefab, ball.transform.position, Quaternion.identity);
            decoy.NetworkObject.Spawn();
            decoy.InitAsDecoy(dir, decoySpeed, lifetime, Color.white);

            // balls never collide with each other
            Physics2D.IgnoreCollision(decoy.Col, ball.Col);
            foreach (var other in decoys)
                if (other != null) Physics2D.IgnoreCollision(decoy.Col, other.Col);

            decoys.Add(decoy);
        }
    }

    public void UnregisterDecoy(Ball decoy)
    {
        decoys.Remove(decoy);
    }

    public void ClearDecoys()
    {
        if (!IsServer) return;

        foreach (var decoy in new List<Ball>(decoys))
            if (decoy != null && decoy.IsSpawned) decoy.NetworkObject.Despawn(true);

        decoys.Clear();
    }

    /// <summary>Bouncer Net: physics on the server, visuals on every client.</summary>
    public void ServerSpawnNet()
    {
        if (!IsServer) return;

        ArenaEffects.ServerCreateNet(arenaHalfHeight, CardBalance.NetDuration);
        SpawnNetVisualClientRpc(arenaHalfHeight, CardBalance.NetDuration);
    }

    /// <summary>Portal Trap: two random portals, at least 3 units apart.</summary>
    public void ServerSpawnPortals()
    {
        if (!IsServer) return;

        float maxY = Mathf.Max(0.5f, arenaHalfHeight - 1f);
        Vector2 a, b;
        int guard = 0;
        do
        {
            a = new Vector2(Random.Range(-portalHalfWidth, portalHalfWidth), Random.Range(-maxY, maxY));
            b = new Vector2(Random.Range(-portalHalfWidth, portalHalfWidth), Random.Range(-maxY, maxY));
        }
        while (Vector2.Distance(a, b) < 3f && ++guard < 20);

        ArenaEffects.ServerCreatePortals(a, b, CardBalance.PortalDuration);
        SpawnPortalVisualsClientRpc(a, b, CardBalance.PortalDuration);
    }

    [ClientRpc]
    private void SpawnNetVisualClientRpc(float halfHeight, float duration)
    {
        ArenaEffects.ClientShowNet(halfHeight, duration);
    }

    [ClientRpc]
    private void SpawnPortalVisualsClientRpc(Vector2 a, Vector2 b, float duration)
    {
        ArenaEffects.ClientShowPortals(a, b, duration);
    }

    [ClientRpc]
    private void ClearArenaVisualsClientRpc()
    {
        ArenaEffects.ClientClear();
    }
}
