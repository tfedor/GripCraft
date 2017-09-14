using UnityEngine;

public class Dir
{

	public const int North = 0;
	public const int East = 1;
	public const int South = 2;
	public const int West = 3;

	public static int Opposite(int dir)
	{
		return (dir + 2) % 4;
	}

	public static Vector3 Vector(int dir)
	{
		switch (dir)
		{
			case North: return Vector3.forward;
			case East:  return Vector3.right;
			case South: return Vector3.back;
			case West:  return Vector3.left;
		}
		return Vector3.zero;
	}

	public static int TurnRight(int dir)
	{
		return (dir + 1) % 4;
	}
}
