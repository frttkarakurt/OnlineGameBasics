using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (LobbyManager.Instance != null)
        {
            if (sceneName == LobbyManager.Instance.gameSceneName)
            {
                LobbyManager.Instance.ReplacePlayersOnScene();
            }
        }
    }
}
