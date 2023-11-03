using UnityEngine;

static class Helper
{
    public static Texture2D CurveToGradient(AnimationCurve animationCurve, int outputWidth)
    {
        Texture2D texture = new Texture2D(outputWidth, 1);
        for (int x = 0; x < outputWidth; x++)
        {
            float v = animationCurve.Evaluate(x / (outputWidth - 1f));
            texture.SetPixel(x, 0, new Color(v, v, v));
        }
        texture.Apply();
        return texture;
    }
}