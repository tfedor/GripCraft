
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
}
