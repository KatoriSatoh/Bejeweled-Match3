using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (fileName = "GameConfigs")]
public class GameConfigs : ScriptableObject {
    public float gameTime;
    public float hintTime;
    public float frenzyTime;

    public float boardOffsetX;
    public float boardOffsetY;

    public float tileSize;

    public float frenzyEachItem;
    public float frenzyMax;

    public int scoreEachItem;
    public int frenzyTilesSpawn;

	public float comboOffsetY;
	public float comboScaleMin;
	public float comboScaleMax;
}
