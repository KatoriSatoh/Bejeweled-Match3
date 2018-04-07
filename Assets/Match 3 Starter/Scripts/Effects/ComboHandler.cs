using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboHandler : MonoBehaviour {
	public GameConfigs GameDefine;

	void Start() {
		transform.position = new Vector3(transform.position.x, GameDefine.comboOffsetY, transform.position.z);
		transform.localScale = new Vector3 (GameDefine.comboScaleMin, GameDefine.comboScaleMin, 1);
	}

	// Update is called once per frame
	void Update () {
		Vector3 currentScale = transform.localScale;
		currentScale += Vector3.one * Time.deltaTime;
		transform.localScale = currentScale;

		if (currentScale.x > GameDefine.comboScaleMax) {
			Destroy (gameObject);
		}
	}
}
