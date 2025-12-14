using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float rotationDuration = 5f;

    [Header("QTE Settings")]
    [SerializeField] private float QTESuccessThreshold = 0.8f; 
    [SerializeField] private float QTESweetSpotAngle = 30f; 

    public Product _selectedProduct { get; private set; } 

    private Quaternion _initialRotation; 
    private bool _isRotating = false;
    private Coroutine _rotationCoroutine;
    private bool _selectionLocked = false; 
    private Product _waitingProduct; 
    
    private float _qteSuccessTime = 0f; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void OnEnable()
    {
        GameEvents.OnProductReachedInspection += LockSelectionOnArrival;
        GameEvents.OnProductResolved += HandleProductResolved; 
    }

    private void OnDisable()
    {
        GameEvents.OnProductReachedInspection -= LockSelectionOnArrival;
        GameEvents.OnProductResolved -= HandleProductResolved; 
    }
    
    private void HandleProductResolved(Product product)
    {
        if (this == null) return; 
        
        UnlockSelection(); 
        _selectedProduct = null; 
    }
    
    private void LockSelectionOnArrival(Product product)
    {
        if (_isRotating || _selectedProduct != null)
        {
            UnlockSelection();
            _selectedProduct = null;
        }

        _selectionLocked = true;
        _waitingProduct = product; 
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleSelection();

        if (_isRotating)
            HandleRotationInput();
    }

    private void HandleSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Product product = hit.collider.GetComponent<Product>();
            
            if (product != null) 
            {
                if (_isRotating && _selectedProduct != product)
                {
                    return;
                }
                if (_selectionLocked)
                {
                    if (product != _waitingProduct) return; 
                    
                    _waitingProduct = null;
                    _selectionLocked = false; 
                }
                if (_selectedProduct == product && product.HasCompletedInitialInspection) return;
                _selectedProduct = product;
                
                GameEvents.OnProductSelected?.Invoke(product);

                if (product.HasCompletedInitialInspection)
                {
                    GameEvents.OnProductReadyForInspection?.Invoke(product);
                }
                else if (!_isRotating)
                {
                    _initialRotation = product.transform.rotation;
                    _qteSuccessTime = 0f; 
                    _rotationCoroutine = StartCoroutine(RotationSequence(product));
                }
            }
        }
    }

    private void HandleRotationInput()
    {
        if (_selectedProduct == null) return;

        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;

            _selectedProduct.transform.Rotate(Vector3.up, -rotX, Space.World);
            
            if (CheckIfInSweetSpot()) 
            {
                _qteSuccessTime += Time.deltaTime;
            }
        }
    }
    
    private bool CheckIfInSweetSpot()
    {
        if (_selectedProduct == null) return false;
        
        float rotationDifference = Quaternion.Angle(_selectedProduct.transform.rotation, _initialRotation);
        
        return rotationDifference < QTESweetSpotAngle; 
    }

    private IEnumerator RotationSequence(Product product)
    {
        _isRotating = true;

        float timer = 0f;
        while (timer < rotationDuration && _selectedProduct == product && GameManager.Instance.IsWaveActive)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (_selectedProduct != product)
        {
             _isRotating = false;
             yield break; 
        }

        float successRatio = _qteSuccessTime / rotationDuration;
        
        if (successRatio >= QTESuccessThreshold)
        {
            product.MaxDamageTimer *= 1.5f; 
            product.CurrentDamageTimer = Mathf.Min(product.CurrentDamageTimer * 1.5f, product.MaxDamageTimer); 
            GameManager.Instance.AddScore(50); 
        }
        
        yield return StartCoroutine(RotateProductBack(product, _initialRotation));
        
        _isRotating = false; 
        
        if (_selectedProduct == product) 
        {
            product.MarkInitialInspectionComplete();
            GameEvents.OnProductReadyForInspection?.Invoke(product);
        }
    }
    
    private IEnumerator RotateProductBack(Product product, Quaternion targetRotation)
    {
        float backTime = 0.3f; 
        float t = 0;
        Quaternion startRotation = product.transform.rotation;

        while (t < backTime)
        {
            t += Time.deltaTime;
            if(product == null) yield break; 
            product.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t / backTime);
            yield return null;
        }
        if(product != null) product.transform.rotation = targetRotation;
    }
    
    public void UnlockSelection()
    {
        if (_rotationCoroutine != null) 
        {
            StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = null;
        }
        
        _isRotating = false;
        _waitingProduct = null;
        _selectionLocked = false;
    }

    public void SubmitFix(List<ProblemDataSO> chosenProblems)
    {
        if (_selectedProduct == null || chosenProblems == null || GameManager.Instance == null) return;
        GameManager.Instance.HandlePlayerAttemptFix(_selectedProduct, chosenProblems);
    }

    public void SubmitReject()
    {
        if (_selectedProduct == null || GameManager.Instance == null) return;
        GameManager.Instance.HandlePlayerReject(_selectedProduct);
    }
}