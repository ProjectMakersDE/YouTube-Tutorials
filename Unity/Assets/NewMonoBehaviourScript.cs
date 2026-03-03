using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private async void Start()
    {
        HorizonApp.Initialize();
        var server = new HorizonServer();
        var connection = await server.Connect();
        
        var _anonymousToken = "";
        var _successLogin = false;
        
        if (UserManager.Instance != null && UserManager.Instance.HasCachedAnonymousToken())
        {
            string cachedToken = PlayerPrefs.GetString("horizOn_AnonymousToken", "");
            if (!string.IsNullOrEmpty(cachedToken))
            {
                _anonymousToken = cachedToken;
            }
        }

        if (string.IsNullOrEmpty(_anonymousToken))
        {
            var success = await UserManager.Instance.SignUpAnonymous("PlayerName");

            if (success)
            {
                Debug.Log("Successfully created anonymous token");
            }
            else
            {
                Debug.Log("Failed to create anonymous token");
            }
        }
        else
        {
            var success = await UserManager.Instance.SignInAnonymous(_anonymousToken);

            if (success)
            {
                Debug.Log("Successfully load anonymous user");
            }
            else
            {
                Debug.Log("Failed to load anonymous user");
            }
        }

        if (UserManager.Instance.IsSignedIn)
        {
            var user = UserManager.Instance.CurrentUser;
            Debug.Log($"Welcome, {user.DisplayName}!");
            
            UserManager.Instance.SignOut();
        }
    }
}