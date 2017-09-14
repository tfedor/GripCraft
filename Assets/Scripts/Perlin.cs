
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
        x = x % 9;
        y = y % 9;
        
        Vector2 point = new Vector2(x, y);
        
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        if (x0 < 0) { x0 += 9; }
        if (x1 < 0) { x1 += 9; }
        
        if (y0 < 0) { y0 += 9; }
        if (y1 < 0) { y1 += 9; }
        
        
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
