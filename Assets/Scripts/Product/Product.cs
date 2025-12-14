using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(Collider))]
public class Product : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private TextMeshProUGUI _problemText; 

    // Runtime State
    public string ProductName { get; private set; }
    public ProductTypeSO SourceType { get; private set; }
    public List<ProblemDataSO> ActiveProblems { get; private set; } = new List<ProblemDataSO>();
    public bool IsRejectedProduct { get; private set; }
    public bool IsFullyFixed => ActiveProblems.Count == 0;
    public bool HasCompletedInitialInspection { get; private set; } = false;

    // Urgency Fields
    [Header("Urgency Settings")]
    public float MaxDamageTimer = 10f; 
    public float CurrentDamageTimer;
    public bool IsTimerFrozen = false;
    public bool IsCritical => CurrentDamageTimer <= (MaxDamageTimer * 0.3f); 
    
    private void Awake()
    {
        _meshRenderer = _meshRenderer ?? GetComponent<MeshRenderer>();
        _meshFilter = _meshFilter ?? GetComponent<MeshFilter>();
    }

    private void Update()
    {
        if ( GameManager.Instance.IsWaveActive && !IsTimerFrozen && HasCompletedInitialInspection)
        {
            if (CurrentDamageTimer > 0)
            {
                CurrentDamageTimer -= Time.deltaTime;
                
                if (CurrentDamageTimer <= 0)
                {
                    CurrentDamageTimer = 0;
                    
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ForceFailProduct(this);
                    }
                }
            }
        }
    }
    
    public void Initialize(ProductTypeSO type, bool isReject)
    {
        SourceType = type;
        ProductName = type != null ? type.productName : "Unknown";
        IsRejectedProduct = isReject;
        ActiveProblems.Clear();

        if (_meshFilter != null && type?.productMesh != null)
            _meshFilter.sharedMesh = type.productMesh;

        if (_meshRenderer != null && type != null)
        {
            if (IsRejectedProduct && type.ghostMaterial != null) 
                _meshRenderer.sharedMaterial = type.ghostMaterial;
            else if (type.normalMaterial != null) 
                _meshRenderer.sharedMaterial = type.normalMaterial;
        }

        if (!IsRejectedProduct && type?.possibleProblems != null && type.possibleProblems.Count > 0)
        {
            int count = Random.Range(4, Mathf.Min(6, type.possibleProblems.Count));
            var pool = new List<ProblemDataSO>(type.possibleProblems);
            string debugText = "";
            for (int i = 0; i < count; i++)
            {
                if (pool.Count == 0) break;
                int idx = Random.Range(0, pool.Count);
                var prob = pool[idx];
                
                ActiveProblems.Add(prob);
                debugText += $"- {prob.problemName}\n";
                pool.RemoveAt(idx);
            }
            if (_problemText != null) _problemText.text = debugText;
        }
        else if (_problemText != null)
        {
            _problemText.text = IsRejectedProduct ? "DEFECTIVE UNIT" : "No Defects";
        }

        MaxDamageTimer = 10f; 
        CurrentDamageTimer = MaxDamageTimer;
        IsTimerFrozen = false;
        
        HasCompletedInitialInspection = false;
    }
    
    public VerificationResult VerifyFixes(List<ProblemDataSO> proposedFixes)
    {
        if (IsRejectedProduct)
        {
            return new VerificationResult { IsSuccessful = false, MistakeCount = 1, ErrorReason = "Attempted to fix a rejected product." };
        }

        if (proposedFixes == null) proposedFixes = new List<ProblemDataSO>();

        var missingFixes = ActiveProblems.Except(proposedFixes).ToList();
        var wrongFixes = proposedFixes.Except(ActiveProblems).ToList();

        int totalMistakes = missingFixes.Count + wrongFixes.Count;
        bool isSuccessful = totalMistakes == 0;

        if (isSuccessful)
        {
            ActiveProblems.Clear();
            if (SourceType != null && _meshRenderer != null && SourceType.fixedMaterial != null)
                _meshRenderer.sharedMaterial = SourceType.fixedMaterial;
            if (_problemText != null) _problemText.text = "VERIFIED (SUCCESS)";
            return new VerificationResult { IsSuccessful = true, MistakeCount = 0 };
        }
        else
        {
            string reason = "";
            if (missingFixes.Count > 0) reason += $"Missing {missingFixes.Count} Fixes. ";
            if (wrongFixes.Count > 0) reason += $"Wrong {wrongFixes.Count} Fixes.";
            
            if (_problemText != null) _problemText.text = "VERIFICATION FAILED";
            
            return new VerificationResult { 
                IsSuccessful = false, 
                MistakeCount = totalMistakes, 
                ErrorReason = reason.Trim() 
            };
        }
    }
    
    public void MarkInitialInspectionComplete()
    {
        HasCompletedInitialInspection = true;
    }

    public void ResetForPool()
    {
        ProductName = string.Empty;
        SourceType = null;
        ActiveProblems.Clear();
        IsRejectedProduct = false;
        HasCompletedInitialInspection = false; 
        CurrentDamageTimer = 0;
        IsTimerFrozen = false;
        if(_problemText) _problemText.text = "";
    }
}

public struct VerificationResult
{
    public bool IsSuccessful;
    public int MistakeCount;
    public string ErrorReason;
}