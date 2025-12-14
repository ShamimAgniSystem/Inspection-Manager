using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float focusDuration = 1.0f;
    [SerializeField] private float idleDuration = 0.8f;
    public AnimationCurve easeCurve; // Made public/serialized for assignment

    [Header("Camera Positions (Set in Inspector)")]
    public Transform idlePosition;
    public Transform[] focusPositions; 
    
    // Dependencies
    private ConveyorLane[] _lanes; 
    private Coroutine _cameraRoutine;
    
    private void Start()
    {
        // FIX: Get lanes directly from GameManager to guarantee order consistency
        if (GameManager.Instance != null)
        {
            _lanes = GameManager.Instance.lanes;
        }
        
        if (_lanes == null || _lanes.Length == 0)
        {
            Debug.LogError("CameraController ERROR: No ConveyorLanes found in GameManager.");
            return;
        }

        // Check if the number of focus points matches the number of lanes
        if (focusPositions.Length != _lanes.Length)
        {
            Debug.LogError("CAMERA SETUP ERROR: The number of Focus Positions (" + focusPositions.Length + 
                           ") does not match the number of Conveyor Lanes (" + _lanes.Length + 
                           "). Check your GameManager.lanes order and focusPositions array size!");
            return;
        }

        if (idlePosition == null)
        {
            Debug.LogError("CameraController setup incomplete: Assign Idle Position.");
            return;
        }
        
        // Start in the idle position
        transform.position = idlePosition.position;
        transform.rotation = idlePosition.rotation;
    }

    private void OnEnable()
    {
        GameEvents.OnProductReachedInspection += OnProductReachedInspection;
        GameEvents.OnProductResolved += OnProductResolved;
    }

    private void OnDisable()
    {
        GameEvents.OnProductReachedInspection -= OnProductReachedInspection;
        GameEvents.OnProductResolved -= OnProductResolved;
    }

    private void OnProductReachedInspection(Product product)
    {
        if (product == null || _lanes == null) return;
        
        ConveyorLane lane = _lanes.FirstOrDefault(l => l.CurrentProduct == product);
        if (lane == null) return;

        // Use Array.IndexOf on the consistent _lanes array
        int laneIndex = System.Array.IndexOf(_lanes, lane);
        
        if (laneIndex >= 0 && laneIndex < focusPositions.Length)
        {
            Transform targetFocus = focusPositions[laneIndex];
            
            if (_cameraRoutine != null) StopCoroutine(_cameraRoutine);
            _cameraRoutine = StartCoroutine(MoveCameraRoutine(targetFocus, focusDuration));
        }
    }

    private void OnProductResolved(Product resolvedProduct)
    {
        if (_cameraRoutine != null) StopCoroutine(_cameraRoutine);
        _cameraRoutine = StartCoroutine(CheckNextFocusRoutine());
    }

    private IEnumerator CheckNextFocusRoutine()
    {
        if (_lanes == null || _lanes.Length != focusPositions.Length) yield break; // Safety check

        // Check lanes sequentially (0, 1, 2...)
        yield return new WaitForSeconds(0.2f); 

        for (int i = 0; i < _lanes.Length; i++)
        {
            ConveyorLane lane = _lanes[i];
            
            // Check if the product is occupied AND stationary (stopped at inspection)
            /*if (lane.IsOccupied && lane.CurrentProduct != null && lane.speed < 0.05f) 
            {
                // Found the next product to inspect. Focus on its position.
                Transform targetFocus = focusPositions[i]; 
                yield return StartCoroutine(MoveCameraRoutine(targetFocus, focusDuration));
                yield break; 
            }*/
        }
        
        // If no products are waiting, return to idle.
        yield return StartCoroutine(MoveCameraRoutine(idlePosition, idleDuration));
    }

    private IEnumerator MoveCameraRoutine(Transform targetTransform, float duration)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            float easedT = easeCurve.Evaluate(t); 

            transform.position = Vector3.Lerp(startPos, targetTransform.position, easedT);
            transform.rotation = Quaternion.Lerp(startRot, targetTransform.rotation, easedT);
            
            yield return null;
        }

        transform.position = targetTransform.position;
        transform.rotation = targetTransform.rotation;
        _cameraRoutine = null;
    }
}