using System;

public static class GameEvents
{
    // Wave & Score
    public static Action<int> OnWaveStarted;
    public static Action<float> OnWaveTimerUpdated;
    public static Action<int> OnScoreUpdated;
    
    // Product Flow
    public static Action<Product> OnProductReachedInspection; 
    public static Action<Product> OnProductSelected;          
    public static Action<Product> OnProductReadyForInspection; 
    public static Action<Product> OnProductResolved;          
    
    public static Action<Product, string> OnMistakeMade; 
    public static Action<string> OnGameOver;

    // Feedback Events
    public static Action<Product> OnProductSuccess; 
    public static Action OnToolCountUpdated;
}