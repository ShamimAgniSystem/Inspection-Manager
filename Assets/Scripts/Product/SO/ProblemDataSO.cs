using UnityEngine;

[CreateAssetMenu(fileName = "Problem", menuName = "Factory/Problem")]
public class ProblemDataSO : ScriptableObject
{
    public string problemName = "Problem";
    public string description = "Fix this issue";
    public bool isRejectedProblem = false; 
}