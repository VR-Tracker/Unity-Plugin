using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRT_LerpPosCol : MonoBehaviour
{
    /// <summary>
    /// This script will lerp position (up and down) and color of an object
    /// </summary>

    [SerializeField] Renderer rend;
    [SerializeField] Color originalColor, targetColor, fromColor, toColor;
    bool fading;
    float timer = 0;
    Vector3 defaultPosition, downPosition, fromPosition, toPosition;
    [SerializeField] float offsetDown = -1.32f;
    bool setPosition = false;

    private void Awake()
    {
        originalColor = rend.materials[0].color;
    }   

    public void LerpColor(bool fadeOut)
    {
        //Debug.Log("lerp color " + fadeOut);
        fading = false;
        timer = 0;

        if (fadeOut)
        {
            fromColor = originalColor;
            toColor = targetColor;
        }
        else
        {
            fromColor = targetColor;
            toColor = originalColor;
        }
        LerpPosition(fadeOut);

        fading = true;
    }

    void LerpPosition(bool fadeOut)
    {
        if (!setPosition)
        {
            setPosition = true;
            defaultPosition = transform.position;
            downPosition = new Vector3(defaultPosition.x, offsetDown, defaultPosition.z);
        }

        if (fadeOut)
        {
            fromPosition = defaultPosition;
            toPosition = downPosition;
        }
        else
        {
            fromPosition = downPosition;
            toPosition = defaultPosition;
        }
    }

    private void Update()
    {
        if (fading)
        {
            if (timer < 1)
            {
                timer += Time.deltaTime;
                rend.materials[0].color = Color.Lerp(fromColor, toColor, timer);
                transform.localPosition = Vector3.Lerp(fromPosition, toPosition, timer);
            }
            else
            {
                timer = 0;
                fading = false;
            }
        }
    }
}

