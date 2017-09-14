using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
	public GameObject WorldChunkPrefab;

	private TerrainChunk _originChunk;
	private Dictionary<Tuple<int, int, int>, TerrainChunk> _chunkMap; 

	void Start ()
	{
		_originChunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		
		TerrainChunk prev = _originChunk;
		for (var i = 1; i < 5; i++)
		{
			CreateInDiameter(i, prev);
			prev = prev.GetNbr(Dir.North);
		}
		
	}

	/**
	 * When creating new chunk, check whether there isn't a neighbor already.
	 * When creating chunk north, then check whether parent doesn't have west/east neihgbor
	 * and whether that neighbor doesn't have north neighbor
	 * Similarly for other directions
	 */
	void CheckSecondNbrs(TerrainChunk parent, int firstDir, int secondDir, TerrainChunk newChunk)
	{
		TerrainChunk nbr = parent.GetNbr(firstDir);
		if (nbr)
		{
			nbr = nbr.GetNbr(secondDir);
			if (nbr)
			{
				nbr.SetNbr(Dir.Opposite(firstDir), newChunk);
				newChunk.SetNbr(firstDir, nbr);
			}
		}
	}
	
	TerrainChunk CreateChunk(int dir, TerrainChunk parent)
	{
		if (parent.GetNbr(dir)) { return null; }
		
		TerrainChunk chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = parent.transform.parent;
		chunk.transform.position = parent.transform.position + TerrainChunk.ChunkSize * Dir.Vector(dir);

		parent.SetNbr(dir, chunk);
		chunk.SetNbr(Dir.Opposite(dir), parent);
		
		if (Dir.IsVertical(dir))
		{
			CheckSecondNbrs(parent, Dir.West, dir, chunk);
			CheckSecondNbrs(parent, Dir.East, dir, chunk);
		}
		else
		{
			CheckSecondNbrs(parent, Dir.North, dir, chunk);
			CheckSecondNbrs(parent, Dir.South, dir, chunk);
		}

		return chunk;
	}

	void CreateInDiameter(int level, TerrainChunk parent)
	{
		int count = 4*level*2;
		int steps = level;
		
		parent = CreateChunk(Dir.North, parent);
		int dir = Dir.East;
		for (var i=0; i < count; i++)
		{
			parent = CreateChunk(dir, parent);
			
			if (--steps == 0)
			{
				dir = Dir.TurnRight(dir);
				steps = level*2;
			}
		}
	}
}
