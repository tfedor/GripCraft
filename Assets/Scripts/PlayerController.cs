using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private const int BuildRange = 5;
	
	private Vector3 _cursor;
	private WorldGenerator _world;

	private CharacterController _controller;
	private readonly float _walkSpeed = 6;
	private readonly float _runSpeed = 12;
	private readonly float _jumpSpeed = 8;
	private readonly float _gravity = 20f;
	private readonly float _airControl = 0.5f;
	private Vector3 _direction = Vector3.zero;

	//
	public GameObject CursorCube;
	private bool _enableBuild = true;
	private bool _buildMode = true;
	private Block.Type _selectedType = Block.Type.Ground;
	
	void Awake()
	{
		_controller = GetComponent<CharacterController>();
	}
	
	void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		_cursor = new Vector3(Camera.main.pixelWidth*0.5f, Camera.main.pixelHeight*0.5f);

		_world = GameObject.Find("WorldOrigin").GetComponent<WorldGenerator>();
	}

	void Move()
	{
		Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		dir = transform.TransformDirection(dir);
		dir *= Input.GetButton("Run") ? _runSpeed : _walkSpeed;

		if (!_controller.isGrounded)
		{
			dir.x *= _airControl;
			dir.z *= _airControl;
			dir.y = _direction.y - _gravity * Time.deltaTime;
		}
		else
		{
			dir.y = Input.GetButton("Jump") ? _jumpSpeed : 0;
		}

		_direction = dir;
		_controller.Move(_direction * Time.deltaTime);
	}

	void Rotate()
	{
		Camera.main.transform.Rotate(Vector3.left, Input.GetAxis("Mouse Y"));
		transform.Rotate(Vector3.up, Input.GetAxis("Mouse X"));
	}

	void BuildOptions()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			_buildMode = !_buildMode;
			Debug.Log("Build Mode: " + _buildMode);
			if (_buildMode)
			{
				Debug.Log("Selected type: " + _selectedType.ToString());
			}
		}
		if      (Input.GetKeyDown(KeyCode.Alpha1)) { _selectedType = Block.Type.Ground; }
		else if (Input.GetKeyDown(KeyCode.Alpha2)) { _selectedType = Block.Type.Stone; }
		else if (Input.GetKeyDown(KeyCode.Alpha3)) { _selectedType = Block.Type.Sand; }
		else if (Input.GetKeyDown(KeyCode.Alpha4)) { _selectedType = Block.Type.Gem; }
		
	}
	
	void Update ()
	{
		Move();
		Rotate();
		BuildOptions();

		
		// ray cast

		Ray ray = Camera.main.ScreenPointToRay(_cursor);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, BuildRange))
		{
			TerrainChunk chunk = hit.transform.GetComponent<TerrainChunk>();


			//
			CursorCube.SetActive(true);
			Vector3 cubePos = hit.point + (_buildMode ? 1 : -1) * hit.normal * TerrainChunk.CubeHalfWidth;
			int x = Mathf.FloorToInt(cubePos.x);
			int y = Mathf.FloorToInt(cubePos.y);
			int z = Mathf.FloorToInt(cubePos.z);
			
			CursorCube.transform.position = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
			
			//
			if (Input.GetButtonDown("Fire1"))
			{
				if (_buildMode) {
					if (_enableBuild) {
						TerrainChunk affectedChunk = _world.SetBlock(y, x, z, _selectedType);
						if (affectedChunk && affectedChunk != chunk)
						{
							chunk.RecomputeMesh();
						}			
					}
				} else {
					_world.SetBlock(y, x, z, Block.Type.Empty);
					
				}
				
			}
		}
		else
		{
			CursorCube.SetActive(false);
		}
		
		//GetCurrentChunkPos(); // TODO
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


	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject == CursorCube)
		{
			CursorCube.GetComponent<MeshRenderer>().enabled = false;
			_enableBuild = false;
		}
	}
	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject == CursorCube)
		{
			CursorCube.GetComponent<MeshRenderer>().enabled = true;
			_enableBuild = true;
		}
	}
}
