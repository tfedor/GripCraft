using System.IO;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private const int BuildRange = 5;
	private const int GenerateDistance = 4;
	
	private Vector3 _cursor;
	private WorldGenerator _world;
	private UIController _ui;

	private CharacterController _controller;
	private readonly float _walkSpeed = 6;
	private readonly float _runSpeed = 12;
	private readonly float _jumpSpeed = 7;
	private readonly float _gravity = 20f;
	private readonly float _airControl = 0.5f;
	private Vector3 _direction = Vector3.zero;

	//
	public GameObject CursorCube;
	private bool _enableBuild = true;
	private bool _buildMode = true;
	private Block.Type _selectedType = Block.Type.Ground;

	private float _lastClick = 0;
	public const float ClickRate = 0.2f;
	
	//
	private TerrainChunk _prevChunk = null;
	private bool _didChangeChunk = true;
	
	void Awake()
	{
		_controller = GetComponent<CharacterController>();
		
		Cursor.lockState = CursorLockMode.Locked;
		_cursor = new Vector3(Camera.main.pixelWidth*0.5f, Camera.main.pixelHeight*0.5f);

		_world = GameObject.Find("WorldOrigin").GetComponent<WorldGenerator>();
		_ui = GameObject.Find("UI").GetComponent<UIController>();
	}

	void Start()
	{
		_ui.SetMode(_buildMode);
		_ui.SelectType(_selectedType);
		
		transform.position = new Vector3(
			0.5f, 
			_world.GetHeight(0, 0) + _controller.radius + _controller.height * 0.5f,
			0.5f
		);
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
		bool prevMode = _buildMode;
		_buildMode = Input.GetButton("Fire2");
		if (prevMode != _buildMode)
		{
			_ui.SetMode(_buildMode);
			
			CursorCube.transform.localScale = _buildMode ? Vector3.one : Vector3.one * 1.001f;
		}
		if      (Input.GetKeyDown(KeyCode.Alpha1)) { _selectedType = (Block.Type)0; _ui.SelectType(_selectedType); }
		else if (Input.GetKeyDown(KeyCode.Alpha2)) { _selectedType = (Block.Type)1; _ui.SelectType(_selectedType); }
		else if (Input.GetKeyDown(KeyCode.Alpha3)) { _selectedType = (Block.Type)2; _ui.SelectType(_selectedType); }
		else if (Input.GetKeyDown(KeyCode.Alpha4)) { _selectedType = (Block.Type)3; _ui.SelectType(_selectedType); }
	}

	void Cast()
	{
		Ray ray = Camera.main.ScreenPointToRay(_cursor);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, BuildRange))
		{
			TerrainChunk chunk = hit.transform.GetComponent<TerrainChunk>();
			if (chunk == null) { return; }

			//
			CursorCube.SetActive(true);
			Vector3 cubePos = hit.point + (_buildMode ? 1 : -1) * hit.normal * TerrainChunk.CubeHalfWidth;
			int x = Mathf.FloorToInt(cubePos.x);
			int y = Mathf.FloorToInt(cubePos.y);
			int z = Mathf.FloorToInt(cubePos.z);
			
			CursorCube.transform.position = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
			CursorCube.transform.rotation = Quaternion.LookRotation(hit.normal);
			
			//
			if (Input.GetButton("Fire1") && Time.time - _lastClick > ClickRate)
			{
				if (_buildMode) {
					if (_enableBuild) {
						TerrainChunk affectedChunk = _world.SetBlock(x, y, z, _selectedType);
						if (affectedChunk != null && affectedChunk != chunk)
						{
							chunk.RecomputeMesh();
						}			
					}
				}
				else
				{
					Block.Type type = _world.HitBlock(x, y, z, chunk);
					if (type != Block.Type.Empty)
					{
						ParticleSystem ps = CursorCube.GetComponent<ParticleSystem>();
						ParticleSystem.MainModule settings = ps.main;
						settings.startColor = new ParticleSystem.MinMaxGradient( Block.GetParticleColor(type) );
						ps.Emit(10);
					}
				}

				_lastClick = Time.time;
			}
		}
		else
		{
			CursorCube.SetActive(false);
		}
	}
	
	void Update ()
	{
	    if (Input.GetKeyUp(KeyCode.Escape))
		{
			Application.Quit();
		}
	
		Move();
		Rotate();
		BuildOptions();
		Cast();
		UpdateWorld();

		if (Input.GetKeyUp(KeyCode.F5)) { Save("gamesave.gsf"); }
		if (Input.GetKeyUp(KeyCode.F9)) { Load("gamesave.gsf"); }
	}
	
	void UpdateWorld()
	{
		
		TerrainChunk chunk = _world.GetChunkAtPosition(transform.position.x, 0, transform.position.z);
		if (chunk)
		{
			if (!chunk.Visited)
			{
				_world.CreateInDiameter(GenerateDistance, chunk);
				chunk.Visited = true;
				
				//_world.RecomputeMeshes();
				StartCoroutine(_world.RecomputeMeshesCoroutine());
			}

			_didChangeChunk = chunk != _prevChunk;
			_prevChunk = chunk;
		}		
	}

	public bool DidChangeChunk()
	{
		return _didChangeChunk;
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

	void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			Cursor.lockState = CursorLockMode.Locked;
		}
	}
	
	
	public void Save(string filename)
	{
		using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
		{
			_world.Save(writer);
			
			writer.Write(transform.position.x);
			writer.Write(transform.position.y);
			writer.Write(transform.position.z);
			
			writer.Write(transform.rotation.x);
			writer.Write(transform.rotation.y);
			writer.Write(transform.rotation.z);
			writer.Write(transform.rotation.w);
		}
		Debug.Log("Saved");
	}

	public void Load(string filename)
	{
		
		if (!File.Exists(filename))
		{
			Debug.Log("Load file doesn't exist");
			return;
		}
		
		using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
		{
			_world.Load(reader);

			transform.position = new Vector3(
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle()
			);
			
			transform.rotation = new Quaternion(
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle()
			);
		}
		
		Debug.Log("Loaded");
	}
}
