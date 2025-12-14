using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int maxHealth = 3;
    public ConveyorLane[] lanes; 
    [SerializeField] private List<ProductTypeSO> availableProducts;

    [Header("Wave Settings")]
    [SerializeField] private float baseWaveTime = 30f;
    [SerializeField] private int fixScore = 100;
    [SerializeField] private int waveCompleteBonus = 500;
    [SerializeField] private float timeBetweenWaves = 1.5f;

    [Header("New Systems")]
    [SerializeField] private int _freezeToolCount = 3;
    [SerializeField] private int _quickScanCount = 3;
    [SerializeField] private float _baseLaneSpeed = 2f; 

    // State
    private int _currentHealth;
    private int _score;
    private int _currentWave;
    private float _waveTimeRemaining;
    private bool _waveActive;
    
    public bool IsWaveActive => _waveActive; 

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
    }

    private void Start() => StartNewGame();

    private void Update()
    {
        if (!_waveActive) return;

        _waveTimeRemaining -= Time.deltaTime;
        float totalTime = Mathf.Max(baseWaveTime - (_currentWave * 1f), 10f);
        GameEvents.OnWaveTimerUpdated?.Invoke(_waveTimeRemaining / totalTime);

        if (_waveTimeRemaining <= 0f)
            TriggerGameOver("Time Expired!");
    }

    public void StartNewGame()
    {
        _currentHealth = maxHealth;
        _score = 0;
        _currentWave = 0;
        _freezeToolCount = 3; 
        _quickScanCount = 3;
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealthUI(_currentHealth, maxHealth);
        GameEvents.OnScoreUpdated?.Invoke(_score);
        GameEvents.OnToolCountUpdated?.Invoke(); 
        StartNextWave();
    }

    private void StartNextWave()
    {
        _currentWave++;
        _waveActive = true;
        _waveTimeRemaining = Mathf.Max(baseWaveTime - (_currentWave * 1f), 10f);
        
        if (lanes == null || availableProducts == null || availableProducts.Count == 0)return;

        if (ProductManager.Instance == null || !ProductManager.Instance.IsReadyToSpawn())return;

        foreach (var lane in lanes)
        {
            if (lane == null) continue;
            
            try
            {
                var type = availableProducts[Random.Range(0, availableProducts.Count)];
                bool isGhost = Random.value < (0.1f + (_currentWave * 0.05f)); 
                lane.SpawnProduct(type, isGhost);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CRITICAL SPAWN EXCEPTION on lane {lane.name}: {ex.Message}. This lane will be empty.");
                continue; 
            }
        }

        GameEvents.OnWaveStarted?.Invoke(_currentWave);
    }
    
    #region Tool/Utility System Methods
    public void UseFreezeTool(Product product)
    {
        if (_freezeToolCount > 0 && product != null && _waveActive && !product.IsTimerFrozen)
        {
            product.IsTimerFrozen = true;
            _freezeToolCount--;
            GameEvents.OnToolCountUpdated?.Invoke();
        }
    }

    public void UseQuickScanTool(Product product)
    {
        if (_quickScanCount > 0 && product != null && _waveActive && !product.HasCompletedInitialInspection)
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.UnlockSelection(); 
            }
            
            product.MarkInitialInspectionComplete();
            GameEvents.OnProductReadyForInspection?.Invoke(product);
            
            _quickScanCount--;
            GameEvents.OnToolCountUpdated?.Invoke();
        }
    }
    #endregion

    #region Interactions
    public void ForceFailProduct(Product product)
    {
        if (!_waveActive || product == null) return;
        
        HandleMistake(product, "Product stability failed and exploded!", 2); 
        CompleteProductFlow(product);
    }
    
    public void HandlePlayerAttemptFix(Product product, List<ProblemDataSO> chosenProblems)
    {
        if (!_waveActive || product == null) return;
        
        product.IsTimerFrozen = false; 

        if (product.IsRejectedProduct)
        {
            TriggerGameOver("CRITICAL FAILURE: Attempted to fix a defective unit!"); 
            CompleteProductFlow(product);
            return;
        }

        VerificationResult result = product.VerifyFixes(chosenProblems);

        if (result.IsSuccessful)
        {
            AddScore(fixScore);
            GameEvents.OnProductSuccess?.Invoke(product); 
        }
        else
        {
            HandleMistake(product, result.ErrorReason, result.MistakeCount);
        }
        
        CompleteProductFlow(product);
    }

    public void HandlePlayerReject(Product product)
    {
        if (!_waveActive || product == null) return;
        
        product.IsTimerFrozen = false; 

        if (product.IsRejectedProduct)
        {
            AddScore(fixScore);
            GameEvents.OnProductSuccess?.Invoke(product); 
        }
        else
        {
            TriggerGameOver("CRITICAL FAILURE: Rejected a valid or fixable unit!"); 
            CompleteProductFlow(product);
            return;
        }
        
        CompleteProductFlow(product);
    }

    private void CompleteProductFlow(Product product)
    {
        if (product == null || lanes == null) return;

        foreach (var lane in lanes)
        {
            if (lane != null && lane.CurrentProduct == product)
            {
                lane.SendToVerifiedLine(); 
                break;
            }
        }
        
        GameEvents.OnProductResolved?.Invoke(product);
        
        bool allProductsAreLeaving = lanes.All(lane => 
            !lane.IsOccupied || 
            (lane.IsOccupied && lane.IsMovingToVerified) 
        );

        if (allProductsAreLeaving)
        {
            StartCoroutine(EndWaveRoutine());
        }
    }
    #endregion

    #region Rules & State
    private void HandleMistake(Product p, string reason, int damage)
    {
        if (damage <= 0) return;
        
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealthUI(_currentHealth, maxHealth);
        GameEvents.OnMistakeMade?.Invoke(p, reason); 

        if (_currentHealth <= 0)
        {
            TriggerGameOver("Too many mistakes!");
        }
    }

    public void AddScore(int amount)
    {
        _score += amount;
        GameEvents.OnScoreUpdated?.Invoke(_score);
    }

    private IEnumerator EndWaveRoutine()
    {
        _waveActive = false;
        AddScore(waveCompleteBonus);
        
        _currentHealth = maxHealth;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealthUI(_currentHealth, maxHealth); 

        yield return new WaitForSeconds(timeBetweenWaves);
        
        if (lanes != null)
        {
            foreach(var l in lanes)
            {
                if (l != null) l.ClearProductImmediate();
            }
        }
        
        StartNextWave();
    }

    private void TriggerGameOver(string reason)
    {
        _waveActive = false;
        if (lanes != null)
        {
            foreach(var l in lanes)
            {
                if (l != null) l.StopConveyor();
            }
        }
        GameEvents.OnGameOver?.Invoke(reason);
    }
    
    public int GetScore() => _score;
    public int GetFreezeToolCount() => _freezeToolCount;
    public int GetQuickScanToolCount() => _quickScanCount;
    
    public float GetBaseLaneSpeed() 
    {
        float difficultyMod = _currentWave * 0.1f;
        return _baseLaneSpeed + difficultyMod;
    }
    #endregion
}