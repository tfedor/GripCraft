using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
	public GameObject WorldChunkPrefab;

	private readonly Dictionary<Vector3, TerrainChunk> _chunkMap
		= new Dictionary<Vector3, TerrainChunk>(); 

	void Start ()
	{
		TerrainChunk chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = transform;
		SetChunk(chunk);
		
		TerrainChunk prev = chunk;
		for (var i = 1; i < 5; i++)
		{
			CreateInDiameter(i, prev.transform);
			prev = GetNbrChunk(Dir.North, prev);
		}
	}
	
	void SetChunk(TerrainChunk chunk)
	{
		var pos = chunk.transform.position;
		_chunkMap[pos] = chunk;
	}
	
	TerrainChunk GetChunk(Vector3 position)
	{
		if (!_chunkMap.ContainsKey(position)) { return null; }
		return _chunkMap[position];
	}

	TerrainChunk GetNbrChunk(int dir, TerrainChunk chunk)
	{
		return GetChunk(GetNbrChunkPosition(dir, chunk.transform));
	}

	Vector3 GetNbrChunkPosition(int dir, Transform chunk)
	{
		return chunk.position + TerrainChunk.ChunkSize * Dir.Vector(dir);
	}

	TerrainChunk CreateChunk(int dir, Transform parent)
	{
		var chunkPos = GetNbrChunkPosition(dir, parent);
		
		TerrainChunk chunk = GetChunk(chunkPos);
		if (GetChunk(chunkPos))
		{
			return chunk;
		}
		
		chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = parent.transform.parent;
		chunk.transform.position = chunkPos;
		SetChunk(chunk);
		return chunk;
	}

	public void CreateInDiameter(int level, Transform parent)
	{
		int count = 4*level*2;
		int steps = level;
		
		parent = CreateChunk(Dir.North, parent).transform;
		int dir = Dir.East;
		for (var i=0; i < count; i++)
		{
			parent = CreateChunk(dir, parent).transform;
			
			if (--steps == 0)
			{
				dir = Dir.TurnRight(dir);
				steps = level*2;
			}
		}
	}
}
