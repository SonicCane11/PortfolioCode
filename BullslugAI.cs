/*
	Controls the movement of a Bullslug enemy in Vamoose by determining which of 7 states to be in, 
	and carrying out the appropriate action for that state. Also controls some particle effects and 
	passes information to a sound script and the animator.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BullslugAI : MonoBehaviour {

    [Range(1, 8)]
    [Tooltip("WAIT = 1; ALERT = 2; PREP = 3; WARN = 4; CHARGE = 5; STOP = 6; FALL = 7; RETURN = 1")]
    public int currentState = 0;
    public bool isTowardRightWayPoint;
    public GameObject sightPoint;
    public GameObject Player;
    public bool isHittingPlayer = false;
    public bool isPlayerInZone = false;
    public GameObject headPoint;

    [Header("Stop Positions (x values; infinity represents N/A)")]
    public float leftWallStop = Mathf.Infinity;
    public float rightWallStop = Mathf.Infinity;
    public float leftFallStop = Mathf.Infinity;
    public float rightFallStop = Mathf.Infinity;

    [Header("Waypoints (X values)")]
    public float leftWaypoint;
    public float rightWaypoint;

    [Header("Tuning")]
    public float walkSpeed;
    public float prepSpeed;
    public float chargeSpeed;
    public float chargeStartLerpConstant;
    public float chargeStopLerpConstant;
    [Tooltip("After turn, how long until can turn again")]
    public float chargeTurnTime;
    public float minChargeSpeed;
    public float alertPause;
    public float prepPause;
    public float warnPause;
    public float stopPause;
    [Tooltip("How far horizontally bullslug can spot player.")]
    public float sightDistance;
    [Tooltip("How far upwards the bullslug can see at its max sight distance.")]
    public float sightHeight;
    public float fallGravity;
    public float maxFallSpeed;
    public float flingConstant;
    public float minimumFlingSpeed;
    public float fallRotationStart;
    public float maxRotation;
    public float rotationConstant;

    [Header("Player Helpless")]
    public float helplessYFactorStart;
    public float helplessXFactorStart;
    public float helplessDuration;

    [Header("Particles")]
    public ParticleSystem alertParticles;
    public ParticleSystem[] chargeParticles;
    public GameObject chargeParticlePoint;
    public TrailRenderer chargeTrail;
    public GameObject deathEffect;

    [Header("Animation")]
    public Animator animator;
    public GameObject sprite;

    public bool isFacingRight = false;
    private bool isPlayerToRight = false;
    private bool isTiming = false;
    private float inclinedRayDistance;
    private float prepDistanceSoFar;
    private bool mustStartCharge = true;
    public float currentChargeSpeed = 0;
    private bool mustLerpTurn;
    private float currentFallSpeed;
    private bool isMovingForward = true;
    private float currentRotation;
    private SpriteRenderer sRenderer;
    private float xMovement;
    private bool isPlayerSpot = false;
    private bool isPlayerTouching = false;
    private int numPlatformCollisions = 0;
    private float animationSpeed;
    private bool isAlive = true;

    public float realChargeSpeed;
    // Use this for initialization
    void Start () {
		
	}

    void Awake()
    {
        // Find distance and angle of inclined ray. 
        inclinedRayDistance = Mathf.Sqrt((sightDistance * sightDistance) + (sightHeight * sightHeight));

        currentRotation = fallRotationStart;

        sRenderer = this.GetComponent<SpriteRenderer>();

        alertParticles = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update () {
        foreach (ParticleSystem P in chargeParticles)
        {
            P.enableEmission = false;
        }
        //alertParticles.enableEmission = false;
        if (currentState == 1)
        {
            //sRenderer.color = Color.black;
            wait();
            if (currentState == 2)
                alertParticles.Play();
            animationSpeed = walkSpeed * 5;
        }
        else if (currentState == 2)
        {
            //sRenderer.color = Color.red;
            alert();
        }
        else if (currentState == 3)
        {
            //sRenderer.color = Color.red;
            prep();
            animationSpeed = prepSpeed * 5;
        }
        else if (currentState == 4)
        {
            //sRenderer.color = Color.red;
            warn();
        }
        else if (currentState == 5)
        {
            //sRenderer.color = Color.red;
            if (isMovingForward)
            {
                foreach (ParticleSystem P in chargeParticles)
                {
                    P.enableEmission = true;
                    chargeParticlePoint.transform.localEulerAngles = new Vector3(0, 180 - this.transform.eulerAngles.y, 0); // flips y dimension when needed so particles always appear, and rotates in z for flip effect
                }
            }
            charge();
            animationSpeed = chargeSpeed;
        }
        else if (currentState == 6)
        {
            stop();
        }
        else if (currentState == 7)
        {
            fall();
        }
        else
        {
            reset();
        }

        sprite.transform.localPosition = this.transform.localPosition;
        
        if (xMovement > 0)
            sprite.transform.eulerAngles = new Vector3(sprite.transform.eulerAngles.x, sprite.transform.eulerAngles.y, this.transform.eulerAngles.z);
        else
            sprite.transform.eulerAngles = new Vector3(sprite.transform.eulerAngles.x, sprite.transform.eulerAngles.y, -1 * this.transform.eulerAngles.z);

        animator.SetInteger("State", currentState); // Informs Animator of current state
        animator.SetBool("isRight", this.transform.rotation.y == 0); // Informs Animator if left or right
        if (animationSpeed < 0.5) // somewhat hard-coded... for the giant bullslug, move legs quickly, but move slowly
            animationSpeed = 10;
        animator.SetFloat("Speed", animationSpeed);

        if (!isMovingForward || currentState != 5)
            chargeTrail.emitting = false;
        if (isFacingRight)
            chargeTrail.transform.localPosition = new Vector3(chargeTrail.transform.localPosition.x, chargeTrail.transform.localPosition.y, 1);
        else
            chargeTrail.transform.localPosition = new Vector3(chargeTrail.transform.localPosition.x, chargeTrail.transform.localPosition.y, -1);
    }

    // State 1: Paces between two waypoints.
    private void wait()
    {
        mustStartCharge = true;
        mustLerpTurn = false;
        if (isTowardRightWayPoint)
        {
            isFacingRight = isMovingForward = true;
            move(walkSpeed);
            if (this.transform.localPosition.x >= rightWaypoint)
            {
                isTowardRightWayPoint = false;
            }
        }
        else
        {
            isFacingRight = false;
            isMovingForward = true;
            move(walkSpeed);
            if (this.transform.localPosition.x <= leftWaypoint)
            {
                isTowardRightWayPoint = true;
            }
        }

        isPlayerSpot = lookForPlayer();

        if (isPlayerSpot)
        {
            currentState = 2;
        }
        isPlayerSpot = isPlayerTouching = false;
    }

    // State 2: Stops and shows it has spot the player.
    private void alert()
    {
        if (!isTiming)
        {
            StartCoroutine(waitAlert());
        }
    }

    // State 3: walks backwards a bit, gathering momentum.
    private void prep()
    {
        if (!isTiming)
            StartCoroutine(waitPrep());

        trackPlayer();
        isMovingForward = false;
        move(prepSpeed);
    }


    // State 4: Stops for a bit, telegraphing the charge that follows.
    private void warn()
    {
        if (!isTiming)
            StartCoroutine(waitWarn());
    }

    // State 5: charges toward player, following
    private void charge()
    {
        if (!isPlayerInZone)   // If player out of zone, set to slow to 0 if above. If not above, then reset states.
        {
            if (currentChargeSpeed > 0.1f)
            {
                mustLerpTurn = true;
                mustStartCharge = false;
            }
            else
            {
                currentState = 8;
            }
        }
        else
        {
            if (!isTiming)  // turn bullslug if needed
            {
                bool tempDir = isFacingRight;
                trackPlayer();
                if (tempDir != isFacingRight && currentChargeSpeed >= 0.1)
                {
                    mustLerpTurn = true;   
                    mustStartCharge = false;     
                    StartCoroutine(turnTimer());
                }
            }

        }

        if (mustStartCharge)  // if starting, lerp up to speed with start constant
        {
            currentChargeSpeed = Mathf.Lerp(currentChargeSpeed, chargeSpeed, chargeStartLerpConstant);
            if (currentChargeSpeed > minChargeSpeed)
                realChargeSpeed = currentChargeSpeed;
            else
                realChargeSpeed = 0;
            isMovingForward = true;
            moveCharge(currentChargeSpeed);
            if (currentChargeSpeed >= (chargeSpeed - 0.1f))
                mustStartCharge = false;
        }
        else if (mustLerpTurn) // if turning, lerp the speed to 0
        {
            currentChargeSpeed = Mathf.Lerp(currentChargeSpeed, 0, chargeStopLerpConstant);
            if (currentChargeSpeed > minChargeSpeed)
                realChargeSpeed = currentChargeSpeed;
            else
                realChargeSpeed = 0;
            isMovingForward = false;
            moveCharge(currentChargeSpeed);
            if (currentChargeSpeed <= 0.1f)  // Now that we've lerped to 0, need to start back up
            {
                mustLerpTurn = false;
                mustStartCharge = true;
                isMovingForward = true;
            }

        }
        else // else, keep at the same max charge speed!
        {
            isMovingForward = true;
            moveCharge(chargeSpeed);
            if (numPlatformCollisions == 0)
            {
                currentState = 7;
                StartCoroutine(destroy());
            }

        }
        if (isHittingPlayer) // rammer adjusts field this automatically
            currentState = 6;

        if (numPlatformCollisions == 0)
        {
            currentState = 7;
            StartCoroutine(destroy());
        }

        if (isMovingForward || currentState != 5)
            chargeTrail.emitting = true;

    }

    // State 6: Show some impact from ramming
    private void stop()
    {
        if (!isTiming)
        {
            isHittingPlayer = false;
            StartCoroutine(waitStop());
        }
    }

    // State 7: fall to death
    private void fall()
    {
        float movement;
        if (currentChargeSpeed < minimumFlingSpeed)
        {
            currentChargeSpeed = Mathf.Lerp(currentChargeSpeed, minimumFlingSpeed, flingConstant);
            
        }
        movement = currentChargeSpeed;
        if (isFacingRight)
        {
            if (!isMovingForward)
                movement *= -1;
        }
        else // facing left
        {
            if (isMovingForward)
                movement *= -1;
        }

        currentFallSpeed = Mathf.Lerp(currentFallSpeed, maxFallSpeed, fallGravity);

        this.transform.localPosition = new Vector2(transform.localPosition.x + (movement * Time.deltaTime), transform.localPosition.y - (currentFallSpeed * Time.deltaTime));
        float rot = rotationConstant * currentChargeSpeed * Time.deltaTime;
        this.transform.Rotate(0, 0, -1 * rot * Time.deltaTime);//
        //if (movement > 0) //(transform.localPosition.x >= rightFallStop)
        //{
        //    Debug.Log("Right");
        //    this.transform.Rotate(0, 0, -1 * rot * Time.deltaTime);
        //}
        //else
        //{
        //    Debug.Log("Left");
        //    this.transform.Rotate(0, 0, rot * Time.deltaTime);
        //}

    }

    // State 8: revert values to return to state 1
    private void reset()
    {
        if (this.transform.localPosition.x < leftWaypoint)
        {
            isTowardRightWayPoint = true;
            isFacingRight = true;
        }
        else
        {
            isTowardRightWayPoint = false;
            isFacingRight = false;
        }
        isPlayerSpot = isPlayerTouching = false;

        if (lookForPlayer())
        {
            alertParticles.Play();
            currentState = 2;
        }
        else
            currentState = 1;
    }

    // Moves based on a given left/right orienatation and speed.
    // Flips the bullslug if needed, and stops it from backing into a wall or off the platform.
    private void move(float speed)
    {
        // Find the amount to move, taking into account direction.
        // Also reflects bullslug to face the proper direction in-game.
        float movement = speed * Time.deltaTime;
        if (isFacingRight)
        {
            this.transform.eulerAngles = new Vector3(0, 0, 0);
            if (!isMovingForward)
                movement *= -1;
        }
        else // facing left
        {
            this.transform.eulerAngles = new Vector3(0, 180, 0);
            if (isMovingForward)
                movement *= -1;
        }
        xMovement = movement;


        // Move
        float newPos = this.transform.localPosition.x + movement;
        if (!isMovingForward)
        {
            if (canMoveBack())    
                this.transform.localPosition = new Vector3(newPos, transform.localPosition.y, transform.localPosition.z);
        }
        else
            this.transform.localPosition = new Vector3(newPos, transform.localPosition.y, transform.localPosition.z);
    }

    // Similar to the regular move function.
    // It DOES allow the bullslug to move OFF a platform.
    // If the bullslug hits a wall, it stops, and it set to not lerp a trun, and to start up again.
    private void moveCharge(float speed)
    {
        // Find the amount to move, taking into account direction.
        // Also reflects bullslug to face the proper direction in-game.
        float movement = speed * Time.deltaTime;
        if (isFacingRight)
        {
            this.transform.eulerAngles = new Vector3(0, 0, 0);
            if (!isMovingForward)
                movement *= -1;
        }
        else // facing left
        {
            this.transform.eulerAngles = new Vector3(0, 180, 0);
            if (isMovingForward)
                movement *= -1;
        }
        xMovement = movement;


        // Move
        float newPos = this.transform.localPosition.x + movement;
        if (!isMovingForward)
        {
            if (canMoveBackOnlyPlatforms())
                this.transform.localPosition = new Vector3(newPos, transform.localPosition.y, transform.localPosition.z);
        }
        else
        {
            if ((movement > 0 && canMoveRightOnlyPlatforms()) || (movement < 0 && canMoveLeftOnlyPlatforms()))
                this.transform.localPosition = new Vector3(newPos, transform.localPosition.y, transform.localPosition.z);
            else
            {
                mustLerpTurn = false;
                mustStartCharge = true;
                currentChargeSpeed = 0;
                realChargeSpeed = 0;
            }
        }
    }

    // Returns true if player is in sight - else false.
    // Uses rays to create a cone of sight - player is seen if the player comes into 
    // contact with a leg of this sight triangle, before the legs comes into contact 
    // with something else. This allows the player to take cover behind something to hide.
    // If the player is touching the bullslug, it automatically spots the player, and if 
    // the player is outside the bullslug's zone, it automatically does not spot them.
    private bool lookForPlayer()
    {
        if (isPlayerTouching)
            return true;

        if (!isPlayerInZone)
            return false;

        // Set ray directions and finds vertical ray's start position
        Vector2 horizontalRayDir, inclinedRayDir, verticalRayDir; 
        Vector2 verticalRayStart;  
        if (sightPoint.transform.position.x >= this.transform.position.x) // bullslug is facing right
        {
            horizontalRayDir = new Vector2(1, 0);
            inclinedRayDir = new Vector2(sightDistance, sightHeight);
            inclinedRayDir.Normalize();
            verticalRayStart = new Vector2(sightPoint.transform.position.x + sightDistance, sightPoint.transform.position.y);
        }
        else
        {
            horizontalRayDir = new Vector2(-1, 0);
            inclinedRayDir = new Vector2(-1 * sightDistance, sightHeight);
            inclinedRayDir.Normalize();
            verticalRayStart = new Vector2(sightPoint.transform.position.x - sightDistance, sightPoint.transform.position.y);
        }
        verticalRayDir = new Vector2(0, 1);
        
        // Raycast
        RaycastHit2D[] horizontalRayHits = Physics2D.RaycastAll(sightPoint.transform.position, horizontalRayDir, sightDistance);
        RaycastHit2D[] verticalRayHits = Physics2D.RaycastAll(verticalRayStart, verticalRayDir, sightHeight);
        RaycastHit2D[] inclinedRayHits = Physics2D.RaycastAll(sightPoint.transform.position, inclinedRayDir, inclinedRayDistance);

        // Draw Rays - for debugging
        Debug.DrawRay(sightPoint.transform.position, horizontalRayDir * sightDistance, Color.blue, Time.deltaTime);
        Debug.DrawRay(verticalRayStart, verticalRayDir * sightHeight, Color.green, Time.deltaTime);
        Debug.DrawRay(sightPoint.transform.position, inclinedRayDir * inclinedRayDistance, Color.red, Time.deltaTime);

        // Determine if rays spot player
        // If both rays inclined and horizontal rays hit a ground before the player, then bullslug never spots player.
        // If either of those two rays hit the player, but the one that did hit a ground first, never spots player.
        // In all other conditions, if any ray hits the player, the player is spot.
        bool inclinedSpotGroundFirst = false,
             horizontalSpotGroundFirst = false,
             inclinedSpotPlayer = false,
             horizontalSpotPlayer = false,
             verticalSpotPlayer = false;

        foreach (RaycastHit2D iHit in inclinedRayHits)
        {
            if (iHit.collider != null)
            {
                if (iHit.collider.tag == "Platform" && !inclinedSpotPlayer)
                    inclinedSpotGroundFirst = true;
                else if (iHit.collider.tag == "Player")
                    inclinedSpotPlayer = true;
            }
        }

        foreach (RaycastHit2D hHits in horizontalRayHits)
        {
            if (hHits.collider != null)
            {
                if (hHits.collider.tag == "Platform" && !horizontalSpotPlayer)
                    horizontalSpotGroundFirst = true;
                else if (hHits.collider.tag == "Player")
                    horizontalSpotPlayer = true;
            }
        }

        foreach (RaycastHit2D vHit in verticalRayHits)
        {
            if (vHit.collider != null)
            {
                if (vHit.collider.tag == "Player")
                    verticalSpotPlayer = true;
            }
        }

        if (inclinedSpotGroundFirst && horizontalSpotGroundFirst)
            return false;
        else if (inclinedSpotPlayer && inclinedSpotGroundFirst)
            return false;
        else if (horizontalSpotPlayer && horizontalSpotGroundFirst)
            return false;
        else if (inclinedSpotPlayer || horizontalSpotPlayer || verticalSpotPlayer)
        {
            return true;
        }
        else
            return false;  
    }

    private IEnumerator waitAlert()
    {
        isTiming = true;
        yield return new WaitForSeconds(alertPause);
        currentState = 3;
        isTiming = false;
    } 

    private IEnumerator waitPrep()
    {
        isTiming = true;
        yield return new WaitForSeconds(prepPause);
        currentState = 4;
        isTiming = false;
    }

    private IEnumerator waitWarn()
    {
        isTiming = true;
        yield return new WaitForSeconds(warnPause);
        currentState = 5;
        isTiming = false;
    }

    private IEnumerator waitStop()
    {
        isTiming = true;
        isHittingPlayer = false;
        yield return new WaitForSeconds(stopPause);
        mustStartCharge = true;
        currentChargeSpeed = 0;
        realChargeSpeed = 0;
        mustLerpTurn = false;
        isMovingForward = true;
        currentState = 8;
        isTiming = false;
    }

    private IEnumerator turnTimer()
    {
        isTiming = true;
        yield return new WaitForSeconds(chargeTurnTime);
        isTiming = false;
    }

    // Returns true if there's nothing immediately behind, false if there is.
    private bool canMoveBack()
    {
        if (isFacingRight)
        {
            if (leftFallStop != Mathf.Infinity)
            {
                if (this.transform.localPosition.x <= leftFallStop)
                    return false;
            }
            if (leftWallStop != Mathf.Infinity)
            {
                if (this.transform.localPosition.x <= leftWallStop)
                    return false;
            }

        }
        else
        {
            if (rightFallStop != Mathf.Infinity)
            {
                if (this.transform.localPosition.x >= rightFallStop)
                    return false;
            }
            if (rightWallStop != Mathf.Infinity)
            {
                if (this.transform.localPosition.x >= rightWallStop)
                    return false;
            }
        }
        return true;
    }

    // Returns true if there're no platforms (only) immediately behind, false if there are.
    private bool canMoveBackOnlyPlatforms()
    {
        if (isFacingRight)
        {
            if (leftWallStop != Mathf.Infinity)
            {
                if (this.transform.localPosition.x <= leftWallStop)
                    return false;
            }
        }
        else
        {
            if (rightWallStop != Mathf.Infinity)
            {
                if (this.transform.localPosition.x >= rightWallStop)
                    return false;
            }
        }
        return true;
    }

    // Returns true if there's no platforms immediately to left, false if there are.
    private bool canMoveLeftOnlyPlatforms()
    {
         if (leftWallStop != Mathf.Infinity)
         {
             if (this.transform.localPosition.x <= leftWallStop)
                 return false;
         }
        return true;
    }

    // Returns true if there's no platforms immediately to right, false if there are.
    private bool canMoveRightOnlyPlatforms()
    {
        if (rightWallStop != Mathf.Infinity)
        {
            if (this.transform.localPosition.x >= rightWallStop)
                return false;
        }
        return true;
    }

    // determines whether bullslug should face left (false) or right (true)
    private void trackPlayer()
    {
        if (Player.transform.position.x - headPoint.transform.position.x >= 0)
        {
            isFacingRight = true;
        }
        else
        {
            isFacingRight = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "Platform")
            numPlatformCollisions++;
        else if (collider.gameObject.tag == "Player")
            isPlayerTouching = true;
        else if (collider.gameObject.tag == "Lava")
        {
            if (isAlive)
            {
                isAlive = false;
                GameObject d = Object.Instantiate(deathEffect, this.transform.position, Quaternion.identity);
                d.transform.localScale = this.transform.localScale;
                currentState = 7;
                StartCoroutine(destroy());
                currentChargeSpeed = 2;
            }
        }
    }

    IEnumerator destroy()
    {
        yield return new WaitForSeconds(45);
        GameObject.Destroy(this.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.tag == "Platform")
            if (collider.tag == "Platform")
                numPlatformCollisions--;
    }
}
