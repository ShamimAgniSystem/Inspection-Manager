using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Core UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject productPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject interactionPanel; 
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseReastert;
    
    //main menu button
    [SerializeField] private Button play;

    [Header("Product Inspection UI")]
    [SerializeField] private TextMeshProUGUI productNameText;
    [SerializeField] private Transform problemsContainer;
    [SerializeField] private GameObject problemButtonPrefab;
    [SerializeField] private Button fixButton;
    [SerializeField] private Button rejectButton;

    [Header("HUD Elements")]
    [SerializeField] private Slider waveTimerSlider;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image healthBar;

    [Header("Game Over Elements")]
    [SerializeField] private TextMeshProUGUI gameOverReasonText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton;

    [Header("Interaction Tools")]
    [SerializeField] private Button freezeToolButton;
    [SerializeField] private TextMeshProUGUI freezeToolCountText;
    [SerializeField] private Button quickScanButton;
    [SerializeField] private TextMeshProUGUI quickScanCountText;

    [Header("Product Urgency UI")]
    [SerializeField] private GameObject damageTimerParent;
    [SerializeField] private Image damageTimerFill;
    [SerializeField] private Color normalTimerColor = Color.green;
    [SerializeField] private Color criticalTimerColor = Color.red;

    private List<ProblemDataSO> _selectedProblems = new List<ProblemDataSO>();
    private List<GameObject> _spawnedButtonObjects = new List<GameObject>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        restartButton?.onClick.AddListener(() => SceneManager.LoadScene(0));
        pauseReastert?.onClick.AddListener(() => SceneManager.LoadScene(0));
        fixButton?.onClick.AddListener(OnFixClicked);
        rejectButton?.onClick.AddListener(OnRejectClicked);
        freezeToolButton?.onClick.AddListener(OnFreezeToolClicked);
        quickScanButton?.onClick.AddListener(OnPauseButtonClicked);
        play?.onClick.AddListener(PlayButtonClicked);
        pauseButton?.onClick.AddListener(OnPauseButtonClicked);
        resumeButton?.onClick.AddListener(OnResumeButtonClicked);
    }

    private void OnEnable()
    {
        GameEvents.OnProductReadyForInspection += ShowProductUI;
        GameEvents.OnWaveTimerUpdated += UpdateTimer;
        GameEvents.OnScoreUpdated += OnScoreUpdated;
        GameEvents.OnGameOver += ShowGameOver;
        GameEvents.OnWaveStarted += OnWaveStarted;
        GameEvents.OnMistakeMade += (p, r) => Debug.Log($"Mistake Made: {r}");
        GameEvents.OnToolCountUpdated += UpdateToolCounts;
    }

    private void OnDisable()
    {
        GameEvents.OnProductReadyForInspection -= ShowProductUI;
        GameEvents.OnWaveTimerUpdated -= UpdateTimer;
        GameEvents.OnScoreUpdated -= OnScoreUpdated;
        GameEvents.OnGameOver -= ShowGameOver;
        GameEvents.OnWaveStarted -= OnWaveStarted;
        GameEvents.OnMistakeMade -= (p, r) => Debug.Log($"Mistake Made: {r}");
        GameEvents.OnToolCountUpdated -= UpdateToolCounts;
    }

    private void Start()
    {
        ShowMainMenu();
        
        HideProductUI();
        HideGameOver();
        UpdateToolCounts();
    }

    #region Main maneu and panels activity 

    
    private void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
    public void PlayButtonClicked()
    {
        Time.timeScale = 1f;
        mainMenuPanel.SetActive(false);
        GameManager.Instance.StartNewGame();
    }
    public void OnPauseButtonClicked()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    public void OnResumeButtonClicked()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }
    #endregion
    
    
    
    private void Update()
    {
        UpdateUrgencyUI(PlayerController.Instance?._selectedProduct);
    }

    #region Tool & Urgency UI

    private void UpdateToolCounts()
    {
        if (GameManager.Instance == null) return;

        int freezeCount = GameManager.Instance.GetFreezeToolCount();
        int quickCount = GameManager.Instance.GetQuickScanToolCount();

        freezeToolCountText?.SetText($"x{freezeCount}");
        quickScanCountText?.SetText($"x{quickCount}");

        Product selected = PlayerController.Instance?._selectedProduct;
        bool productSelected = selected != null;

        freezeToolButton.interactable = freezeCount > 0 && productSelected && (selected?.IsTimerFrozen == false);

        quickScanButton.interactable = quickCount > 0 && productSelected && (selected?.HasCompletedInitialInspection == false);
    }

    private void UpdateUrgencyUI(Product p)
    {
        bool showTimer = p != null && p.HasCompletedInitialInspection && damageTimerParent != null && damageTimerFill != null;
        
        damageTimerParent?.SetActive(showTimer);

        if (showTimer)
        {
            damageTimerFill.fillAmount = p.CurrentDamageTimer / p.MaxDamageTimer;
            damageTimerFill.color = p.IsCritical ? criticalTimerColor : normalTimerColor;
        }
    }

    private void OnFreezeToolClicked()
    {
        var product = PlayerController.Instance?._selectedProduct;
        if (product != null)
        {
            GameManager.Instance?.UseFreezeTool(product);
        }
    }

    

    #endregion

    #region Product Inspection UI

    private void ShowProductUI(Product pb)
    {
        if (pb == null) return;
        productPanel?.SetActive(true);

        _selectedProblems.Clear();
        productNameText?.SetText(pb.ProductName);

        _spawnedButtonObjects.ForEach(Destroy);
        _spawnedButtonObjects.Clear();

        if (pb.SourceType?.possibleProblems == null) return;

        var allTypeProblems = pb.SourceType.possibleProblems;
        foreach (var problem in allTypeProblems)
        {
            if (problemButtonPrefab == null || problemsContainer == null) continue;

            var btnObj = Instantiate(problemButtonPrefab, problemsContainer);
            _spawnedButtonObjects.Add(btnObj);

            Button btn = btnObj.GetComponent<Button>();
            btnObj.GetComponentInChildren<TextMeshProUGUI>()?.SetText(problem.problemName);

            btn?.onClick.AddListener(() => OnProblemButtonClicked(problem, btn));
        }

        fixButton.interactable = false;
        UpdateToolCounts();
    }

    private void OnProblemButtonClicked(ProblemDataSO problem, Button button)
    {
        if (problem == null || button == null) return;

        _selectedProblems.Add(problem);
        button.interactable = false;

        button.image.color = new Color(0.8f, 0.9f, 1f);
        fixButton.interactable = true;
    }

    private void OnFixClicked()
    {
        if (_selectedProblems.Count == 0) return;

        PlayerController.Instance?.SubmitFix(new List<ProblemDataSO>(_selectedProblems));
        HideProductUI();
    }

    private void OnRejectClicked()
    {
        PlayerController.Instance?.SubmitReject();
        HideProductUI();
    }

    public void HideProductUI()
    {
        productPanel?.SetActive(false);
        _selectedProblems.Clear();

        _spawnedButtonObjects.ForEach(Destroy);
        _spawnedButtonObjects.Clear();
    }

    #endregion

    #region HUD Updates

    private void OnWaveStarted(int wave)
    {
        waveText?.SetText($"Wave: {wave}");
    }

    private void UpdateTimer(float normalized)
    {
        waveTimerSlider?.SetValueWithoutNotify(normalized);
    }

    private void OnScoreUpdated(int score)
    {
        scoreText?.SetText($"SCORE: {score}");
    }

    public void UpdateHealthUI(int current, int max)
    {
        if (healthBar == null) return;
        healthBar.fillAmount = (float)current / max;
    }

    #endregion

    #region Game Over

    private void ShowGameOver(string reason)
    {
        gameOverPanel?.SetActive(true);
        gameOverReasonText?.SetText(reason);

        finalScoreText?.SetText($"Final Score: {GameManager.Instance?.GetScore() ?? 0}");

        HideProductUI();
    }

    public void HideGameOver()
    {
        gameOverPanel?.SetActive(false);
        
    }
    #endregion
}