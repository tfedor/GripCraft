using System.Collections.Generic;
using System.Configuration;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
	public const int InitialWorldSize = 15;
	public const int RenderDistance = 7 * 16;
	public const int MaxHeight = 128;
	
	public GameObject WorldChunkPrefab;

	private readonly Dictionary<Vector3, TerrainChunk> _chunkMap
		= new Dictionary<Vector3, TerrainChunk>();

	private readonly Dictionary<Vector3, short> _hitMap
		= new Dictionary<Vector3, short>();

	private readonly HashSet<TerrainChunk> _recompute = new HashSet<TerrainChunk>();

	private float[] _seedX;
	private float[] _seedZ;
	private float[] _scale;
	private float[] _power;
	private float _powerScale;

	private float _heightSeedX;
	private float _heightSeedZ;
	private float _heightScale;
	
	void Start ()
	{
		Random.InitState((int)System.DateTime.Now.Ticks); 

		_scale = new[] {1/128f, 1/64f, 1/64f, 1/16f, 1/8f};
		_power = new[] { 0.8f,     1f,  0.4f,  0.2f, 0.1f};
		_powerScale = 0;
		
		_seedX = new float[_scale.Length];
		_seedZ = new float[_scale.Length];
		for (int i = 0; i < _scale.Length; i++) {
			_seedX[i] = Random.Range(1000f, 9999f);
			_seedZ[i] = Random.Range(1000f, 9999f);
			_powerScale += _power[i];
		}

		_heightSeedX = Random.Range(1000f, 9999f);
		_heightSeedZ = Random.Range(1000f, 9999f);
		_heightScale = 1/512f;
		
		TerrainChunk chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = transform;
		chunk.Visited = true;
		chunk.Generate();
		SaveChunk(chunk);
		
		MarkToRecompute(chunk);
		
		for (var i = 1; i < InitialWorldSize; i++)
		{
			CreateInDiameter(i, chunk);
		}
		
		RecomputeMeshes();
	}

	public int GetHeight(int x, int z)
	{

		float noise = 0;
		
		int levels = _scale.Length;
		for (int i = 0; i < levels; i++)
		{
			noise += _power[i]
			         * Mathf.PerlinNoise(
				         (_seedX[i] + x) * _scale[i],
				         (_seedZ[i] + z) * _scale[i]
					 );
		}

		noise /= _powerScale;

		float maxHeight = MaxHeight * Mathf.PerlinNoise(
			(_heightSeedX + x) * _heightScale,
			(_heightSeedZ + z) * _heightScale
		);
		
		return 1 + Mathf.RoundToInt(noise * maxHeight);
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
	
	public TerrainChunk CreateChunk(Vector3 position, bool recomputeExisting)
	{
		TerrainChunk chunk = GetChunk(position);
		if (chunk)
		{
			if (recomputeExisting)
			{
				MarkToRecompute(chunk);
			}
			return chunk;
		}
		
		chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
		chunk.transform.parent = transform;
		chunk.transform.position = position;
		chunk.Generate();
		SaveChunk(chunk);

		MarkToRecompute(chunk);
		
		TerrainChunk nbr;
		nbr = GetNbrChunk(Vector3.forward, chunk);
		if (nbr) { MarkToRecompute(nbr); }
		
		nbr = GetNbrChunk(Vector3.back, chunk);
		if (nbr) { MarkToRecompute(nbr); }
		
		nbr = GetNbrChunk(Vector3.left, chunk);
		if (nbr) { MarkToRecompute(nbr); }
		
		nbr = GetNbrChunk(Vector3.right, chunk);
		if (nbr) { MarkToRecompute(nbr); }
		
		return chunk;
	}

	public void CreateInDiameter(int distance, TerrainChunk origin)
	{
		Vector3 o = origin.transform.position;
		const int chunkSize = TerrainChunk.ChunkSize;
		
		int range = chunkSize*distance;
		
		for (int i = -range; i <= range; i += chunkSize)
		{
			CreateChunk(new Vector3(o.x + i, o.y, o.z + range), false);
			CreateChunk(new Vector3(o.x + i, o.y, o.z - range), false);
		}
		for (int i = -range+chunkSize; i < range; i += chunkSize)
		{
			CreateChunk(new Vector3(o.x + range, o.y, o.z + i), false);
			CreateChunk(new Vector3(o.x - range, o.y, o.z - i), false);
		}
	}

	private void MarkToRecompute(TerrainChunk chunk)
	{
		_recompute.Add(chunk);
	}
	
	public void RecomputeMeshes()
	{
		foreach (TerrainChunk chunk in _recompute)
		{
			chunk.RecomputeMesh();
		}
		_recompute.Clear();
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

		TerrainChunk chunk = CreateChunk(chunkPos, true);
		chunk.SetBlock(by, bx, bz, type);
		
		// if digging down, ensure there's new chunk
		if (type == Block.Type.Empty)
		{
			if (by == 0) {
				CreateChunk(chunkPos + Vector3.down * chunkSize, true);
			}
			
			if (bx == 0)                  { CreateChunk(chunkPos + Vector3.left * chunkSize, true); }
			else if (bx == chunkSize - 1) { CreateChunk(chunkPos + Vector3.right * chunkSize, true); }
			
			if (bz == 0)                  { CreateChunk(chunkPos + Vector3.back * chunkSize, true); }
			else if (bz == chunkSize - 1) { CreateChunk(chunkPos + Vector3.forward * chunkSize, true); }
		}

		RecomputeMeshes();
		return chunk;
	}
}
