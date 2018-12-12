using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public bool isFlat = true;
    
    [Header("Level Setup")]
    public GameObject pauseMenu;
    public GameObject nextLevel;
    public Scene currentLevel;
    public Text countText;
    public Text livesText;
    public Text winText;
    public Text loseText;
    public int winGoal;
    public int lives;
    public AudioSource jumpSource;
    public AudioClip jumpSound;
    public AudioSource pickUpSource;
    public AudioClip pickUpSound;

    private int count;
    private float restartTime = 0f;

    [Header("Physics Variables")]
    [Range(1, 10)]
    public float jumpVelocity;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float distToGround = 0.55f;
    public float speed = 10;
    public float speedLimit = 15;
    public LayerMask ground;

    private bool grounded;
    private Rigidbody rb;

    [Header("Pickup Movement")]
    public GameObject pickup;
    // 0 = Don't rotate
    public float xRotate = 15f;
    public float yRotate = 30f;
    public float zRotate = 45f;
    public bool bounce;
    public float amplitude = 0.5f;
    public float frequency = 1f;

    // Position Storage Variables
    private Vector3 posOffset = new Vector3();
    private Vector3 tempPos = new Vector3();

    // Use this for initialization
    void Start()
    {
        TimeStop(false);
        rb = GetComponent<Rigidbody>();
        jumpSource.clip = jumpSound;
        pickUpSource.clip = pickUpSound;
        currentLevel = SceneManager.GetActiveScene();
        count = 0;
        SetUIText();
        winText.text = "";
        loseText.text = "";

        // Store the starting position & rotation of the pickup
        posOffset = pickup.transform.position;
    }

    // Update us called once per frame
    void Update()
    {
        //

        // Pause
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) {
            PauseToggle();
        }

        //Spins the object around the x, y and z axis
        pickup.transform.Rotate(new Vector3(xRotate, yRotate, zRotate) * Time.deltaTime, Space.World);

        // Float up/down with a Sin()
        if (bounce)
        {
            tempPos = posOffset;
            tempPos.y += Mathf.Sin(Time.fixedTime * Mathf.PI * frequency) * amplitude;

            pickup.transform.position = tempPos;
        }

        // If out of lives...
        if (lives <= 0)
        {
            // Freeze player for 5 seconds then end application
            StopPlayer();
            restartTime -= Time.deltaTime;
            if (restartTime <= 0)
            {
                Retry();
            }
        }
        grounded = Grounded();

    }

    // Updates optimized for physics
    void FixedUpdate()
    {
        //Adds a velocity for jumping and a traditional force for rolling
        rb.AddForce(JumpForce(), ForceMode.VelocityChange);
        rb.AddForce(RollForce());
        Vector3 tilt = Input.acceleration * speed;
        tilt.z = 0;

        if (isFlat)
        {
            tilt = Quaternion.Euler(90, 0, 0) * tilt;
        }

        rb.AddForce(tilt);
        /*
        if (tilt.x > speedLimit || tilt.x < -speedLimit) {
            rb.AddForce(-1 * tilt.x, 0, 0);
        }
        if (tilt.y > speedLimit || tilt.y < -speedLimit) {
            rb.AddForce(0, -1 * tilt.y, 0);
        }
        if (Mathf.Abs(rb.velocity.x) > speedLimit) {
            rb.velocity.x = speedLimit;
        }
        */
        float mag = Vector3.Magnitude (rb.velocity);

        if (mag > speedLimit) {
            float brakeSpeed = mag - speedLimit;  // calculate the speed decrease
 
            Vector3 normalisedVelocity = rb.velocity.normalized;
            Vector3 brakeVelocity = normalisedVelocity * brakeSpeed;  // make the brake Vector3 value
 
            rb.AddForce(-brakeVelocity);  // apply opposing brake force
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // touching a collectable
        if (other.gameObject.CompareTag("Pick Up")) {
            // Adds score and deactivates object
            pickUpSource.Play();
            other.gameObject.SetActive(false);
            count++;
            SetUIText();
            // Falling off the map
        } else if (other.gameObject.CompareTag("Dead Zone")) {
            lives--;
            SetUIText();
            transform.position = Vector3.zero;
            StopPlayer();
        }
    }

    // Makes jumping dyamnic with different fall speeds and heights
    private void SnappyJump(float multiplier)
    {
        rb.velocity += Vector3.up * Physics.gravity.y * (multiplier - 1) * Time.deltaTime;
    }

    // Checks if there is a floor distToGround units below player
    private bool Grounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, distToGround, ground);
    }

    // Updates UI
    private void SetUIText()
    {
        countText.text = "Count: " + count.ToString();
        livesText.text = "Lives: " + lives.ToString();
        // Win event
        if (count >= winGoal)
        {
            winText.text = "You Win! :\")";
            nextLevel.SetActive(true);
            StopPlayer();
        }
        else if (lives <= 0)
        {
            loseText.text = "You Lose! :^(";
            // Initate Restart
            restartTime = 3f;
        }
    }

    // Toggles the pause menu
    public void PauseToggle()
    {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        TimeStop(pauseMenu.activeSelf);
    }

    // Resets the level and and unfreezes time
    public void Retry()
    {
        TimeStop(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Reload level");
    }

    // End Game
    public void Quit()
    {
        rb.velocity = Vector3.zero;
        Debug.Log("QUIT!");
        Application.Quit();
    }

    // Flexible level select for all menu buttons
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    // Freezes player
    private void StopPlayer()
    {
        rb.velocity = Vector3.zero;
    }

    // Toggles time pause
    private void TimeStop(bool condition)
    {
        if (condition) {
            Time.timeScale = 0f;
            Debug.Log("Pause");
        } else {
            Time.timeScale = 1f;
            Debug.Log("Unpause");
        }
    }

    //calculates force to propel straight up
    private Vector3 JumpForce()
    {
        Vector3 movement = Vector3.zero;

        if ((Input.GetMouseButtonDown(0) || Input.GetButtonDown("Jump")) && (grounded || Grounded()))
        {
            movement.y = jumpVelocity;
            jumpSource.Play();
            if (rb.velocity.y < 0)
            {
                SnappyJump(fallMultiplier);
            }
            else if (rb.velocity.y > 0 && !(Input.GetMouseButtonDown(0) || Input.GetButton("Jump")))
            {
                SnappyJump(lowJumpMultiplier);
            }
        }
        return movement;
    }

    // Calculates force to go across the x and z axis
    private Vector3 RollForce()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(moveHorizontal * speed, 0.0f, moveVertical * speed);

        return movement;
    }
}