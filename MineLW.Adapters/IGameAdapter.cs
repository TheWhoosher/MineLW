using MineLW.API.Client;
using MineLW.API.Utils;
using MineLW.Networking;

namespace MineLW.Adapters
{
    public interface IGameAdapter
    {
        GameVersion Version { get; }
        NetworkState NetworkState { get; }

        IClient CreateClient(PlayerProfile profile, NetworkClient client);
    }
}