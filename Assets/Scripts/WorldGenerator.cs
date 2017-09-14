using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
	public GameObject WorldChunkPrefab;

	private Vector3[] _directions = {Vector3.forward, Vector3.right, Vector3.back, Vector3.left}; 
	
	private readonly Dictionary<Vector3, TerrainChunk> _chunkMap
		= new Dictionary<Vector3, TerrainChunk>();

	public Perlin perlin;
	
	void Start ()
	{
		perlin = new Perlin(10, 10);
		
		TerrainChunk chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = transform;
		chunk.Generate();
		chunk.Visited = true;
		SetChunk(chunk);

		CreateLayers(5, chunk);
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

	public TerrainChunk GetNbrChunk(Vector3 dir, TerrainChunk chunk)
	{
		return GetChunk(GetNbrChunkPosition(dir, chunk.transform));
	}

	public Vector3 GetNbrChunkPosition(Vector3 dir, Transform chunk)
	{
		return chunk.position + TerrainChunk.ChunkSize * dir;
	}

	public TerrainChunk CreateChunk(Vector3 dir, Transform parent)
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
		chunk.Generate();
		SetChunk(chunk);

		parent.GetComponent<TerrainChunk>().RecomputeMesh();
		
		return chunk;
	}
	public TerrainChunk CreateChunk(Vector3 position)
	{
		TerrainChunk chunk = GetChunk(position);
		if (GetChunk(position))
		{
			return chunk;
		}
		
		chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = transform;
		chunk.transform.position = position;
		chunk.Generate();
		SetChunk(chunk);
		
		return chunk;
	}

	public void CreateInDiameter(int level, Transform parent)
	{
		int count = 4*level*2;
		int steps = level;
		
		parent = CreateChunk(_directions[0], parent).transform;
		int dir = 1;
		for (var i=0; i < count; i++)
		{
			parent = CreateChunk(_directions[dir%4], parent).transform;
			
			if (--steps == 0)
			{
				dir++;
				steps = level*2;
			}
		}
	}

	public void CreateLayers(int layers, TerrainChunk origin)
	{
		for (var i = 1; i < layers; i++)
		{
			CreateInDiameter(i, origin.transform);
			origin = GetNbrChunk(Vector3.forward, origin);
		}
	}


	// in world coordinates

	public TerrainChunk GetChunkAtPosition(float x, float y, float z)
	{
		int ix = Mathf.FloorToInt(x);
		int iy = Mathf.FloorToInt(y);
		int iz = Mathf.FloorToInt(z);
		
		int by = iy % TerrainChunk.ChunkSize;
		int bx = ix % TerrainChunk.ChunkSize;
		int bz = iz % TerrainChunk.ChunkSize;
		
		if (by < 0) { by += TerrainChunk.ChunkSize; }
		if (bx < 0) { bx += TerrainChunk.ChunkSize; }
		if (bz < 0) { bz += TerrainChunk.ChunkSize; }
		
		Vector3 chunkPos = new Vector3(ix - bx, iy - by, iz - bz);
		
		if (!_chunkMap.ContainsKey(chunkPos)) { return null; }
		return _chunkMap[chunkPos];
	}
	
	public Block.Type GetBlock(int y, int x, int z)
	{
		int by = y % TerrainChunk.ChunkSize;
		int bx = x % TerrainChunk.ChunkSize;
		int bz = z % TerrainChunk.ChunkSize;
		
		if (by < 0) { by += TerrainChunk.ChunkSize; }
		if (bx < 0) { bx += TerrainChunk.ChunkSize; }
		if (bz < 0) { bz += TerrainChunk.ChunkSize; }
		
		Vector3 chunkPos = new Vector3(x - bx, y - by, z - bz);

		TerrainChunk chunk = GetChunk(chunkPos);
		if (!chunk) { return Block.Type.Empty; }
		
		return chunk.GetBlock(by, bx, bz);
	}
	
	public TerrainChunk SetBlock(int y, int x, int z, Block.Type type)
	{
		int by = y % TerrainChunk.ChunkSize;
		int bx = x % TerrainChunk.ChunkSize;
		int bz = z % TerrainChunk.ChunkSize;
		
		if (by < 0) { by += TerrainChunk.ChunkSize; }
		if (bx < 0) { bx += TerrainChunk.ChunkSize; }
		if (bz < 0) { bz += TerrainChunk.ChunkSize; }
		
		Vector3 chunkPos = new Vector3(x - bx, y - by, z - bz);

		TerrainChunk chunk = CreateChunk(chunkPos);
		chunk.SetBlock(by, bx, bz, type);
		
		Debug.Log(by);
		
		// if digging down, ensure there's new chunk
		if (type == Block.Type.Empty && by == 0)
		{
			CreateChunk(Vector3.down, chunk.transform).RecomputeMesh();
		}

		if (type == Block.Type.Empty)
		{
			TerrainChunk nbr = null;
			if (bx == 0)                               { nbr = GetNbrChunk(Vector3.left, chunk); }
			else if (bx == TerrainChunk.ChunkSize - 1) { nbr = GetNbrChunk(Vector3.right, chunk); }
			if (nbr) { nbr.RecomputeMesh(); }
			
			if (bz == 0)                               { nbr = GetNbrChunk(Vector3.back, chunk); }
			else if (bz == TerrainChunk.ChunkSize - 1) { nbr = GetNbrChunk(Vector3.forward, chunk); }
			if (nbr) { nbr.RecomputeMesh(); }
		}
		
		chunk.RecomputeMesh();
		return chunk;
	}
}
