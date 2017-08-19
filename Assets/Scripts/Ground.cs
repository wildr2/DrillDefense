using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    private SpriteRenderer sRenderer;
    private Sprite sprite;
    private Texture2D tex;

    private void Awake()
    {
        sRenderer = GetComponent<SpriteRenderer>();

        tex = new Texture2D(100, 100);
        //FillTexture(tex, Color.red);
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        sRenderer.sprite = sprite;
    }


    private static void FillTexture(Texture2D tex, Color color)
    {
        Color32[] colors = tex.GetPixels32();

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }

        tex.SetPixels32(colors);
        tex.Apply();
    }
}
