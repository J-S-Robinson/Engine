using System;

namespace Engine.Graphics
{
    public static class CircleTextureGenerator
    {
        public static byte[] MakeFilledCircleRgba(int diameter)
        {
            var bytes = new byte[diameter * diameter * 4];

            float r = diameter / 2f;
            float cx = r;
            float cy = r;
            const float aa = 1.0f;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - cx + 0.5f;
                    float dy = y - cy + 0.5f;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    float alphaF = (r - dist + (aa * 0.5f)) / aa;
                    alphaF = MathF.Min(1f, MathF.Max(0f, alphaF));
                    byte a = (byte)(alphaF * 255f);

                    int i = (y * diameter + x) * 4;
                    bytes[i + 0] = 255;
                    bytes[i + 1] = 255;
                    bytes[i + 2] = 255;
                    bytes[i + 3] = a;
                }
            }

            return bytes;
        }
    }
}