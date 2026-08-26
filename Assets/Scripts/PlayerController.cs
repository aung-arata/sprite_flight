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

    Rigidbody2D rb;
    public GameObject boosterFlame;
    public UIDocument uiDocument;

    private Label scoreText;

    public GameObject explosionEffect;
    private Button restartButton;

    // Update is called once per frame
    void Update()
    {
        UpdateScore();
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
        restartButton = uiDocument.rootVisualElement.Q<Button>("RestartButton");
        restartButton.style.display = DisplayStyle.None;

        restartButton.clicked += ReloadScene;
        
        // Cache camera reference
        mainCamera = Camera.main;
        CacheArenaBorders();
        UpdateCameraForScreen();
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
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateCameraForScreen();
        }

        bool isInputActive = false;
        Vector2 inputPosition = Vector2.zero;
        
        // Cache the player's screen position z-depth for consistent world position conversion
        float screenPosZ = mainCamera != null ? mainCamera.WorldToScreenPoint(transform.position).z : 0f;

        // Check for mouse input (for PC/editor testing) - with null safety
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isInputActive = true;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, screenPosZ));
            inputPosition = worldPos;
        }
        // Check for touch input (for mobile)
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isInputActive = true;
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, screenPosZ));
            inputPosition = worldPos;
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
        Destroy(gameObject);
        Instantiate(explosionEffect, transform.position, transform.rotation);
        restartButton.style.display = DisplayStyle.Flex;
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
