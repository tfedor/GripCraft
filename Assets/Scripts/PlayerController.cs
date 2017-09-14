using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private const int BuildRange = 5;
	
	private Vector3 _cursor;
	private Vector3 _prevChunk;

	private Transform _lastChunk;

	private WorldGenerator _generator;

	void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		_cursor = new Vector3(Camera.main.pixelWidth*0.5f, Camera.main.pixelHeight*0.5f);

		_generator = GameObject.Find("WorldOrigin").GetComponent<WorldGenerator>();
	}

	public void GetCurrentChunkPos()
	{
		Ray ray = new Ray(transform.position, Vector3.down);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 100))
		{
			if (_lastChunk != hit.transform)
			{
				_lastChunk = hit.transform;
				_generator.CreateInDiameter(3, hit.transform);
				
				// TODO set visited chunk
			}
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
				chunk.Hit(hit);
			} else if (Input.GetButtonDown("Fire2"))
			{
				chunk.Add(hit);
			}
		}
		
		GetCurrentChunkPos();
	}
}
