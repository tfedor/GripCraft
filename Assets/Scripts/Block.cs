﻿
using UnityEngine;

public class Block
{
	public const float TextureSize = 0.25f; 
	
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

	public static Type GetType(int height)
	{
		float rand = Random.value;
		if (height < -20 - rand * 30) { return Type.Gem; }
		if (height <   5 - rand *  3) { return Type.Stone; }
		if (height <  20 - rand *  6) { return Type.Sand; }
		if (height <  80 - rand * 20) { return Type.Ground; }
		return Type.Stone;
	}
}
