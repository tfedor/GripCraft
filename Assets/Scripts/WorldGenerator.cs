using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldGenerator : MonoBehaviour
{
	public const int InitialWorldSize = 7;
	public const int RenderDistance = 7 * 16;
	public const int MaxHeight = 128;

	private const float RecomputeTimeLimit = 0.01f;
	
	public GameObject WorldChunkPrefab;

	private readonly Dictionary<Vector3, TerrainChunk> _chunkMap
		= new Dictionary<Vector3, TerrainChunk>();

	private readonly Dictionary<Vector3, char> _hitMap
		= new Dictionary<Vector3, char>();

	private readonly HashSet<TerrainChunk> _recompute = new HashSet<TerrainChunk>();

	private readonly Queue<Vector3> _lightAddQ = new Queue<Vector3>();
	private readonly Queue<Vector3> _lightDelQ = new Queue<Vector3>();

	private int _worldSeed;
	private float[] _seedX;
	private float[] _seedZ;
	private float[] _scale;
	private float[] _power;
	private float _powerScale;

	private float _heightSeedX;
	private float _heightSeedZ;
	private float _heightScale;

	private float _blockSeedX;
	private float _blockSeedZ;

	private bool _loading = false;

	private void InitiateWorld()
	{
		Random.InitState(_worldSeed);

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

		_blockSeedX = Random.Range(1000f, 9999f);
		_blockSeedZ = Random.Range(1000f, 9999f);

		_heightSeedX = Random.Range(1000f, 9999f);
		_heightSeedZ = Random.Range(1000f, 9999f);
		_heightScale = 1/512f;
	}
	
	void Start ()
	{
		_worldSeed = (int) System.DateTime.Now.Ticks;
		InitiateWorld();
		
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

	public float GetBlockRand(int x, int z)
	{
		return Mathf.PerlinNoise(_blockSeedX + x, _blockSeedZ + z);
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
	public IEnumerator RecomputeMeshesCoroutine()
	{
		TerrainChunk[] chunks = _recompute.ToArray();
		_recompute.Clear();

		float st = Time.realtimeSinceStartup;
		foreach (TerrainChunk chunk in chunks)
		{
			chunk.RecomputeMesh();
			if (Time.realtimeSinceStartup - st > RecomputeTimeLimit)
			{
				yield return null;
				st = Time.realtimeSinceStartup;
			}
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
	
	public Block.Type GetBlock(int x, int y, int z)
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
		
		return chunk.GetBlock(bx, @by, bz);
	}
	
	public short GetLightLevel(int x, int y, int z)
	{
		int bx = x % TerrainChunk.ChunkSize;
		int by = y % TerrainChunk.ChunkSize;
		int bz = z % TerrainChunk.ChunkSize;
		
		if (bx < 0) { bx += TerrainChunk.ChunkSize; }
		if (by < 0) { by += TerrainChunk.ChunkSize; }
		if (bz < 0) { bz += TerrainChunk.ChunkSize; }
		
		Vector3 chunkPos = new Vector3(x - bx, y - by, z - bz);

		TerrainChunk chunk = GetChunk(chunkPos);
		if (!chunk) { return 0; }
		
		return chunk.GetLightLevel(bx, by, bz);
	}
	public void SetLightLevel(int x, int y, int z, short level)
	{
		int bx = x % TerrainChunk.ChunkSize;
		int by = y % TerrainChunk.ChunkSize;
		int bz = z % TerrainChunk.ChunkSize;
		
		if (bx < 0) { bx += TerrainChunk.ChunkSize; }
		if (by < 0) { by += TerrainChunk.ChunkSize; }
		if (bz < 0) { bz += TerrainChunk.ChunkSize; }
		
		Vector3 chunkPos = new Vector3(x - bx, y - by, z - bz);

		TerrainChunk chunk = GetChunk(chunkPos);
		if (!chunk) { return; }
		
		chunk.SetLightLevel(bx, by, bz, level);
	}
	
	public Block.Type HitBlock(int x, int y, int z, TerrainChunk chunk)
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

		Block.Type type = chunk.GetBlock(bx, @by, bz);
		int hitpoints = Block.Hitpoints(type);
		
		if (hits >= hitpoints)
		{
			_hitMap.Remove(blockPosition);
			SetBlock(x, y, z, Block.Type.Empty);
			return Block.Type.Empty;
		}
		
		_hitMap[blockPosition] = (char)hits;
		return type;
	}
	
	public TerrainChunk SetBlock(int x, int y, int z, Block.Type type)
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
		
		if (type == Block.Type.Empty)
		{
			if (chunk.GetLightLevel(bx, by, bz) != 0)
			{
				_lightDelQ.Enqueue(new Vector3(x,y,z));
				ComputeRemoveLights();
			}
			
			chunk.SetLightLevel(bx,by,bz,0);
			chunk.SetBlock(bx, by, bz, type);

			if (!_loading)
			{
				if (by <= 1)                  { CreateChunk(chunkPos + Vector3.down * chunkSize, true); }
				else if (by == chunkSize - 1)
				{
					TerrainChunk upNbr = GetChunk(chunkPos + Vector3.up * chunkSize);
					if (upNbr) { MarkToRecompute(upNbr); }
				}
				
				if (bx == 0)                  { CreateChunk(chunkPos + Vector3.left * chunkSize, true); }
				else if (bx == chunkSize - 1) { CreateChunk(chunkPos + Vector3.right * chunkSize, true); }
				
				if (bz == 0)                  { CreateChunk(chunkPos + Vector3.back * chunkSize, true); }
				else if (bz == chunkSize - 1) { CreateChunk(chunkPos + Vector3.forward * chunkSize, true); }
			}
			
			// lights
			_lightAddQ.Enqueue(new Vector3(x-1,y,z));
			_lightAddQ.Enqueue(new Vector3(x+1,y,z));
			_lightAddQ.Enqueue(new Vector3(x,y-1,z));
			_lightAddQ.Enqueue(new Vector3(x,y+1,z));
			_lightAddQ.Enqueue(new Vector3(x,y,z-1));
			_lightAddQ.Enqueue(new Vector3(x,y,z+1));
		} else {
			_lightDelQ.Enqueue(new Vector3(x,y,z));
			ComputeRemoveLights();

			chunk.SetLightLevel(bx, by, bz, Block.Light[type]);
			if (Block.Light[type] != 0)
			{
				_lightAddQ.Enqueue(new Vector3(x,y,z));	
			}
			
			chunk.SetBlock(bx, by, bz, type);
		}
		
		ComputeLights();
		RecomputeMeshes();
		return chunk;
	}
	
	public void ComputeLights()
	{
		while (_lightAddQ.Count > 0)
		{
			Vector3 v = _lightAddQ.Dequeue();

			int x = (int)v.x;
			int y = (int)v.y;
			int z = (int)v.z;

			Block.Type type = GetBlock(x, y, z);
			if (type != Block.Type.Empty && type != Block.Type.Gem) { continue; }
			
			short nbrLight;
			short newLight = (short)(GetLightLevel(x, y, z) - 1);

			nbrLight = GetLightLevel(x - 1, y, z);
			if (nbrLight < newLight) { _lightAddQ.Enqueue(new Vector3(x - 1, y, z)); SetLightLevel(x - 1, y, z, newLight); } 
			
			nbrLight = GetLightLevel(x + 1, y, z);
			if (nbrLight < newLight) { _lightAddQ.Enqueue(new Vector3(x + 1, y, z)); SetLightLevel(x + 1, y, z, newLight); }
			
			nbrLight = GetLightLevel(x, y, z - 1);
			if (nbrLight < newLight) { _lightAddQ.Enqueue(new Vector3(x, y, z - 1)); SetLightLevel(x, y, z - 1, newLight); }
			
			nbrLight = GetLightLevel(x, y, z + 1);
			if (nbrLight < newLight) { _lightAddQ.Enqueue(new Vector3(x, y, z + 1)); SetLightLevel(x, y, z + 1, newLight); }
			
			nbrLight = GetLightLevel(x, y + 1, z);
			if (nbrLight < newLight) { _lightAddQ.Enqueue(new Vector3(x, y + 1, z)); SetLightLevel(x, y + 1, z, newLight); }

			if (newLight == TerrainChunk.MaxLight - 1) { newLight = TerrainChunk.MaxLight; }
			nbrLight = GetLightLevel(x, y - 1, z);
			if (nbrLight < newLight) { _lightAddQ.Enqueue(new Vector3(x, y - 1, z)); SetLightLevel(x, y - 1, z, newLight); }
			
			TerrainChunk chunk = GetChunkAtPosition(x, y, z);
			if (chunk != null) { MarkToRecompute(chunk); } // TODO no need to recompute complete mesh, just lights
		}
	}

	public void ComputeRemoveLights()
	{
		
		while (_lightDelQ.Count > 0)
		{
			Vector3 v = _lightDelQ.Dequeue();
			int x = (int)v.x;
			int y = (int)v.y;
			int z = (int)v.z;
		
			short nbrLight;
			short curLight = GetLightLevel(x, y, z);
			
			if (curLight == 0) { continue; }
			
			nbrLight = GetLightLevel(x - 1, y, z);
			if (nbrLight < curLight) { _lightDelQ.Enqueue(new Vector3(x - 1, y, z)); SetLightLevel(x,y,z,0); }
			else                     { _lightAddQ.Enqueue(new Vector3(x - 1, y, z)); } 
			
			nbrLight = GetLightLevel(x + 1, y, z);
			if (nbrLight < curLight) { _lightDelQ.Enqueue(new Vector3(x + 1, y, z)); SetLightLevel(x,y,z,0); }
			else                     { _lightAddQ.Enqueue(new Vector3(x + 1, y, z)); }
			nbrLight = GetLightLevel(x, y, z - 1);
			if (nbrLight < curLight) { _lightDelQ.Enqueue(new Vector3(x, y, z - 1)); SetLightLevel(x,y,z,0); }
			else                     { _lightAddQ.Enqueue(new Vector3(x, y, z - 1)); }
			
			nbrLight = GetLightLevel(x, y, z + 1);
			if (nbrLight < curLight) { _lightDelQ.Enqueue(new Vector3(x, y, z + 1)); SetLightLevel(x,y,z,0); }
			else                     { _lightAddQ.Enqueue(new Vector3(x, y, z + 1)); }
			
			nbrLight = GetLightLevel(x, y + 1, z);
			if (nbrLight < curLight) { _lightDelQ.Enqueue(new Vector3(x, y + 1, z)); SetLightLevel(x,y,z,0); }
			else                     { _lightAddQ.Enqueue(new Vector3(x, y + 1, z)); }
			
			nbrLight = GetLightLevel(x, y - 1, z);
			if (nbrLight < curLight || (curLight == TerrainChunk.MaxLight && nbrLight == TerrainChunk.MaxLight))
			                         { _lightDelQ.Enqueue(new Vector3(x, y - 1, z)); SetLightLevel(x,y,z,0); }
			else                     { _lightAddQ.Enqueue(new Vector3(x, y - 1, z)); }

			TerrainChunk chunk = GetChunkAtPosition(x, y, z);
			if (chunk != null) { MarkToRecompute(chunk); } // TODO no need to recompute complete mesh, just lights
		}
	}

	public void Save(BinaryWriter writer)
	{
		writer.Write(_worldSeed);

		// hit map
		writer.Write(_hitMap.Count);
		foreach (KeyValuePair<Vector3, char> pair in _hitMap)
		{
			writer.Write((int) pair.Key.x);
			writer.Write((int) pair.Key.y);
			writer.Write((int) pair.Key.z);
			writer.Write(pair.Value);
		}
		
		// chunks
		writer.Write(_chunkMap.Count);
		foreach (KeyValuePair<Vector3, TerrainChunk> pair in _chunkMap)
		{
			writer.Write((int) pair.Key.x);
			writer.Write((int) pair.Key.y);
			writer.Write((int) pair.Key.z);
			pair.Value.SaveState(writer);
		}
	}

	public void Load(BinaryReader reader)
	{
		_loading = true;
		
		foreach (TerrainChunk chunk in _chunkMap.Values) { Destroy(chunk.gameObject); }
		_chunkMap.Clear();
		_recompute.Clear();
		_hitMap.Clear();
		_lightAddQ.Clear();
		_lightDelQ.Clear();
		
		_worldSeed = reader.ReadInt32();		
		InitiateWorld();
		
		// hits
		int count = reader.ReadInt32();
		for (int i = 0; i < count; i++)
		{
			int x = reader.ReadInt32();
			int y = reader.ReadInt32();
			int z = reader.ReadInt32();
			
			_hitMap[new Vector3(x, y, z)] = reader.ReadChar();
		}
		
		// chunks
		count = reader.ReadInt32();
		for (int i = 0; i < count; i++)
		{
			int x = reader.ReadInt32();
			int y = reader.ReadInt32();
			int z = reader.ReadInt32();
			
			TerrainChunk chunk = Instantiate(WorldChunkPrefab).GetComponent<TerrainChunk>();
			chunk.transform.parent = transform;
			chunk.transform.position = new Vector3(x,y,z);
			chunk.Generate();
			SaveChunk(chunk);
			MarkToRecompute(chunk);		
			
			chunk.ReplayState(reader);		
		}
		
		RecomputeMeshes();

		_loading = false;
	}
}
