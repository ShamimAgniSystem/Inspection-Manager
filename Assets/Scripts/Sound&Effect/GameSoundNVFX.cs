using UnityEngine;
using System.Collections.Generic;

public class GameVFX_SFX_Manager : MonoBehaviour
{
    public static GameVFX_SFX_Manager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip fixSuccessClip;
    [SerializeField] private AudioClip rejectCorrectClip;
    [SerializeField] private AudioClip mistakeClip;
    [SerializeField] private AudioClip criticalFailureClip; 
    [SerializeField] private AudioClip productClickClip;
    [SerializeField] private AudioClip timerFreezeClip;

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject successVFXPrefab;
    [SerializeField] private GameObject failureVFXPrefab;
    [SerializeField] private GameObject freezeVFXPrefab;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f; 
        }
    }

    private void OnEnable()
    {
        GameEvents.OnProductSelected += PlayProductClick;
        GameEvents.OnMistakeMade += PlayMistake;
        GameEvents.OnGameOver += PlayCriticalFailure;
        GameEvents.OnProductSuccess += HandleProductSuccessVFX_SFX; 
    }

    private void OnDisable()
    {
        GameEvents.OnProductSelected -= PlayProductClick;
        GameEvents.OnMistakeMade -= PlayMistake;
        GameEvents.OnGameOver -= PlayCriticalFailure;
        GameEvents.OnProductSuccess -= HandleProductSuccessVFX_SFX; 
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void PlayVFX(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(vfx, ps.main.duration + 0.5f);
            }
            else
            {
                Destroy(vfx, 3f); 
            }
        }
    }

    private void PlayProductClick(Product product)
    {
        PlaySFX(productClickClip);
    }

    private void HandleProductSuccessVFX_SFX(Product product)
    {
        if (product == null) return;
        Vector3 productPos = product.transform.position;

        if (product.IsRejectedProduct)
        {
            PlaySFX(rejectCorrectClip);
        }
        else
        {
            PlaySFX(fixSuccessClip);
        }
        
        PlayVFX(successVFXPrefab, productPos);
    }

    private void PlayMistake(Product product, string reason)
    {
        if (product == null) return;

        if (!reason.Contains("CRITICAL FAILURE"))
        {
            PlaySFX(mistakeClip);
        }
        
        PlayVFX(failureVFXPrefab, product.transform.position);
    }

    private void PlayCriticalFailure(string reason)
    {
        PlaySFX(criticalFailureClip); 
    }
    
    public void PlayFreezeToolVFX_SFX(Product product)
    {
        if (product == null) return;
        
        if (product.IsTimerFrozen) 
        {
            PlaySFX(timerFreezeClip);
            GameObject vfx = Instantiate(freezeVFXPrefab, product.transform);
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(vfx, ps.main.duration + 0.5f);
            }
            else
            {
                Destroy(vfx, 3f); 
            }
        }
    }
}