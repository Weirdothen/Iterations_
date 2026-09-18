using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// A network-serializable struct that represents a single player's state in the lobby.
/// 
/// EXTENDING LATER:
/// To add more data (e.g. player name, selected character), simply add fields here.
/// Example:
///   public FixedString32Bytes PlayerName;
///   public int SelectedCharacterIndex;
/// The NetworkList will automatically sync the new fields to all clients.
/// </summary>
public struct LobbyPlayerState : INetworkSerializable, System.IEquatable<LobbyPlayerState>
{
    public ulong ClientId;
    public bool IsReady;

    // Reserved for future use — add more fields here as needed
    // public FixedString32Bytes PlayerName;
    // public int SelectedCharacterIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref IsReady);

        // Add new fields here in the same order when extending:
        // serializer.SerializeValue(ref PlayerName);
        // serializer.SerializeValue(ref SelectedCharacterIndex);
    }

    public bool Equals(LobbyPlayerState other)
    {
        return ClientId == other.ClientId && IsReady == other.IsReady;
    }
}
