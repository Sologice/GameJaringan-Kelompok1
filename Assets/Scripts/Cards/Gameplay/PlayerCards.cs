using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Hand / deck / playing cards for one player. Add this component to the Player prefab.
///
/// Flow: the owner sends its 6 chosen cards to the server -> the server builds the CardDeck (Clash-Royale
/// cycle) -> every change of the hand is pushed ONLY to the owner (ClientRpc). Pressing J/K/L calls
/// PlayCardServerRpc; the server checks state, mana and CanPlay, spends mana, runs the effect from the
/// CardEffectFactory and cycles the card.
/// </summary>
public class PlayerCards : NetworkBehaviour
{
    /// <summary>The PlayerCards of the local owner (null before connecting).</summary>
    public static PlayerCards Local { get; private set; }

    /// <summary>Fired on every client when anyone plays a card (clientId of the player, card).</summary>
    public static event Action<ulong, CardId> CardPlayed;

    /// <summary>Owner only: hand / next card changed.</summary>
    public event Action HandChanged;

    /// <summary>Owner only: server message such as "Not enough mana".</summary>
    public event Action<string> MessageReceived;

    // What the owner's UI shows. Only valid on the owning client.
    public CardId[] Hand { get; } = new CardId[CardBalance.HandSize];
    public CardId Next { get; private set; }
    public bool HasHand { get; private set; }

    public PlayerController Controller { get; private set; }

    // Server only
    private List<CardId> deckCards;
    private CardDeck deck;
    private bool deckLocked; // the deck can't be swapped once a card was played

    private void Awake()
    {
        Controller = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        // Safety net: a random deck until the owner's real one arrives (no sync yet, the client may not be ready)
        if (IsServer) ServerBuildDeck(DeckSelection.RandomDeck(), sync: false);

        if (IsOwner)
        {
            Local = this;
            SubmitDeckServerRpc(DeckSelection.ToIntArray());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this) Local = null;
    }

    // ---------------------------------------------------------------- Client -> server

    /// <summary>Call from the owner (input or UI button). slot = 0..2.</summary>
    public void RequestPlay(int slot)
    {
        if (!IsOwner) return;
        PlayCardServerRpc(slot);
    }

    [ServerRpc]
    private void SubmitDeckServerRpc(int[] cardIds)
    {
        if (deckLocked) return;

        if (!DeckSelection.TryParse(cardIds, out var cards))
            cards = DeckSelection.RandomDeck(); // invalid / cheated deck

        ServerBuildDeck(cards, sync: true);
    }

    [ServerRpc]
    private void PlayCardServerRpc(int slot)
    {
        ServerTryPlay(slot);
    }

    // ---------------------------------------------------------------- Server

    private void ServerBuildDeck(List<CardId> cards, bool sync)
    {
        deckCards = cards;
        deck = new CardDeck(cards);
        if (sync) SyncHand();
    }

    /// <summary>Called by GameManager when a new match starts.</summary>
    public void ServerResetForMatch()
    {
        if (!IsServer) return;

        deckLocked = false;
        if (deckCards != null) ServerBuildDeck(deckCards, sync: true);
    }

    private void ServerTryPlay(int slot)
    {
        var game = GameManager.Instance;

        if (deck == null || game == null || slot < 0 || slot >= CardBalance.HandSize) return;

        if (!game.CardsPlayable)
        {
            Notify("Cards can only be played while the ball is live");
            return;
        }

        CardId id = deck.GetHand(slot);
        CardDefinition def = CardCatalog.Get(id);

        if (Controller.mana.Value + 0.0001f < def.ManaCost)
        {
            Notify("Not enough mana");
            return;
        }

        CardContext ctx = game.BuildContext(Controller);
        ICardEffect effect = CardEffectFactory.Create(id);

        // Checked before paying: a card that can't work right now costs nothing and is not cycled
        if (!effect.CanPlay(ctx))
        {
            Notify($"Can't use {def.Name} right now");
            return;
        }

        Controller.ServerAddMana(-def.ManaCost);
        effect.Execute(ctx);

        deckLocked = true;
        deck.Play(slot);
        SyncHand();

        AnnouncePlayedClientRpc((int)id);
    }

    private void SyncHand()
    {
        if (deck == null) return;

        SyncHandClientRpc(
            (int)deck.GetHand(0), (int)deck.GetHand(1), (int)deck.GetHand(2), (int)deck.Next,
            OwnerOnly());
    }

    private void Notify(string message)
    {
        NotifyClientRpc(message, OwnerOnly());
    }

    private ClientRpcParams OwnerOnly()
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
    }

    // ---------------------------------------------------------------- Server -> clients

    [ClientRpc]
    private void SyncHandClientRpc(int slot0, int slot1, int slot2, int next, ClientRpcParams rpcParams = default)
    {
        Hand[0] = (CardId)slot0;
        Hand[1] = (CardId)slot1;
        Hand[2] = (CardId)slot2;
        Next = (CardId)next;
        HasHand = true;
        HandChanged?.Invoke();
    }

    [ClientRpc]
    private void NotifyClientRpc(string message, ClientRpcParams rpcParams = default)
    {
        MessageReceived?.Invoke(message);
    }

    [ClientRpc]
    private void AnnouncePlayedClientRpc(int cardId)
    {
        CardPlayed?.Invoke(OwnerClientId, (CardId)cardId);
    }
}
