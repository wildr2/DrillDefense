using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[ExecuteInEditMode]
public class EditorRectGuide : MonoBehaviour
{
    private RectTransform rect;
    public string input;
    public string output;

	private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }
    private void Update()
    {
        if (input != "")
        {
            try
            {
                input = input.Replace("f", "");
                input = input.Replace(" ", "");
                string[] split = input.Split(new char[] { ',' });
                rect.localPosition = new Vector2(float.Parse(split[0]), float.Parse(split[1]));
                rect.sizeDelta = new Vector2(float.Parse(split[2]), float.Parse(split[3]));
            }
            catch { }
            input = "";
        }

        output = rect.localPosition.x + "f, " + rect.localPosition.y + "f, "
            + rect.rect.width + "f, " + rect.rect.height + "f";
    }
}
