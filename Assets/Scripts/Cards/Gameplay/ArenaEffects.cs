using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates the Bouncer Net and Portal Trap with plain GameObjects (no prefabs, no extra network objects).
/// The server builds the colliders (physics happens only there); every machine, including the host,
/// builds the visuals when GameManager sends a ClientRpc.
/// </summary>
public static class ArenaEffects
{
    private static GameObject serverNet;
    private static GameObject serverPortalA;
    private static GameObject serverPortalB;
    private static readonly List<GameObject> visuals = new();

    private static Sprite squareSprite;
    private static Sprite circleSprite;

    public static bool ServerNetActive => serverNet != null;
    public static bool ServerPortalsActive => serverPortalA != null;

    // ---------------------------------------------------------------- Server (physics)

    public static void ServerCreateNet(float halfHeight, float duration)
    {
        if (serverNet != null) Object.Destroy(serverNet);

        serverNet = new GameObject("BouncerNet (server)");
        serverNet.transform.position = Vector3.zero;

        var box = serverNet.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.3f, halfHeight * 2f + 2f); // a bit taller than the arena so nothing slips past
        box.sharedMaterial = new PhysicsMaterial2D("NetBounce") { bounciness = 1f, friction = 0f };

        Object.Destroy(serverNet, duration);
    }

    public static void ServerCreatePortals(Vector2 a, Vector2 b, float duration)
    {
        ServerClearPortals();

        serverPortalA = CreatePortalCollider("PortalA (server)", a);
        serverPortalB = CreatePortalCollider("PortalB (server)", b);

        var pa = serverPortalA.GetComponent<ArenaPortal>();
        var pb = serverPortalB.GetComponent<ArenaPortal>();
        pa.Partner = pb;
        pb.Partner = pa;

        Object.Destroy(serverPortalA, duration);
        Object.Destroy(serverPortalB, duration);
    }

    private static GameObject CreatePortalCollider(string name, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.position = position;

        var circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = CardBalance.PortalRadius;

        go.AddComponent<ArenaPortal>();
        return go;
    }

    private static void ServerClearPortals()
    {
        if (serverPortalA != null) Object.Destroy(serverPortalA);
        if (serverPortalB != null) Object.Destroy(serverPortalB);
    }

    public static void ServerClear()
    {
        if (serverNet != null) Object.Destroy(serverNet);
        ServerClearPortals();
    }

    // ---------------------------------------------------------------- Clients (visuals only)

    public static void ClientShowNet(float halfHeight, float duration)
    {
        var go = CreateVisual("BouncerNet (visual)", Vector2.zero, GetSquare(), new Color(0.4f, 0.9f, 1f, 0.85f));
        go.transform.localScale = new Vector3(0.3f, halfHeight * 2f + 2f, 1f);
        Object.Destroy(go, duration);
    }

    public static void ClientShowPortals(Vector2 a, Vector2 b, float duration)
    {
        float diameter = CardBalance.PortalRadius * 2f;

        var goA = CreateVisual("PortalA (visual)", a, GetCircle(), new Color(1f, 0.6f, 0.1f, 0.9f));
        var goB = CreateVisual("PortalB (visual)", b, GetCircle(), new Color(0.2f, 0.6f, 1f, 0.9f));
        goA.transform.localScale = new Vector3(diameter, diameter, 1f);
        goB.transform.localScale = new Vector3(diameter, diameter, 1f);

        Object.Destroy(goA, duration);
        Object.Destroy(goB, duration);
    }

    public static void ClientClear()
    {
        foreach (var go in visuals)
            if (go != null) Object.Destroy(go);
        visuals.Clear();
    }

    private static GameObject CreateVisual(string name, Vector2 position, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 5;

        visuals.Add(go);
        return go;
    }

    // ---------------------------------------------------------------- Generated sprites (1 unit big)

    private static Sprite GetSquare()
    {
        if (squareSprite != null) return squareSprite;

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return squareSprite;
    }

    private static Sprite GetCircle()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 64;
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        float r = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d <= r - 1f ? 1f : 0f));
        }

        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
