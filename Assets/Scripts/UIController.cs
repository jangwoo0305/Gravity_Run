using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lapText;
    [SerializeField] private PlayerMove playerMove;

    private void Awake()
    {
        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();
    }

    private void OnEnable()
    {
        if (playerMove != null)
            playerMove.LapChanged += OnLapChanged;
    }

    private void OnDisable()
    {
        if (playerMove != null)
            playerMove.LapChanged -= OnLapChanged;
    }

    void Start()
    {
        UpdateLapUI(playerMove != null ? playerMove.CurrentLap : 0);
    }

    private void OnLapChanged(int lap)
    {
        UpdateLapUI(lap);
    }

    void UpdateLapUI(int lap)
    {
        if (lapText == null)
            return;

        lapText.text = $"Lap : {lap}";
    }
}
