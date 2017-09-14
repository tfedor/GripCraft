using System;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{

	public GameObject OptionPrefab;

	private Text _mode;
	
	private Text[] _selectedList;
	private int _selected;

	void Awake()
	{
		_mode = transform.Find("Mode").GetComponent<Text>();
		
		Transform optionList = transform.Find("Options");

		_selectedList = new Text[Enum.GetNames(typeof(Block.Type)).Length - 1];
		
		for (int i = 0; i < 4; i++)
		{
			RectTransform option = Instantiate(OptionPrefab).GetComponent<RectTransform>();
			option.transform.parent = optionList;
			option.anchoredPosition = new Vector3(0, i * option.sizeDelta.y * -1.2f, 0);

			option.GetComponent<Text>().text = (i + 1).ToString();

			_selectedList[i] = option.transform.GetChild(0).GetComponent<Text>();
			_selectedList[i].text = "";
			
			_selectedList[i].transform.GetChild(0).GetComponent<Text>().text = ((Block.Type)i).ToString();
		}
	}
	
	public void SelectType(Block.Type type)
	{
		_selectedList[_selected].text = "";
		_selected = (int) type;
		_selectedList[_selected].text = "X";
	}

	public void SetMode(bool isBuildMode)
	{
		_mode.text = isBuildMode ? "Build" : "Destroy";
	}
	
}
