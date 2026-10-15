using UnityEngine;

public class GameManagerKeeper : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}