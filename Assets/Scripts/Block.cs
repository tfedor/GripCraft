
using UnityEngine;

public class Block
{
	public const float TextureSize = 0.25f;
	public const float TextureOffset = 0.5f * (1 / 64f); // Size of Texture in pixels
	
	public enum Side
	{
		Top = 0,
		Side = 1,
		Bottom = 2
	}
	
	public enum Type
	{
		Empty = -1,
		Ground = 3,
		Sand = 2,
		Stone = 1,
		Gem = 0
	}

	public static int Hitpoints(Type type)
	{
		switch(type)
		{
			case Type.Ground: return 3;
			case Type.Sand: return 1;
			case Type.Stone: return 5;
			case Type.Gem: return 10;
		}
		return 0;
	}

	public static Type GetType(int blockHeight, int height)
	{
		float rand = Random.value;
		if (blockHeight < -20 - rand * 30) { return Type.Gem; }
		if (blockHeight <   5 - rand *  3) { return Type.Stone; }

		if (height < 25)
		{
			if (blockHeight < 20 - rand * 6) { return Type.Sand; }
		}
		
		if (blockHeight < 80 - rand * 20) { return Type.Ground; }
		return Type.Stone;
	}

	public static Color GetParticleColor(Type type)
	{
		switch(type)
		{
			case Type.Ground: return new Color32(0,128,0,255);
			case Type.Sand:   return new Color32(255,216,0,255);
			case Type.Stone:  return new Color32(56,56,56,255);
			case Type.Gem:    return new Color32(106,0,124,255);
		}
		return Color.white;
	}
}
