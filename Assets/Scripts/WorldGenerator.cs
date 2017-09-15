using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
	public GameObject WorldChunkPrefab;

	private readonly Dictionary<Vector3, TerrainChunk> _chunkMap
		= new Dictionary<Vector3, TerrainChunk>();

	private readonly Dictionary<Vector3, short> _hitMap
		= new Dictionary<Vector3, short>();

	private readonly Queue<TerrainChunk> _recompute = new Queue<TerrainChunk>();

	private int _maxHeight = 31;
	private float _seedX;
	private float _seedZ;
	private float _step = 1 / 64f;
	
	void Start ()
	{
		Random.InitState((int)System.DateTime.Now.Ticks);
		_seedX = Random.Range(100f, 999f);
		_seedZ = Random.Range(100f, 999f);
		
		TerrainChunk chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = transform;
		chunk.Visited = true;
		chunk.Generate();
		SaveChunk(chunk);
		
		_recompute.Enqueue(chunk);

		for (var i = 1; i < 5; i++)
		{
			CreateInDiameter(i, chunk);
		}
		
		RecomputeMeshes();
	}

	public int GetHeight(int x, int z)
	{
		var noise = Mathf.PerlinNoise((_seedX + x) * _step, (_seedZ + z) * _step);
		return 1 + Mathf.RoundToInt(1 + noise * _maxHeight);
	}
	
	void SaveChunk(TerrainChunk chunk)
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
		return GetChunk(chunk.transform.position + TerrainChunk.ChunkSize * dir);
	}
	
	public TerrainChunk CreateChunk(Vector3 position)
	{
		TerrainChunk chunk = GetChunk(position);
		if (chunk)
		{
			_recompute.Enqueue(chunk);
			return chunk;
		}
		
		chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = transform;
		chunk.transform.position = position;
		chunk.Generate();
		SaveChunk(chunk);
		
		_recompute.Enqueue(chunk);
		return chunk;
	}

	public void CreateInDiameter(int distance, TerrainChunk origin)
	{
		Vector3 o = origin.transform.position;
		const int chunkSize = TerrainChunk.ChunkSize;
		
		int range = chunkSize*distance;
		
		for (int i = -range; i <= range; i += chunkSize)
		{
			CreateChunk(new Vector3(o.x + i, o.y, o.z + range));
			CreateChunk(new Vector3(o.x + i, o.y, o.z - range));
		}
		for (int i = -range+chunkSize; i < range; i += chunkSize)
		{
			CreateChunk(new Vector3(o.x + range, o.y, o.z + i));
			CreateChunk(new Vector3(o.x - range, o.y, o.z - i));
		}
	}
	
	public void RecomputeMeshes()
	{
		while (_recompute.Count > 0)
		{
			_recompute.Dequeue().RecomputeMesh();
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
		
		return GetChunk(new Vector3(ix - bx, iy - by, iz - bz));
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
	
	public void HitBlock(int y, int x, int z, TerrainChunk chunk)
	{
		Vector3 blockPosition = new Vector3(x,y,z);
		int by = y % TerrainChunk.ChunkSize;
		int bx = x % TerrainChunk.ChunkSize;
		int bz = z % TerrainChunk.ChunkSize;
		
		if (by < 0) { by += TerrainChunk.ChunkSize; }
		if (bx < 0) { bx += TerrainChunk.ChunkSize; }
		if (bz < 0) { bz += TerrainChunk.ChunkSize; }
		
		int hits = 1;
		if (_hitMap.ContainsKey(blockPosition))
		{
			hits += _hitMap[blockPosition];
		}
		
		int hitpoints = Block.Hitpoints(chunk.GetBlock(by, bx, bz));
		
		if (hits >= hitpoints)
		{
			_hitMap.Remove(blockPosition);
			SetBlock(y, x, z, Block.Type.Empty);
		}
		else
		{
			_hitMap[blockPosition] = (short)hits;
		}
	}
	
	public TerrainChunk SetBlock(int y, int x, int z, Block.Type type)
	{
		const int chunkSize = TerrainChunk.ChunkSize;
		
		int by = y % chunkSize;
		int bx = x % chunkSize;
		int bz = z % chunkSize;
		
		if (by < 0) { by += chunkSize; }
		if (bx < 0) { bx += chunkSize; }
		if (bz < 0) { bz += chunkSize; }
		
		Vector3 chunkPos = new Vector3(x - bx, y - by, z - bz);

		TerrainChunk chunk = CreateChunk(chunkPos);
		chunk.SetBlock(by, bx, bz, type);
		
		// if digging down, ensure there's new chunk
		if (type == Block.Type.Empty)
		{
			if (by == 0) {
				CreateChunk(chunkPos + Vector3.down * chunkSize);
			}
			
			TerrainChunk nbr = null;
			if (bx == 0)                  { nbr = GetNbrChunk(Vector3.left, chunk); }
			else if (bx == chunkSize - 1) { nbr = GetNbrChunk(Vector3.right, chunk); }
			if (nbr) { _recompute.Enqueue(nbr); }
			
			if (bz == 0)                  { nbr = GetNbrChunk(Vector3.back, chunk); }
			else if (bz == chunkSize - 1) { nbr = GetNbrChunk(Vector3.forward, chunk); }
			if (nbr) { _recompute.Enqueue(nbr); }
		}

		RecomputeMeshes();
		return chunk;
	}
}
