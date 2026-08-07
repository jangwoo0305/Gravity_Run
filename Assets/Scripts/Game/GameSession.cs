using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameSession : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string BestScoreKey = "GravityRun.BestScore";

    private static GameSession instance;

    private PlayerMove player;
    private float elapsedTime;
    private int lastScore = -1;

    public static event Action<int, int> ScoreChanged;
    public static event Action<int, int> GameOverChanged;

    public static bool IsGameOver { get; private set; }
    public static int Score { get; private set; }
    public static int BestScore { get; private set; }
    public static float Difficulty { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureSessionForActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSessionForActiveScene();
    }

    private static void EnsureSessionForActiveScene()
    {
        if (SceneManager.GetActiveScene().name != GameSceneName)
            return;

        if (instance != null)
            Destroy(instance.gameObject);

        GameObject sessionObject = new GameObject(nameof(GameSession));
        instance = sessionObject.AddComponent<GameSession>();
    }

    private void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
        IsGameOver = false;
        Score = 0;
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        Difficulty = 1f;
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMove>();
        NotifyScoreChanged();
    }

    private void Update()
    {
        if (IsGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space))
                Restart();
            return;
        }

        elapsedTime += Time.deltaTime;
        int lapBonus = player != null ? player.CurrentLap * 100 : 0;
        Score = Mathf.FloorToInt(elapsedTime * 10f) + lapBonus;
        Difficulty = Mathf.Clamp(1f + elapsedTime / 45f + lapBonus / 600f, 1f, 3.5f);

        if (Score != lastScore)
            NotifyScoreChanged();
    }

    public static void ReportGameOver()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            PlayerPrefs.Save();
        }

        Time.timeScale = 0f;
        GameOverChanged?.Invoke(Score, BestScore);
    }

    public static void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneName);
    }

    private static void NotifyScoreChanged()
    {
        lastScoreForInstance();
        ScoreChanged?.Invoke(Score, BestScore);
    }

    private static void lastScoreForInstance()
    {
        if (instance != null)
            instance.lastScore = Score;
    }
}
