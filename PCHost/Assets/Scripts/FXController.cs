using UnityEngine;

public class FXController : MonoBehaviour
{
    public float duration = 6f;

    void Start()
    {
        Invoke("StopAllFX", duration);
    }

    void StopAllFX()
    {
        foreach (Transform child in transform)
        {
            var ps = child.GetComponentsInChildren<ParticleSystem>();
            foreach (var p in ps)
                p.Stop();
        }
    }
}
