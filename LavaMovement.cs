/*
	Controls the movement for the lava in Vamoose. Several trip wires are placed around 
	the level, and when the player crosses them, the lava rises to a certain point, adjusting 
	the speed. This lets the lava "keep up" with the player and for the speed to be conducive 
	to balanced gameplay.
*/

using UnityEngine;
using System.Collections;
[System.Serializable]
public struct PositionChange
{
    public GameObject playerTripwire;
    public GameObject lavaNewYPos;
    public float newSpeed;
    public bool hasPlayerCrossed;
}

public class LavaMovement : MonoBehaviour
{
    public float currentNormalSpeed;
    public float catchUpSpeed;
    public float maxDistance;
    public GameObject player;
    public AudioSource ambience;
    public PositionChange[] positionChanges;
    public LevelController levelScript;
    public AudioSource speedupSFX;

    private float volume = 0.2f;
    private float catchupLerpConstant;
    private float currentCatchupSpeed;
    private bool SFXplayed = false;
    private bool speedupWaitOver;
    private bool alreadyWaiting;
    private PlayerMovement playerScript;

    void Start () {
        ambience.loop = true;
        ambience.Play(0);
        currentCatchupSpeed = catchUpSpeed;
        playerScript = player.GetComponent<PlayerMovement>();
	}

    void Update() {
        if (!playerScript.hasDied)
        {
            float catchupDistance = posSpeedAdjustment(); // take action if player crossed trip point and lava lagging behind

            if (catchupDistance == Mathf.Infinity) // if that is not the case, move up normally
            {
                SFXplayed = false;
                this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + (currentNormalSpeed * Time.deltaTime), this.transform.position.z);
                currentCatchupSpeed = catchUpSpeed;
            }
            else // if it is, move up quickly
            {
                if (!SFXplayed)
                {
                    speedupSFX.Play();
                    SFXplayed = true;
                }

                if (catchupDistance < 4)    // if nearing, ease into new normal speed
                {
                    float tempLerpCon = 0.1f / Mathf.Abs(catchupDistance);
                    currentCatchupSpeed = Mathf.Lerp(currentCatchupSpeed, currentNormalSpeed, tempLerpCon);
                }
                else // else, far away, so max speed!
                {
                    currentCatchupSpeed = catchUpSpeed;
                }

                //currentCatchupSpeed = Mathf.Lerp(currentCatchupSpeed, currentNormalSpeed, catchupLerpConstant);
                this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + (currentCatchupSpeed * Time.deltaTime), this.transform.position.z);
            }
            // lava SFX are louder when nearer
            volume = 20 - Mathf.Abs(player.transform.position.y - this.transform.position.y);
            if (volume < 0.1f)
                volume = 0.1f;
            ambience.volume = volume;
        }

    }

    // Determines if the player has crossed one of the invisible trip wires. These trip wires represent heights that, once reached 
    // by the player, the lava should be at lease at another cetain height. If the lava is "running late" in this way, this script
    // enables the lava to "catch up." 
    // When Infinity is returned, this means the lava is "on time."
    // Otherwise, it means the lava must move at a higher speed than usual -- the value returned from this method represents 
    // the distance of between the lava's current y locaiton, and where it needs to be.
    // Also, updates the current speed according to the highest trip wire the player crossed.
    private float posSpeedAdjustment()
    {
        for (int i = positionChanges.Length - 1; i >= 0; i--)
        {
            if (positionChanges[i].hasPlayerCrossed)    // if player has crossed a "trip wire"
            {
                //Debug.Log("trip wire reached: " + i);//
                currentNormalSpeed = positionChanges[i].newSpeed;
                float tempY = positionChanges[i].lavaNewYPos.transform.position.y;
                if (this.transform.position.y < tempY - 0.1f)
                {
                    if (speedupWaitOver)
                    {
                        float tempDistance = Mathf.Abs(tempY - transform.position.y);
                        return tempDistance;
                    }
                    else
                    {
                        if (!alreadyWaiting)
                            StartCoroutine(WaitSpeedUp());
                        return Mathf.Infinity;
                    }
                }
                else
                    return Mathf.Infinity;
            }
        }
        speedupWaitOver = false;
        alreadyWaiting = false;
        return Mathf.Infinity;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            levelScript.playerDied = true;
        }
    }

    IEnumerator WaitSpeedUp()
    {
        alreadyWaiting = true;
        yield return new WaitForSeconds(Random.Range(0.3f, 1f));
        speedupWaitOver = true;
    }

    
}

