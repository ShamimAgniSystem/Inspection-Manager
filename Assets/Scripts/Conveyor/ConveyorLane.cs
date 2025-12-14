using System.Collections;
using UnityEngine;

public class ConveyorLane : MonoBehaviour
{
    [Header("Settings")]
    public Transform spawnPoint;
    public Transform inspectionPoint;
    public Transform verifiedPoint;
    [SerializeField] private float speed = 2f;

    public Product CurrentProduct { get; private set; }
    public bool IsOccupied => CurrentProduct != null;

    private bool _movingToVerified = false; 
    public bool IsMovingToVerified => _movingToVerified;
    
    private bool _hasReachedInspection = false;

    public void SpawnProduct(ProductTypeSO type, bool isRejectedProduct)
    {
        if (IsOccupied) return;

        if (ProductManager.Instance == null)
            return;
        
        GameObject go = ProductManager.Instance.GetPreparedProduct(type, isRejectedProduct);
        
        if (go == null) 
            return;

        go.transform.position = spawnPoint.position;
        go.transform.rotation = Quaternion.identity;

        Product newProduct = go.GetComponent<Product>();
        
        if (newProduct == null)
        {
            ProductManager.Instance.ReturnProduct(go); 
            return;
        }

        CurrentProduct = newProduct;
        
        _movingToVerified = false;
        _hasReachedInspection = false;
        
        if (GameManager.Instance != null)
        {
            SetSpeed(GameManager.Instance.GetBaseLaneSpeed()); 
        }
    }

    private void Update()
    {
        if (CurrentProduct == null) return;

        Vector3 target = _movingToVerified ? verifiedPoint.position : inspectionPoint.position;
        
        CurrentProduct.transform.position = Vector3.MoveTowards(CurrentProduct.transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(CurrentProduct.transform.position, target) < 0.05f)
        {
            if (!_movingToVerified)
            {
                if (!_hasReachedInspection)
                {
                    _hasReachedInspection = true;
                    GameEvents.OnProductReachedInspection?.Invoke(CurrentProduct);
                    
                    StopConveyor(); 
                }
            }
            else
            {
                StartCoroutine(ReturnAfterDelay());
            }
        }
    }

    public void SendToVerifiedLine()
    {
        if (CurrentProduct == null) return;
        _movingToVerified = true;
        SetSpeed(4f); 
    }

    private IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        ClearProductImmediate();
    }

    public void ClearProductImmediate()
    {
        if (CurrentProduct != null)
        {
            if (ProductManager.Instance != null)
            {
                ProductManager.Instance.ReturnProduct(CurrentProduct.gameObject);
            }
            else
            {
                Destroy(CurrentProduct.gameObject); 
            }
            
            CurrentProduct = null;
            _movingToVerified = false;
            _hasReachedInspection = false;
            
            SetSpeed(0f); 
        }
    }

    public void SetSpeed(float newSpeed) => speed = newSpeed;
    public void StopConveyor() => speed = 0f;
}