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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        scoreText = uiDocument.rootVisualElement.Q<Label>("ScoreLabel");
        restartButton = uiDocument.rootVisualElement.Q<Button>("RestartButton");
        restartButton.style.display = DisplayStyle.None;

        restartButton.clicked += ReloadScene;
    }

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        scoreText = uiDocument.rootVisualElement.Q<Label>("ScoreLabel");
        restartButton = uiDocument.rootVisualElement.Q<Button>("RestartButton");
        restartButton.style.display = DisplayStyle.None;

        restartButton.clicked += ReloadScene;
        
        // Cache camera reference
        mainCamera = Camera.main;
    }

    void MovePlayer() {
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
