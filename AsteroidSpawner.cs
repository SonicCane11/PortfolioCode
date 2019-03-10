/*
	Sets up the shape and position of a newly spawned asteroid for a simple Asteroids game.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour {
    public float smallScale = 0.5f,
                 bigScale = 0.75f;
    public float minPlayerDistance = 0.2f;
    public GameObject[] spawnPoints;
    public Color[] possibleColors;
    private GameObject playerObject;

    public bool isSpawned = false;

    private SpriteRenderer spriteRenderer;

    // Use this for initialization
    void Start () {
        playerObject = GameObject.FindGameObjectWithTag("player");
        spriteRenderer = GetComponent<SpriteRenderer>();

        Spawn();
    }

    // Update is called once per frame
    void Update () {
		if (!isSpawned)
        {
            Color oldColor = spriteRenderer.color;
            spriteRenderer.color += new Color(0, 0, 0, Time.deltaTime);  // Make sprite more opaque, being solid by 1s
            // If the last calculation made it fully opaque, make random color and isSpawned = true
            if (spriteRenderer.color.a >= 1)    
            {
                Color newColor = possibleColors[Random.Range(0, possibleColors.Length)];
                spriteRenderer.color = new Color(newColor.r, newColor.g, newColor.b, 1);
                isSpawned = true;
            }
        }
	}

    void Spawn()
    {
        // Resize
        transform.localScale += new Vector3(Random.Range(smallScale, bigScale), Random.Range(smallScale, bigScale));
        transform.Rotate(0, 0, Random.Range(0f, 360f));

        // Make black and transparent
        Color oldColor = spriteRenderer.color;
        spriteRenderer.color = new Color(114f, 114f, 114f, 0f); 

        // Pick spawn point - randomly, and one far from player
        int spawnIndex;
        GameObject spawnPoint;
        spawnIndex = Random.Range(0, spawnPoints.Length);
        spawnPoint = spawnPoints[spawnIndex];
        while (Mathf.Abs(spawnPoint.transform.position.x - playerObject.transform.position.x) < minPlayerDistance ||
               Mathf.Abs(spawnPoint.transform.position.y - playerObject.transform.position.y) < minPlayerDistance)
        {
            spawnIndex = Random.Range(0, spawnPoints.Length);
            spawnPoint = spawnPoints[spawnIndex];
        }

        // Place asteroid
        transform.position = spawnPoint.transform.position;
    }
}
