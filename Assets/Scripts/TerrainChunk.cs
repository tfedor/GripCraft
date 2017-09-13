using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine; 
	
public class TerrainChunk : MonoBehaviour
{

    private MeshFilter _meshFilter;
    private Mesh _chunk;
	
	const int ChunkSize = 4;
	private short[,,] _map = {
		{
			{1,1,1,1},
			{1,1,1,1},
			{1,1,1,1},
			{1,1,1,1}
		}, {
			{0,0,0,0},
			{0,1,1,0},
			{0,0,1,0},
			{0,0,0,0}
		}, {
			{0,0,0,0},
			{0,1,1,0},
			{0,1,1,0},
			{0,0,0,0}
		}, {
			{0,0,0,0},
			{0,0,1,0},
			{0,0,0,0},
			{0,0,0,0}
		}
	};

	List<Vector3> _vertices;
	List<Vector3> _normals;
	List<Vector2> _uv;
	List<int> _triangles;
	
	private int _v; // vertex index

    void Start ()
    {
        _meshFilter = GetComponent<MeshFilter>();
	    
	    _vertices = new List<Vector3>();
	    _normals = new List<Vector3>();
	    _uv = new List<Vector2>();
	    _triangles = new List<int>();
	    
	    Generate();
    }

	private bool isEmpty(int y, int x, int z)
	{
		return _map[y, x, z] == 0; // TODO block type
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

	private void AddUv() // TODO type, side
	{
		_uv.Add(new Vector2(0,    3/4f));
		_uv.Add(new Vector2(0,    1));
		_uv.Add(new Vector2(1/4f, 1));
		_uv.Add(new Vector2(1/4f, 3/4f));
	}
	
	private void AddTopFace(int y, int x, int z)
	{
		if (y != ChunkSize - 1 && _map[y + 1, x, z] != 0) { return; }
		
		_vertices.Add(new Vector3(x,   y+1, z));
		_vertices.Add(new Vector3(x,   y+1, z+1));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x+1, y+1, z));
		
		AddNormals(Vector3.up);
		AddUv();
		AddTriangles();
	}
	
	private void AddBottomFace(int y, int x, int z)
	{
		if (y != 0 && _map[y - 1, x, z] != 0) { return; }
		
		_vertices.Add(new Vector3(x+1, y, z));
		_vertices.Add(new Vector3(x+1, y, z+1));
		_vertices.Add(new Vector3(x,   y, z+1));
		_vertices.Add(new Vector3(x,   y, z));
		
		AddNormals(Vector3.down);		
		AddUv();
		AddTriangles();
	}

	private void AddNorthFace(int y, int x, int z)
	{
		if (z != ChunkSize - 1 && _map[y, x, z + 1] != 0) { return; }
		
		_vertices.Add(new Vector3(x+1, y,   z+1));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x,   y+1, z+1));
		_vertices.Add(new Vector3(x,   y,   z+1));
		
		AddNormals(Vector3.forward);
		AddUv();
		AddTriangles();
	}

	private void AddSouthFace(int y, int x, int z)
	{
		if (z != 0 && _map[y, x, z - 1] != 0) { return; }
		
		_vertices.Add(new Vector3(x,   y,   z));
		_vertices.Add(new Vector3(x,   y+1, z));
		_vertices.Add(new Vector3(x+1, y+1, z));
		_vertices.Add(new Vector3(x+1, y,   z));
		
		AddNormals(Vector3.back);
		AddUv();
		AddTriangles();
	}
	
	private void AddWestFace(int y, int x, int z)
	{
		if (x != 0 && _map[y, x - 1, z] != 0) { return; }
		
		_vertices.Add(new Vector3(x, y,   z+1));
		_vertices.Add(new Vector3(x, y+1, z+1));
		_vertices.Add(new Vector3(x, y+1, z));
		_vertices.Add(new Vector3(x, y, z));
		
		AddNormals(Vector3.left);
		AddUv();
		AddTriangles();
	}

	
	private void AddEastFace(int y, int x, int z)
	{
		if (x != ChunkSize - 1 && _map[y, x + 1, z] != 0) { return; }
		
		_vertices.Add(new Vector3(x+1, y,   z));
		_vertices.Add(new Vector3(x+1, y+1, z));
		_vertices.Add(new Vector3(x+1, y+1, z+1));
		_vertices.Add(new Vector3(x+1, y,   z+1));
		
		AddNormals(Vector3.right);
		AddUv();
		AddTriangles();
	}

	private void Generate()
	{
		for (var y = 0; y < ChunkSize; y++) // layers
		{
			for (var x = 0; x < ChunkSize; x++)
			{
				for (var z = 0; z < ChunkSize; z++)
				{
					if (isEmpty(y, x, z)) { continue; }
					
					AddTopFace(y,x,z);
					AddBottomFace(y,x,z);
					AddNorthFace(y,x,z);
					AddSouthFace(y,x,z);
					AddWestFace(y,x,z);
					AddEastFace(y,x,z);		
				}
			}
		}
		
		Mesh mesh = new Mesh();
		mesh.vertices = _vertices.ToArray();
		mesh.triangles = _triangles.ToArray();
		mesh.normals = _normals.ToArray();
		mesh.uv = _uv.ToArray();
		_meshFilter.mesh = mesh;
	}

}
