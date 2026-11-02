using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private GameSession _session;
    [SerializeField] private TMP_Text _coinsText;

    private void OnEnable()
    {
        if (_session == null)
            return;

        _session.StatsChanged += RefreshHUD;
        RefreshHUD();
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void OnDisable()
    {
        if (_session != null)
            _session.StatsChanged -= RefreshHUD;
    }

    private void RefreshHUD()
    {
        if (_session == null || _coinsText == null)
            return;

        _coinsText.text = _session.Coins.ToString();
    }
}