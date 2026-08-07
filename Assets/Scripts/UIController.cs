using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lapText;
    [SerializeField] private GameObject gameOverDimmed;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private PlayerMove playerMove;

    private void Awake()
    {
        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();

        SetGameOverUI(false);
    }

    private void OnEnable()
    {
        if (playerMove != null)
            playerMove.LapChanged += OnLapChanged;
        GameSession.ScoreChanged += OnScoreChanged;
        GameSession.GameOverChanged += OnGameOverChanged;
        if (restartButton != null)
            restartButton.onClick.AddListener(GameSession.Restart);
    }

    private void OnDisable()
    {
        if (playerMove != null)
            playerMove.LapChanged -= OnLapChanged;
        GameSession.ScoreChanged -= OnScoreChanged;
        GameSession.GameOverChanged -= OnGameOverChanged;

        if (restartButton != null)
            restartButton.onClick.RemoveListener(GameSession.Restart);
    }

    void Start()
    {
        UpdateScoreUI(GameSession.Score, GameSession.BestScore);
    }

    private void OnLapChanged(int lap)
    {
        UpdateScoreUI(GameSession.Score, GameSession.BestScore);
    }

    private void OnScoreChanged(int score, int bestScore)
    {
        UpdateScoreUI(score, bestScore);
    }

    private void OnGameOverChanged(int score, int bestScore)
    {
        UpdateScoreUI(score, bestScore);
        UpdateGameOverUI(score, bestScore);
        SetGameOverUI(true);
    }

    void UpdateScoreUI(int score, int bestScore)
    {
        if (lapText == null)
            return;

        int lap = playerMove != null ? playerMove.CurrentLap : 0;
        lapText.text = $"Score {score}\nLap {lap}  Best {bestScore}";
    }

    private void UpdateGameOverUI(int score, int bestScore)
    {
        if (gameOverScoreText == null)
            return;

        gameOverScoreText.text = $"Score {score}\nBest {bestScore}";
    }

    private void SetGameOverUI(bool isVisible)
    {
        if (gameOverDimmed != null)
            gameOverDimmed.SetActive(isVisible);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(isVisible);
    }
}
