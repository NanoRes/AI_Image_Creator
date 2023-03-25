using UnityEngine;

// ReSharper disable once CheckNamespace
public class AnimationController : MonoBehaviour
{
    [SerializeField]
    private ApplicationData applicationData = null;

    private Animation anim = null;

    void Awake()
    {
        anim = this.GetComponent<Animation>();
    }
    
    public void PlayIdle()
    {
        anim.Play(anim.name + "-Idle");
    }

    public void OpenWindow()
    {
        anim.Play("Window-In");
    }

    public void CloseWindow()
    {
        anim.Play("Window-Out");
        applicationData.isWalletOpen = false;
    }
}