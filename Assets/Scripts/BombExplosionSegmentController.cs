using UnityEngine;

public class BombExplosionSegmentController : MonoBehaviour
{
    private BombController owner;

    public void Initialize(BombController bombOwner)
    {
        owner = bombOwner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            if (owner != null)
            {
                owner.TryDamagePlayer(player);
            }

            return;
        }

        GhostController ghost = other.GetComponent<GhostController>();
        if (ghost != null)
        {
            ghost.TryKillByBomb();
        }
    }
}
