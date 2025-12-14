using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProductType", menuName = "Factory/ProductType")]
public class ProductTypeSO : ScriptableObject
{
    public string productName = "Product";
    public Mesh productMesh;
    public Material normalMaterial;
    public Material ghostMaterial;
    public Material fixedMaterial;
    public List<ProblemDataSO> possibleProblems = new List<ProblemDataSO>();
}