using UnityEngine;

public class AvatarAnimatorControl : MonoBehaviour
{
    public Animator anim;

    public void PlayNod() {
        anim.SetTrigger("DoAction");
    }

    public void StartTalking() {
        anim.SetBool("IsTalking", true);
    }

    public void StopTalking() {
        anim.SetBool("IsTalking", false);
    }

    public void SetVisible(bool state) {
        anim.SetBool("IsVisible", state);
    }

    public void SetIdleTimer(float time) {
        anim.SetFloat("IdleTimer", time);
    }

    public void SetRandomIdleState(int value) {
        anim.SetInteger("RandomIdle", value);
    }

}
