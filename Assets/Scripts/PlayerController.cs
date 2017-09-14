using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private const int BuildRange = 5;
	
	private Vector3 _cursor;
	private WorldGenerator _world;

	void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		_cursor = new Vector3(Camera.main.pixelWidth*0.5f, Camera.main.pixelHeight*0.5f);

		_world = GameObject.Find("WorldOrigin").GetComponent<WorldGenerator>();
	}

	public void GetCurrentChunkPos()
	{
		TerrainChunk chunk = _world.GetChunkAtPosition(transform.position.x, 0, transform.position.z);
		if (chunk && !chunk.Visited)
		{
			_world.CreateInDiameter(4, chunk.transform);
			chunk.Visited = true;
		}
	}
	
	void Update ()
	{
		// TODO
		// translate
		transform.Translate(
			Input.GetAxis("Horizontal"),
			0,
			Input.GetAxis("Vertical")
		);
		
		transform.Translate(
			0,
			Input.GetKey(KeyCode.LeftShift)
				? 1
				: (Input.GetKey(KeyCode.LeftControl)
					? -1
					: 0),
			0,
			Space.World);
		
		// rotate
		transform.rotation = Quaternion.Euler(
			transform.rotation.eulerAngles.x - Input.GetAxis("Mouse Y"),
			transform.rotation.eulerAngles.y + Input.GetAxis("Mouse X"),
			0
		);
		
		// ray cast

		Ray ray = Camera.main.ScreenPointToRay(_cursor);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, BuildRange))
		{
			TerrainChunk chunk = hit.transform.GetComponent<TerrainChunk>();

			if (Input.GetButtonDown("Fire1"))
			{
				Vector3 cubePos = hit.point - hit.normal * TerrainChunk.CubeHalfWidth;
				int x = Mathf.FloorToInt(cubePos.x);
				int y = Mathf.FloorToInt(cubePos.y);
				int z = Mathf.FloorToInt(cubePos.z);

				_world.SetBlock(y, x, z, 0);
			}
			else if (Input.GetButtonDown("Fire2"))
			{
				Vector3 cubePos = hit.point + hit.normal * TerrainChunk.CubeHalfWidth;
				int x = Mathf.FloorToInt(cubePos.x);
				int y = Mathf.FloorToInt(cubePos.y);
				int z = Mathf.FloorToInt(cubePos.z);

				if (_world.SetBlock(y, x, z, 1) != chunk)
				{
					Debug.Log("different chunk, redraw");
					chunk.RecomputeMesh();
				}
				
			}
		}
		
		GetCurrentChunkPos();
	}
}
