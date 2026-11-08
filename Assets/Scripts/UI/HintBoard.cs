using Player;
using UnityEngine;

public class HintBoard : MonoBehaviour
{
    [SerializeField] private PlayerHealth player;
    [SerializeField] private GameObject panel;
    [SerializeField] private BoxCollider2D zone;

    private void Update()
    {
        if (player == null || panel == null || zone == null)
            return;

        bool show = !player.IsDead &&
            zone.OverlapPoint(player.transform.position);

        if (panel.activeSelf != show)
            panel.SetActive(show);
    }
}