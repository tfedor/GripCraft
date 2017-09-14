using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private const int BuildRange = 5;
	
	private Vector3 _cursor;
	private Vector3 _prevChunk;

	void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		_cursor = new Vector3(Camera.main.pixelWidth*0.5f, Camera.main.pixelHeight*0.5f);
		
		 
	}

	public Vector3 GetCurrentChunkPos()
	{
		// TODO
		_prevChunk = new Vector3(
			Mathf.FloorToInt(transform.position.x)%TerrainChunk.ChunkSize,
			Mathf.FloorToInt(transform.position.y)%TerrainChunk.ChunkSize,
			Mathf.FloorToInt(transform.position.z)%TerrainChunk.ChunkSize
		);
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
				chunk.Hit(hit);
			} else if (Input.GetButtonDown("Fire2"))
			{
				chunk.Add(hit);
			}
		}	
	}
}
