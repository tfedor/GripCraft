using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TerrainChunk : MonoBehaviour
{
	public const int ChunkSize = 16;
	public const int RenderDistance = 5 * 16 * 5 * 16;
	
	private WorldGenerator _generator;
	
	public const float CubeHalfWidth = 0.5f;
	
	private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
	private MeshCollider _collider;
	private Mesh _chunk;

	private readonly short[,,] _map = new short[16,16,16];
	
	private readonly List<Vector3> _vertices = new List<Vector3>();
	private readonly List<Vector3> _normals = new List<Vector3>();
	private readonly List<Vector2> _uv = new List<Vector2>();
	private readonly List<int> _triangles = new List<int>();
	private int _v; // vertex index

	// world offset coordinates
	private int _wy;
	private int _wx;
	private int _wz;
	
	//
	public bool Visited = false;

	void Awake ()
    {
	    _meshRenderer = GetComponent<MeshRenderer>();
	    _meshFilter = GetComponent<MeshFilter>();
	    _collider = GetComponent<MeshCollider>();

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
				for (var y = 0; y < ChunkSize; y++)
				{
					int height = _generator.GetHeight(_wx + x, _wz + z);
					if (height > maxHeight) { maxHeight = height; }

					_map[y, x, z] = (short)(_wy + y < height ? Block.Type.Ground : Block.Type.Empty);
				}
			}
		}

		if (maxHeight - _wy > ChunkSize)
		{
			_generator.CreateChunk(transform.position + Vector3.up * ChunkSize);
		}
	}

	void Update()
	{
		float x = transform.position.x - Camera.main.transform.position.x;
		float y = transform.position.z - Camera.main.transform.position.z;
		_meshRenderer.enabled = x*x + y*y < RenderDistance;
	}
	
	public void SetBlock(int y, int x, int z, Block.Type type)
	{
		_map[y, x, z] = (short)type;
	}

	public Block.Type GetBlock(int y, int x, int z)
	{
		return (Block.Type)_map[y, x, z];
	}

	public Block.Type GetWorldBlock(int y, int x, int z)
	{
		return _generator.GetBlock(_wy + y, _wx + x, _wz + z);
	}
	
	private bool IsEmpty(int y, int x, int z)
	{
		return _map[y, x, z] == (int)Block.Type.Empty;
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
		float x0 = Mathf.Clamp01((int) side      * Block.TextureSize);
		float x1 = Mathf.Clamp01((int)(side + 1) * Block.TextureSize);
		float y0 = Mathf.Clamp01((int) type      * Block.TextureSize);
		float y1 = Mathf.Clamp01((int)(type + 1) * Block.TextureSize);
		
		_uv.Add(new Vector2(x0, y0));
		_uv.Add(new Vector2(x0, y1));
		_uv.Add(new Vector2(x1, y1));
		_uv.Add(new Vector2(x1, y0));
	}
	
	private void AddTopFace(int y, int x, int z)
	{
		if (GetWorldBlock(y + 1, x, z) != Block.Type.Empty) { return; }
		
		_vertices.Add(new Vector3(x,   y+1, z));
		_vertices.Add(new Vector3(x,   y+1, z+1));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x+1, y+1, z));
		
		AddNormals(Vector3.up);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Top);
		AddTriangles();
	}
	
	private void AddBottomFace(int y, int x, int z)
	{
		if (GetWorldBlock(y - 1, x, z) != Block.Type.Empty) { return; }
		
		_vertices.Add(new Vector3(x+1, y, z));
		_vertices.Add(new Vector3(x+1, y, z+1));
		_vertices.Add(new Vector3(x,   y, z+1));
		_vertices.Add(new Vector3(x,   y, z));
		
		AddNormals(Vector3.down);		
		AddUv((Block.Type)_map[y,x,z], Block.Side.Bottom);
		AddTriangles();
	}

	private void AddNorthFace(int y, int x, int z)
	{
		if (GetWorldBlock(y, x, z + 1) != Block.Type.Empty) { return; }
		
		_vertices.Add(new Vector3(x+1, y,   z+1));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x,   y+1, z+1));
		_vertices.Add(new Vector3(x,   y,   z+1));
		
		AddNormals(Vector3.forward);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Side);
		AddTriangles();
	}

	private void AddSouthFace(int y, int x, int z)
	{
		if (GetWorldBlock(y, x, z - 1) != Block.Type.Empty) { return; }
		
		_vertices.Add(new Vector3(x,   y,   z));
		_vertices.Add(new Vector3(x,   y+1, z));
		_vertices.Add(new Vector3(x+1, y+1, z));
		_vertices.Add(new Vector3(x+1, y,   z));
		
		AddNormals(Vector3.back);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Side);
		AddTriangles();
	}
	
	private void AddWestFace(int y, int x, int z)
	{
		if (GetWorldBlock(y, x - 1, z) != Block.Type.Empty) { return; }
		
		_vertices.Add(new Vector3(x, y,   z+1));
		_vertices.Add(new Vector3(x, y+1, z+1));
		_vertices.Add(new Vector3(x, y+1, z));
		_vertices.Add(new Vector3(x, y, z));
		
		AddNormals(Vector3.left);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Side);
		AddTriangles();
	}

	private void AddEastFace(int y, int x, int z)
	{
		if (GetWorldBlock(y, x + 1, z) != Block.Type.Empty) { return; }
		
		_vertices.Add(new Vector3(x+1, y,   z));
		_vertices.Add(new Vector3(x+1, y+1, z));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x+1, y,   z+1));
		
		AddNormals(Vector3.right);
		AddUv((Block.Type)_map[y,x,z], Block.Side.Side);
		AddTriangles();
	}

	public void RecomputeMesh()
	{	
		for (var y = 0; y < ChunkSize; y++) // layers
		{
			for (var x = 0; x < ChunkSize; x++)
			{
				for (var z = 0; z < ChunkSize; z++)
				{
					if (IsEmpty(y, x, z)) { continue; }
					
					AddTopFace(y,x,z);
					AddBottomFace(y,x,z);
					AddNorthFace(y,x,z);
					AddSouthFace(y,x,z);
					AddWestFace(y,x,z);
					AddEastFace(y,x,z);		
				}
			}
		}
		
		_meshFilter.mesh.Clear();
		_meshFilter.mesh.vertices = _vertices.ToArray();
		_meshFilter.mesh.normals = _normals.ToArray();
		_meshFilter.mesh.uv = _uv.ToArray();
		_meshFilter.mesh.triangles = _triangles.ToArray();
		
		_collider.sharedMesh = _meshFilter.mesh;
		
		_vertices.Clear();
		_triangles.Clear();
		_normals.Clear();
		_uv.Clear();
		_v = 0;
	}
}
