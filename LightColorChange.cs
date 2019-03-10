/*
	Loops through different colors for a constant color changing effect.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightColorChange : MonoBehaviour
{
    public float colorRate;

    private Light light;
    private bool increaseComp; // T for inc, F for dec
    private int changeColor; // which comp to change r = 1, g = 2, b = 3
    void Awake()
    {
        light = GetComponent<Light>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*
         * The light will cycle through these colors.
         * Rgb
         * RGb
         * rGb
         * rGB
         * rgB
         * RgB
         * Note: a capital letter represents fully (1) that component color, 
         * while a lowercase letter represents only partially (or 0) of that 
         * component color.
         */
        float redComp = light.color.r,
              greenComp = light.color.g,
              blueComp = light.color.b;
        if (redComp >= 1 && greenComp <= 0 && blueComp <= 0)
        {
            increaseComp = true;
            changeColor = 2;
        }
        else if (redComp >= 1 && greenComp >= 1 && blueComp <= 0)
        {
            increaseComp = false;
            changeColor = 1;
        }
        else if (redComp <= 0 && greenComp >= 1 && blueComp <= 0)
        {
            increaseComp = true;
            changeColor = 3;
        }
        else if (redComp <= 0 && greenComp >= 1 && blueComp >= 1)
        {
            increaseComp = false;
            changeColor = 2;
        }
        else if (redComp <= 0 && greenComp <= 0 && blueComp >= 1)
        {
            increaseComp = true;
            changeColor = 1;
        }
        else if (redComp >= 1 && greenComp <= 0 && blueComp >= 1)
        {
            increaseComp = false;
            changeColor = 3;
        }

        float changeAmount = Time.deltaTime * colorRate;
        if (!increaseComp)
            changeAmount *= -1;

        if (changeColor == 1)
            redComp += changeAmount;
        else if (changeColor == 2)
            greenComp += changeAmount;
        else
            blueComp += changeAmount;

        light.color = new Color(redComp, greenComp, blueComp);
    }
}
