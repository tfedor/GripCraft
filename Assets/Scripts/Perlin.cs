
using UnityEngine;
using Random = UnityEngine.Random;

public class Perlin
{
    private Vector2[,] seed;
    
    public Perlin(int x, int y)
    {
        seed = new Vector2[x,y];
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                seed[i, j] = Random.insideUnitCircle.normalized;
            }
        }
    }

    public float noise(float x, float y)
    {
        x = x % seed.Length;
        y = y % seed.Length;
        
        Vector2 point = new Vector2(x, y);
        
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float ix1 = Mathf.Lerp(
            Vector2.Dot(seed[x0, y0], new Vector2(x0, y0) - point),
            Vector2.Dot(seed[x1, y0], new Vector2(x1, y0) - point),
            point.x - x0
        );
        float ix2 = Mathf.Lerp(
            Vector2.Dot(seed[x0, y1], new Vector2(x0, y1) - point),
            Vector2.Dot(seed[x1, y1], new Vector2(x1, y1) - point),
            point.x - x0
        );

        return Mathf.Lerp(ix1, ix2, point.y - y0);
    }
}
