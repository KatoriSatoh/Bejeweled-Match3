using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboMgr : MonoBehaviour {
	public static ComboMgr instance;

	public GameObject comboPrefab;
	public List<Sprite> numSprite = new List<Sprite> ();

	void Start() {
		instance = GetComponent<ComboMgr> ();
	}

	public void AddCombo(int num) {
		GameObject combo = Instantiate (comboPrefab);

		SpriteRenderer[] childSprites = combo.GetComponentsInChildren<SpriteRenderer> ();
		childSprites [2].sprite = numSprite [num % 10];
		childSprites [1].sprite = numSprite [(int)Mathf.Floor(num / 10)];
	}
}
