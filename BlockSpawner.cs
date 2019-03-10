/* 
	Spawns gameobjects in a fun pattern for the blocks at the beginning
	of a brick breaker level.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour {
    public AudioSource blockSpawnSFX;

    public GameObject[] Blocks;
    public bool spawningDone = false;
    public SpriteRenderer[] BlockRenderers;

    private int spawnIndex = 0;
    public float spawnRate = 0f;

	// Use this for initialization
	void Start () {
        // Each element is given each block's sprite renderer, and each is set to 0 opaque
        BlockRenderers = new SpriteRenderer[Blocks.Length];
        for (int i = 0; i < Blocks.Length; i++)
        {
            SpriteRenderer BlockRend = Blocks[i].GetComponent<SpriteRenderer>();
            BlockRenderers[i] = BlockRend;
            BlockRend.color = new Color(BlockRend.color.r, BlockRend.color.g, BlockRend.color.b, 0);
        }
	}
	
	// Update is called once per frame
	void Update () {
        // Makes the first invisible block more solid. When a block becomes fully visible, index is incremented, so this will work on the next block 
        // when called next.
        lerpSpawnRate();
        if (spawnIndex < Blocks.Length-1)
        {
            Color CurrentColor = BlockRenderers[spawnIndex].color;
            float newOpaqueLevel = CurrentColor.a + (Time.deltaTime * spawnRate);
            if (newOpaqueLevel > 1f)
                newOpaqueLevel = 1f;   
            BlockRenderers[spawnIndex].color = new Color(CurrentColor.r, CurrentColor.g, CurrentColor.b, newOpaqueLevel);
            if (newOpaqueLevel == 1f)
            {
                spawnIndex++;
                blockSpawnSFX.Play(0);
            }
        }
        else if (!spawningDone) // if at the last block, do something special and kill script
            StartCoroutine(SpawnLastAndEnd());
	}

    IEnumerator SpawnLastAndEnd()
    {
        spawningDone = true;
        yield return new WaitForSeconds(0.4f);
        Color CurrentColor = BlockRenderers[spawnIndex].color;
        BlockRenderers[spawnIndex].color = new Color(CurrentColor.r, CurrentColor.g, CurrentColor.b, 1f);
        this.enabled = false;
        blockSpawnSFX.Play(0);
    }

    void lerpSpawnRate()
    {
        if (spawnRate != 0f)
            spawnRate = Mathf.Lerp(spawnRate, Constants.S.maxBlockSpawnRate, Constants.S.blockSpawnLerpConstant);
        else
            spawnRate = Constants.S.startBlockSpawnRate;
    }
}
