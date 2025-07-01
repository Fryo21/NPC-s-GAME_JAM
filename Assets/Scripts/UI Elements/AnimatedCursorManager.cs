using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimatedCursorManager : MonoBehaviour
{
    public static AnimatedCursorManager Instance { get; private set; }

    private bool isHovering = false;
    private bool isClicking = false;

    [Header("Cursor")]
    [SerializeField] private Texture2D idleCursorTexture;

    [SerializeField] private Texture2D[] hoverCursorTextures;

    [SerializeField] private Texture2D[] clickCursorTextures;

    [SerializeField] private Vector2 cursorHotspot = new Vector2(50, 50);

    [SerializeField] private float cursorImageHoverCycleSpeed = 0.5f;

    [SerializeField] private float cursorImageClickCycleSpeed = 0.2f;

    private Coroutine hoverCoroutine;
    private Coroutine clickCoroutine;



    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    //call this when a new scene loads (as this is dontDestroyOnLoad)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupCursor();
    }


    void SetupCursor()
    {
        SetCursorToIdle();
    }


    public void OnCursorExitUIElement()
    {
        isHovering = false;
        isClicking = false;
        SetCursorToIdle();
    }

    public void OnCursorHoverUIElement()
    {
        isHovering = true;
        SetCursorToHover();
    }

    public void OnCursorClick()
    {
        isClicking = true;
        clickCoroutine = StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        for (int i = 0; i < clickCursorTextures.Length; i++)
        {
            Cursor.SetCursor(clickCursorTextures[i], cursorHotspot, CursorMode.Auto);
            yield return new WaitForSeconds(cursorImageClickCycleSpeed);
        }
        yield return new WaitForSeconds(cursorImageClickCycleSpeed);
        OnCursorClickEnd();
    }


    public void OnCursorClickEnd()
    {
        isClicking = false;

        if (isHovering)
        {
            SetCursorToHover();
        }
        else
        {
            SetCursorToIdle();
        }

    }

    private void SetCursorToIdle()
    {
        Cursor.SetCursor(idleCursorTexture, cursorHotspot, CursorMode.Auto);
    }

    private void SetCursorToHover()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }

        hoverCoroutine = StartCoroutine(HoverAnimation());
    }

    private IEnumerator HoverAnimation()
    {
        int textureIndex = 0;
        while (isHovering && !isClicking)
        {
            Cursor.SetCursor(hoverCursorTextures[textureIndex], cursorHotspot, CursorMode.Auto);
            textureIndex = (textureIndex + 1) % hoverCursorTextures.Length;
            yield return new WaitForSeconds(cursorImageHoverCycleSpeed);
        }
    }

    private void SetCursorToClick()
    {
        Cursor.SetCursor(clickCursorTextures[0], cursorHotspot, CursorMode.Auto);
    }



}