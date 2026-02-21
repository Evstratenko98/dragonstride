using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public static class NetworkRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureNetworkRuntimeExists()
    {
        if (NetworkManager.Singleton != null)
        {
            return;
        }

        var root = new GameObject("NetworkRuntimeRoot");
        Object.DontDestroyOnLoad(root);

        UnityTransport transport = root.AddComponent<UnityTransport>();
        NetworkManager networkManager = root.AddComponent<NetworkManager>();

        if (networkManager.NetworkConfig == null)
        {
            networkManager.NetworkConfig = new NetworkConfig();
        }

        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.LogLevel = LogLevel.Normal;
    }
}
