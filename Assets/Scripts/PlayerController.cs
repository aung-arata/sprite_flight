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

    void MovePlayer() {
        bool isInputActive = false;
        Vector2 inputPosition = Vector2.zero;

        // Check for mouse input (for PC/editor testing)
        if(Mouse.current.leftButton.isPressed)
        {
            isInputActive = true;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
            inputPosition = mousePos;
        }
        // Check for touch input (for mobile)
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isInputActive = true;
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, Camera.main.nearClipPlane));
            inputPosition = worldPos;
        }

        if (isInputActive)
        {
            Vector2 direction = (inputPosition - transform.position).normalized;
            
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
