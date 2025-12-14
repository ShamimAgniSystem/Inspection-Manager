using UnityEngine;

public class ProductManager : MonoBehaviour
{
    public static ProductManager Instance { get; private set; }
    
    [Header("Dependencies")]
    public GameObject productPrefab; 

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
    }

    public bool IsReadyToSpawn()
    {
        if (productPrefab == null)
        {
            return false;
        }
        return true;
    }

    public GameObject GetPreparedProduct(ProductTypeSO type, bool isRejectedProduct)
    {
        if (!IsReadyToSpawn()) return null;
        
        GameObject productObj = Instantiate(productPrefab);
        
        Product product = productObj.GetComponent<Product>();
        
        if (product != null)
        {
            product.Initialize(type, isRejectedProduct);
        }
        return productObj;
    }

    public void ReturnProduct(GameObject productObject)
    {
        if (productObject != null)
        {
            Destroy(productObject);
        }
    }
}