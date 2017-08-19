using UnityEngine;
using System.Collections.Generic;

/*
Screen space canvases will appear ontop of the bars (camera space ones will not).


*/

public class AspectEnforcer : MonoBehaviour
{
    public Color bars_color = Color.black;
    public float aspect_ratio = 16.0f / 9.0f;
    public bool show_bars_in_editor = false;
    public bool expand_camera = false;

    private int ScreenSizeX = 0;
    private int ScreenSizeY = 0;
    

    private void Start()
    {
        RescaleCamera();
    }
    private void Update()
    {
        RescaleCamera();
    }
    private void OnPreCull()
    {
        if (expand_camera || (Application.isEditor && !show_bars_in_editor)) return;

        Rect wp = Camera.main.rect;
        Rect nr = new Rect(0, 0, 1, 1);

        Camera.main.rect = nr;

        GL.Clear(true, true, bars_color);

        Camera.main.rect = wp;
    }

    private void RescaleCamera()
    {

        if (Screen.width == ScreenSizeX && Screen.height == ScreenSizeY) return;

        float windowaspect = (float)Screen.width / (float)Screen.height;
        float scaleheight = windowaspect / aspect_ratio;
        Camera camera = GetComponent<Camera>();

       if (scaleheight < 1.0f)
        {
            // Screen is too tall
            if (expand_camera)
            {
                camera.orthographicSize = 9 * 1 / scaleheight;
            }
            else
            {
                // Letter boxes
                Rect rect = camera.rect;

                rect.width = 1.0f;
                rect.height = scaleheight;
                rect.x = 0;
                rect.y = (1.0f - scaleheight) / 2.0f;

                camera.rect = rect;
            }
        }
        else
        {
            // Screen is too wide
            if (expand_camera)
            {

            }
            else
            {
                // Pilar boxes
                float scalewidth = 1.0f / scaleheight;

                Rect rect = camera.rect;

                rect.width = scalewidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scalewidth) / 2.0f;
                rect.y = 0;

                camera.rect = rect;
            }
        }

        ScreenSizeX = Screen.width;
        ScreenSizeY = Screen.height;
    }

}