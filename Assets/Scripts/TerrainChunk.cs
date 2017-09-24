using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
	public const int ChunkSize = 16;
	
	private WorldGenerator _generator;
	
	public const float CubeHalfWidth = 0.5f;
	
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	private MeshCollider _meshCollider;
	
	private readonly Block.Type[,,] _map = new Block.Type[16,16,16];
	
	private readonly List<Vector3> _vertices = new List<Vector3>();
	private readonly List<Vector3> _normals = new List<Vector3>();
	private readonly List<Vector2> _uv = new List<Vector2>();
	private readonly List<int> _triangles = new List<int>();
	private int _v; // vertex index

	// world offset coordinates
	private int _wy;
	private int _wx;
	private int _wz;

	private int _faces;
	
	//
	public bool Visited = false;

	// state
	private readonly Dictionary<short,Block.Type> _changes = new Dictionary<short, Block.Type>();
	
	void Awake ()
    {
	    _meshRenderer = GetComponent<MeshRenderer>();
	    _meshFilter = GetComponent<MeshFilter>();
	    _meshCollider = GetComponent<MeshCollider>();

	    _meshFilter.mesh = new Mesh();
    }
	
	public void Generate()
	{
		_wy = (int) transform.position.y;
		_wx = (int) transform.position.x;
		_wz = (int) transform.position.z;

		_generator = transform.parent.gameObject.GetComponent<WorldGenerator>();
		
		int maxHeight = 0;
		
		for (var x = 0; x < ChunkSize; x++)
		{
			for (var z = 0; z < ChunkSize; z++)
			{
				int height = _generator.GetHeight(_wx + x, _wz + z);
				if (height > maxHeight) { maxHeight = height; }
				
				for (var y = 0; y < ChunkSize; y++)
				{
					_map[y, x, z] = (_wy + y < height
						? Block.GetType(_wy + y, height, _generator.GetBlockRand(_wx + x, _wz + z))
						: Block.Type.Empty
					);
				}
			}
		}

		// generate new chunk above if needed
		if (maxHeight - _wy > ChunkSize)
		{
			_generator.CreateChunk(transform.position + Vector3.up * ChunkSize, true);
		}
	}

	void Update()
	{
		// manhattan
		float x = Mathf.Abs(transform.position.x - Camera.main.transform.position.x);
		float z = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);

		if (x > WorldGenerator.RenderDistance)
		{
			_meshRenderer.enabled = false;
			return;
		}
		if (z > WorldGenerator.RenderDistance)
		{
			_meshRenderer.enabled = false;
			return;
		}
		_meshRenderer.enabled = true;
		
		/**
		Unneccesarily complex?
		// more precise, in circle
		_meshRenderer.enabled = x*x + z*z < WorldGenerator.RenderDistance * WorldGenerator.RenderDistance;

		// view cone
		float x = transform.position.x - Camera.main.transform.position.x;
		float y = transform.position.z - Camera.main.transform.position.z;
		float dist = x*x + y*y;

		_meshRenderer.enabled = dist < WorldGenerator.RenderDistance;
		
		// minimal radius
		if (dist < WorldGenerator.MinRenderDistance)
		{
			_meshRenderer.enabled = true;
			return;
		}

		// distance we can see
		if (dist > WorldGenerator.RenderDistance)
		{
			_meshRenderer.enabled = false;
			return;
		}
		
		// angle we can see
		Vector2 a = new Vector2(
			Camera.main.transform.position.x - transform.position.x,
			Camera.main.transform.position.z - transform.position.z
		);
		Vector2 b = new Vector2(Camera.main.transform.forward.x, Camera.main.transform.forward.z);
		_meshRenderer.enabled = Vector2.Angle(a, b) > 120;
		*/
	}
	
	public void SetBlock(int y, int x, int z, Block.Type type)
	{
		_map[y, x, z] = type;
		
		// save action
		short key = (short) (
			((y & 15) << 8) |
			((x & 15) << 4) |
			 (z & 15)
		);
		_changes[key] = type;
	}

	public Block.Type GetBlock(int y, int x, int z)
	{
		return (Block.Type)_map[y, x, z];
	}

	private bool IsNbrBlockEmpty(int y, int x, int z, TerrainChunk nbr)
	{
		if (nbr == null)
		{
			return _wy + y >= _generator.GetHeight(_wx + x, _wz + z);
		}
		
		if (y < 0)          { return Block.Type.Empty == nbr.GetBlock(ChunkSize - 1, x, z); }
		if (x < 0)          { return Block.Type.Empty == nbr.GetBlock(y, ChunkSize - 1, z); }
		if (z < 0)          { return Block.Type.Empty == nbr.GetBlock(y, x, ChunkSize - 1); }
		if (y >= ChunkSize) { return Block.Type.Empty == nbr.GetBlock(0, x, z); }
		if (x >= ChunkSize) { return Block.Type.Empty == nbr.GetBlock(y, 0, z); }
		if (z >= ChunkSize) { return Block.Type.Empty == nbr.GetBlock(y, x, 0); }
		
		return false;
	}
	
	private void AddTriangles()
	{
		_v += 4;

		_triangles.Add(_v-4);
		_triangles.Add(_v-3);
		_triangles.Add(_v-2);
						
		_triangles.Add(_v-2);
		_triangles.Add(_v-1);
		_triangles.Add(_v-4);
	}

	private void AddNormals(Vector3 normal)
	{
		for (var i = 0; i < 4; i++)
		{
			_normals.Add(normal);
		}
	}

	private void AddUv(Block.Type type, Block.Side side)
	{
		float x0 = Mathf.Clamp01((int) side      * Block.TextureSize + Block.TextureOffset);
		float x1 = Mathf.Clamp01((int)(side + 1) * Block.TextureSize - Block.TextureOffset);
		float y0 = Mathf.Clamp01((int) type      * Block.TextureSize + Block.TextureOffset);
		float y1 = Mathf.Clamp01((int)(type + 1) * Block.TextureSize - Block.TextureOffset);
		
		_uv.Add(new Vector2(x0, y0));
		_uv.Add(new Vector2(x0, y1));
		_uv.Add(new Vector2(x1, y1));
		_uv.Add(new Vector2(x1, y0));
	}
	
	private void AddTopFace(int y, int x, int z)
	{
		_vertices.Add(new Vector3(x,   y+1, z));
		_vertices.Add(new Vector3(x,   y+1, z+1));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x+1, y+1, z));
		
		AddNormals(Vector3.up);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Top);
		AddTriangles();

		_faces = _faces | 1;
	}
	
	private void AddBottomFace(int y, int x, int z)
	{
		_vertices.Add(new Vector3(x+1, y, z));
		_vertices.Add(new Vector3(x+1, y, z+1));
		_vertices.Add(new Vector3(x,   y, z+1));
		_vertices.Add(new Vector3(x,   y, z));
		
		AddNormals(Vector3.down);		
		AddUv((Block.Type)_map[y,x,z], Block.Side.Bottom);
		AddTriangles();
		
		_faces = _faces | (1 << 1);
	}

	private void AddNorthFace(int y, int x, int z)
	{
		_vertices.Add(new Vector3(x+1, y,   z+1));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x,   y+1, z+1));
		_vertices.Add(new Vector3(x,   y,   z+1));
		
		AddNormals(Vector3.forward);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Side);
		AddTriangles();
		
		_faces = _faces | (1 << 2);
	}

	private void AddSouthFace(int y, int x, int z)
	{
		_vertices.Add(new Vector3(x,   y,   z));
		_vertices.Add(new Vector3(x,   y+1, z));
		_vertices.Add(new Vector3(x+1, y+1, z));
		_vertices.Add(new Vector3(x+1, y,   z));
		
		AddNormals(Vector3.back);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Side);
		AddTriangles();
		
		_faces = _faces | (1 << 3);
	}
	
	private void AddWestFace(int y, int x, int z)
	{
		_vertices.Add(new Vector3(x, y,   z+1));
		_vertices.Add(new Vector3(x, y+1, z+1));
		_vertices.Add(new Vector3(x, y+1, z));
		_vertices.Add(new Vector3(x, y, z));
		
		AddNormals(Vector3.left);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Side);
		AddTriangles();
		
		_faces = _faces | (1 << 4);
	}

	private void AddEastFace(int y, int x, int z)
	{
		_vertices.Add(new Vector3(x+1, y,   z));
		_vertices.Add(new Vector3(x+1, y+1, z));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x+1, y,   z+1));
		
		AddNormals(Vector3.right);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Side);
		AddTriangles();
		
		_faces = _faces | (1 << 5);
	}

	public void RecomputeMesh()
	{
		TerrainChunk chunkBellow = _generator.GetNbrChunk(Vector3.down, this);
		TerrainChunk chunkTop = _generator.GetNbrChunk(Vector3.up, this);
		TerrainChunk chunkNorth = _generator.GetNbrChunk(Vector3.forward, this);
		TerrainChunk chunkSouth = _generator.GetNbrChunk(Vector3.back, this);
		TerrainChunk chunkEast = _generator.GetNbrChunk(Vector3.right, this);
		TerrainChunk chunkWest  = _generator.GetNbrChunk(Vector3.left, this);
		
		_faces = 0;
		
		for (var x = 0; x < ChunkSize; x++)
		{
			for (var z = 0; z < ChunkSize; z++)
			{
				bool prevEmpty = chunkBellow != null && IsNbrBlockEmpty(0,x,z, chunkBellow);
 
				for (var y = 0; y < ChunkSize; y++) // layers
				{
					bool currEmpty = (Block.Type)_map[y, x, z] == Block.Type.Empty;
					if      ( prevEmpty && !currEmpty)          { AddBottomFace(y,x,z); }
					else if (!prevEmpty &&  currEmpty && y > 0) { AddTopFace(y-1,x,z); }

					prevEmpty = currEmpty;
				}

				if (!prevEmpty && IsNbrBlockEmpty(ChunkSize,x,z, chunkTop))
				{
					AddTopFace(ChunkSize-1,x,z);
				}
			}
		}
		
		for (var y = 0; y < ChunkSize; y++)
		{
			for (var x = 0; x < ChunkSize; x++)
			{
				bool prevEmpty = IsNbrBlockEmpty(y,x,-1, chunkSouth);
				
				for (var z = 0; z < ChunkSize; z++)
				{
					bool currEmpty = (Block.Type)_map[y, x, z] == Block.Type.Empty;
					if      ( prevEmpty && !currEmpty)          { AddSouthFace(y,x,z); }
					else if (!prevEmpty &&  currEmpty && z > 0) { AddNorthFace(y,x,z-1); }
					prevEmpty = currEmpty;
				}
				
				if (!prevEmpty && IsNbrBlockEmpty(y,x,ChunkSize, chunkNorth))
				{
					AddNorthFace(y,x,ChunkSize-1);
				}
			}
			
			for (var z = 0; z < ChunkSize; z++)
			{
				bool prevEmpty = IsNbrBlockEmpty(y, -1, z, chunkWest);
				
				for (var x = 0; x < ChunkSize; x++)
				{
					bool currEmpty = (Block.Type)_map[y, x, z] == Block.Type.Empty;
					if       (prevEmpty && !currEmpty)          { AddWestFace(y,x,z); }
					else if (!prevEmpty &&  currEmpty && x > 0) { AddEastFace(y,x-1,z); }
					prevEmpty = currEmpty;
				}
				
				if (!prevEmpty && IsNbrBlockEmpty(y, ChunkSize, z, chunkEast))
				{
					AddEastFace(y, ChunkSize-1, z);
				}	
			}
		}
		
		_meshFilter.mesh.Clear();
		_meshFilter.mesh.vertices = _vertices.ToArray();
		_meshFilter.mesh.normals = _normals.ToArray();
		_meshFilter.mesh.uv = _uv.ToArray();
		_meshFilter.mesh.triangles = _triangles.ToArray();
		
		_meshCollider.sharedMesh = _meshFilter.mesh;
		_meshCollider.convex = _faces != 0 && (_faces & (_faces - 1)) == 0; // if there's only one side, mark mesh as convex
		
		_vertices.Clear();
		_triangles.Clear();
		_normals.Clear();
		_uv.Clear();
		_v = 0;
	}

	public void SaveState(BinaryWriter writer)
	{
		writer.Write((short)_changes.Count);
		foreach (KeyValuePair<short, Block.Type> data in _changes)
		{
			writer.Write((short)((data.Key << 4) | ((short)data.Value & 15)));
		}
	}

	public void ReplayState(BinaryReader reader)
	{
		short changes = reader.ReadInt16();
		for (int i = 0; i < changes; i++)
		{
			short data = reader.ReadInt16();
			int y = (data & (15 << 12)) >> 12;
			int x = (data & (15 <<  8)) >>  8;
			int z = (data & (15 <<  4)) >>  4;
			Block.Type type = (Block.Type)(data & 15);
			
			SetBlock(y, x, z, type);
		}
	}
}
