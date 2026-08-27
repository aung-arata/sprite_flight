using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float thrustForce = 1f;
    public float maxSpeed = 5f;
    private float elapsedTime = 0f;

    private float score = 0f;
    public float scoreMultiplier = 10f;

    [Header("Difficulty")]
    [Min(1f)] public float difficultyInterval = 15f;
    [Min(0f)] public float asteroidSpeedIncreasePerStage = 0.5f;
    [Min(1f)] public float asteroidSpeedLimit = 3f;
    [Min(0f)] public float difficultyMessageDuration = 1.25f;

    Rigidbody2D rb;
    public GameObject boosterFlame;
    public UIDocument uiDocument;

    private Label scoreText;
    private Label finalScoreText;
    private Label bestScoreText;
    private Label newBestText;
    private Label difficultyText;
    private VisualElement gameOverPanel;
    private VisualElement pausePanel;

    public GameObject explosionEffect;
    private Button restartButton;
    private Button pauseButton;
    private Button resumeButton;
    private Button pauseRestartButton;

    private bool isGameOver;
    private bool isPaused;
    private bool isInitialized;
    private bool movementInputArmed = true;
    private bool restartInputArmed;
    private bool isReloading;
    private float timeScaleBeforePause = 1f;
    private int difficultyStage;
    private float asteroidSpeedMultiplier = 1f;
    private Coroutine difficultyMessageCoroutine;

    public float CurrentAsteroidSpeedMultiplier => asteroidSpeedMultiplier;
    public Vector2 ArenaCenter => mainCamera != null
        ? (Vector2)mainCamera.transform.position
        : Vector2.zero;

    private const string BestScoreKey = "BestScore";
    private const float ScreenShakeDuration = 0.25f;
    private const float ScreenShakeMagnitude = 0.2f;

    // Update is called once per frame
    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateCameraForScreen();
        }

        if (isPaused)
        {
            return;
        }

        if (isGameOver)
        {
            HandleRestartInput();
            return;
        }

        UpdateScore();
        UpdateDifficulty();

        if (!movementInputArmed)
        {
            movementInputArmed = !IsPointerPressed();
            return;
        }

        MovePlayer();
    }

    void UpdateScore() {
        elapsedTime += Time.deltaTime;
        score = Mathf.FloorToInt(elapsedTime * scoreMultiplier);

        scoreText.text = "Score: " + score;
        // Debug.Log("Score: " + score);
    }

    // Cached camera reference for performance
    private Camera mainCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private Transform borderLeft;
    private Transform borderRight;
    private Transform borderTop;
    private Transform borderBottom;
    private Vector3 borderLeftBaseScale;
    private Vector3 borderRightBaseScale;
    private Vector3 borderTopBaseScale;
    private Vector3 borderBottomBaseScale;

    private const float ReferenceAspect = 16f / 9f;
    private const float ReferenceOrthographicSize = 7f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        scoreText = uiDocument.rootVisualElement.Q<Label>("ScoreLabel");
        finalScoreText = uiDocument.rootVisualElement.Q<Label>("FinalScoreLabel");
        bestScoreText = uiDocument.rootVisualElement.Q<Label>("BestScoreLabel");
        newBestText = uiDocument.rootVisualElement.Q<Label>("NewBestLabel");
        difficultyText = uiDocument.rootVisualElement.Q<Label>("DifficultyLabel");
        gameOverPanel = uiDocument.rootVisualElement.Q<VisualElement>("GameOverPanel");
        pausePanel = uiDocument.rootVisualElement.Q<VisualElement>("PausePanel");
        restartButton = uiDocument.rootVisualElement.Q<Button>("RestartButton");
        pauseButton = uiDocument.rootVisualElement.Q<Button>("PauseButton");
        resumeButton = uiDocument.rootVisualElement.Q<Button>("ResumeButton");
        pauseRestartButton = uiDocument.rootVisualElement.Q<Button>("PauseRestartButton");
        gameOverPanel.style.display = DisplayStyle.None;
        pausePanel.style.display = DisplayStyle.None;

        restartButton.clicked += ReloadScene;
        pauseButton.RegisterCallback<PointerDownEvent>(HandlePausePointerDown);
        resumeButton.clicked += ResumeGame;
        pauseRestartButton.clicked += ReloadScene;
        
        // Cache camera reference
        mainCamera = Camera.main;
        CacheArenaBorders();
        UpdateCameraForScreen();
        isInitialized = true;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isInitialized)
        {
            PauseGame();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isInitialized)
        {
            PauseGame();
        }
    }

    void PauseGame()
    {
        if (isPaused || isGameOver || isReloading)
        {
            return;
        }

        isPaused = true;
        timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        pausePanel.style.display = DisplayStyle.Flex;
        pauseButton.style.display = DisplayStyle.None;

        if (boosterFlame != null)
        {
            boosterFlame.SetActive(false);
        }
    }

    void HandlePausePointerDown(PointerDownEvent pointerEvent)
    {
        PauseGame();
        pointerEvent.StopPropagation();
    }

    void ResumeGame()
    {
        if (!isPaused || isReloading)
        {
            return;
        }

        Time.timeScale = timeScaleBeforePause;
        isPaused = false;
        movementInputArmed = false;
        pausePanel.style.display = DisplayStyle.None;
        pauseButton.style.display = DisplayStyle.Flex;
    }

    void UpdateDifficulty()
    {
        if (difficultyInterval <= 0f)
        {
            return;
        }

        int newStage = Mathf.FloorToInt(elapsedTime / difficultyInterval);
        if (newStage <= difficultyStage)
        {
            return;
        }

        difficultyStage = newStage;
        float newMultiplier = Mathf.Min(
            1f + difficultyStage * asteroidSpeedIncreasePerStage,
            Mathf.Max(1f, asteroidSpeedLimit));

        if (newMultiplier <= asteroidSpeedMultiplier)
        {
            return;
        }

        asteroidSpeedMultiplier = newMultiplier;

        foreach (Stone stone in FindObjectsByType<Stone>(FindObjectsSortMode.None))
        {
            stone.SetSpeedMultiplier(asteroidSpeedMultiplier, ArenaCenter);
        }

        if (difficultyMessageCoroutine != null)
        {
            StopCoroutine(difficultyMessageCoroutine);
        }

        difficultyMessageCoroutine = StartCoroutine(ShowDifficultyMessage());
    }

    IEnumerator ShowDifficultyMessage()
    {
        difficultyText.text = $"Speed x{asteroidSpeedMultiplier:0.0}";
        difficultyText.style.display = DisplayStyle.Flex;
        yield return new WaitForSeconds(difficultyMessageDuration);
        difficultyText.style.display = DisplayStyle.None;
        difficultyMessageCoroutine = null;
    }

    void CacheArenaBorders()
    {
        borderLeft = FindTransform("Border_Left");
        borderRight = FindTransform("Border_Right");
        borderTop = FindTransform("Border_Top");
        borderBottom = FindTransform("Border_Bottom");

        if (borderLeft != null)
        {
            borderLeftBaseScale = borderLeft.localScale;
        }

        if (borderRight != null)
        {
            borderRightBaseScale = borderRight.localScale;
        }

        if (borderTop != null)
        {
            borderTopBaseScale = borderTop.localScale;
        }

        if (borderBottom != null)
        {
            borderBottomBaseScale = borderBottom.localScale;
        }
    }

    static Transform FindTransform(string objectName)
    {
        GameObject foundObject = GameObject.Find(objectName);
        return foundObject != null ? foundObject.transform : null;
    }

    void UpdateCameraForScreen()
    {
        if (mainCamera == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float currentAspect = (float)Screen.width / Screen.height;
        mainCamera.orthographicSize = currentAspect < ReferenceAspect
            ? ReferenceOrthographicSize * (ReferenceAspect / currentAspect)
            : ReferenceOrthographicSize;

        UpdateArenaForCamera(currentAspect);
    }

    void UpdateArenaForCamera(float currentAspect)
    {
        if (borderLeft == null || borderRight == null || borderTop == null || borderBottom == null)
        {
            return;
        }

        Vector3 cameraPosition = mainCamera.transform.position;
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * currentAspect;

        borderLeft.position = new Vector3(cameraPosition.x - halfWidth, cameraPosition.y, borderLeft.position.z);
        borderRight.position = new Vector3(cameraPosition.x + halfWidth, cameraPosition.y, borderRight.position.z);
        borderTop.position = new Vector3(cameraPosition.x, cameraPosition.y + halfHeight, borderTop.position.z);
        borderBottom.position = new Vector3(cameraPosition.x, cameraPosition.y - halfHeight, borderBottom.position.z);

        borderLeft.localScale = new Vector3(
            borderLeftBaseScale.x,
            halfHeight * 2f + borderTopBaseScale.y,
            borderLeftBaseScale.z);
        borderRight.localScale = new Vector3(
            borderRightBaseScale.x,
            halfHeight * 2f + borderBottomBaseScale.y,
            borderRightBaseScale.z);
        borderTop.localScale = new Vector3(
            halfWidth * 2f + borderLeftBaseScale.x,
            borderTopBaseScale.y,
            borderTopBaseScale.z);
        borderBottom.localScale = new Vector3(
            halfWidth * 2f + borderRightBaseScale.x,
            borderBottomBaseScale.y,
            borderBottomBaseScale.z);
    }

    void MovePlayer() {
        bool isInputActive = TryGetPointerScreenPosition(out Vector2 pointerPosition);
        Vector2 inputPosition = Vector2.zero;

        if (isInputActive && mainCamera == null)
        {
            isInputActive = false;
        }
        else if (isInputActive)
        {
            float screenPosZ = mainCamera.WorldToScreenPoint(transform.position).z;
            pointerPosition.x = Mathf.Clamp(pointerPosition.x, 0f, Screen.width);
            pointerPosition.y = Mathf.Clamp(pointerPosition.y, 0f, Screen.height);

            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
                new Vector3(pointerPosition.x, pointerPosition.y, screenPosZ));

            if (IsFinite(worldPosition.x) && IsFinite(worldPosition.y))
            {
                inputPosition = worldPosition;
            }
            else
            {
                isInputActive = false;
            }
        }

        if (isInputActive)
        {
            Vector2 direction = (inputPosition - (Vector2)transform.position).normalized;
            
            transform.up = direction;
            rb.AddForce(direction * thrustForce);

            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }

            // Show booster flame when input is active
            if (boosterFlame != null)
            {
                boosterFlame.SetActive(true);
            }
        }
        else
        {
            // Hide booster flame when no input
            if (boosterFlame != null)
            {
                boosterFlame.SetActive(false);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;

        int finalScore = Mathf.FloorToInt(score);
        int previousBestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        bool achievedNewBest = finalScore > previousBestScore;
        int bestScore = achievedNewBest ? finalScore : previousBestScore;

        if (achievedNewBest)
        {
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        finalScoreText.text = $"Score: {finalScore}";
        bestScoreText.text = $"Best: {bestScore}";
        newBestText.style.display = achievedNewBest ? DisplayStyle.Flex : DisplayStyle.None;
        gameOverPanel.style.display = DisplayStyle.Flex;
        difficultyText.style.display = DisplayStyle.None;
        pauseButton.style.display = DisplayStyle.None;

        restartInputArmed = !IsPointerPressed();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        foreach (Collider2D playerCollider in GetComponentsInChildren<Collider2D>())
        {
            playerCollider.enabled = false;
        }

        foreach (SpriteRenderer playerRenderer in GetComponentsInChildren<SpriteRenderer>())
        {
            playerRenderer.enabled = false;
        }

        if (boosterFlame != null)
        {
            boosterFlame.SetActive(false);
        }

        Instantiate(explosionEffect, transform.position, transform.rotation);
        StartCoroutine(ShakeCamera());
    }

    void HandleRestartInput()
    {
        if (!restartInputArmed)
        {
            restartInputArmed = !IsPointerPressed();
        }

        if (restartInputArmed && WasPointerPressedThisFrame())
        {
            ReloadScene();
        }
    }

    static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        // Device Simulator disables the native mouse and provides a simulated
        // touchscreen, so touch must be checked before the mouse fallback.
        if (IsTouchscreenAvailable() && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return IsFinite(screenPosition.x) && IsFinite(screenPosition.y);
        }

        if (IsMouseAvailable() && Mouse.current.leftButton.isPressed)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return IsFinite(screenPosition.x) && IsFinite(screenPosition.y);
        }

        return false;
    }

    static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    static bool IsTouchscreenAvailable()
    {
        return Touchscreen.current != null && Touchscreen.current.enabled;
    }

    static bool IsMouseAvailable()
    {
        return Mouse.current != null && Mouse.current.enabled;
    }

    static bool IsPointerPressed()
    {
        bool mousePressed = IsMouseAvailable() && Mouse.current.leftButton.isPressed;
        bool touchPressed = IsTouchscreenAvailable() && Touchscreen.current.primaryTouch.press.isPressed;
        return mousePressed || touchPressed;
    }

    static bool WasPointerPressedThisFrame()
    {
        bool mousePressed = IsMouseAvailable() && Mouse.current.leftButton.wasPressedThisFrame;
        bool touchPressed = IsTouchscreenAvailable() && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        return mousePressed || touchPressed;
    }

    IEnumerator ShakeCamera()
    {
        if (mainCamera == null)
        {
            yield break;
        }

        Vector3 originalPosition = mainCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < ScreenShakeDuration)
        {
            float strength = 1f - elapsed / ScreenShakeDuration;
            Vector2 offset = Random.insideUnitCircle * (ScreenShakeMagnitude * strength);
            mainCamera.transform.localPosition = originalPosition + new Vector3(offset.x, offset.y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = originalPosition;
    }

    void ReloadScene()
    {
        if (isReloading)
        {
            return;
        }

        isReloading = true;
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.clicked -= ReloadScene;
        }

        if (pauseButton != null)
        {
            pauseButton.UnregisterCallback<PointerDownEvent>(HandlePausePointerDown);
        }

        if (resumeButton != null)
        {
            resumeButton.clicked -= ResumeGame;
        }

        if (pauseRestartButton != null)
        {
            pauseRestartButton.clicked -= ReloadScene;
        }

        if (isPaused)
        {
            Time.timeScale = timeScaleBeforePause;
        }
    }
}
