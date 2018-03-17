﻿/*
 * Copyright (c) 2017 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour {
	public static BoardManager instance;
	public List<Sprite> characters = new List<Sprite>();
	public List<Sprite> specialChars = new List<Sprite> ();
	public GameObject tile;
	public int xSize, ySize;

	public Slider frenzyBar;

	private GameObject[,] tiles;

	private float remainingTime, frenzyTimeCurrent, frenzyTimeMax;
	private float frenzyDisplay, frenzyCurrent, frenzyMax;

	public bool IsShifting { get; set; }
	public bool IsFrenzy { get; set; }

	void Awake () {
		remainingTime = 30.0f;
		frenzyTimeMax = 5.0f;
		frenzyMax = 10.0f;
		frenzyDisplay = frenzyCurrent = frenzyTimeCurrent = 0.0f;

		IsFrenzy = false;
	}

	void Start () {
		instance = GetComponent<BoardManager>();

		Vector2 offset = tile.GetComponent<SpriteRenderer>().bounds.size;
        CreateBoard(offset.x, offset.y);
    }

	void Update () {
		remainingTime -= Time.deltaTime;
		if (remainingTime <= 0) {
			remainingTime = 0;
		}

		UpdateFrenzyBar ();
		GUIManager.instance.Time = (int)Mathf.Ceil (remainingTime);
	}

	private void CreateBoard (float xOffset, float yOffset) {
		tiles = new GameObject[xSize, ySize];

        float startX = transform.position.x;
		float startY = transform.position.y;

		Sprite[] previousLeft = new Sprite[ySize];
		Sprite previousBelow = null;

		for (int x = 0; x < xSize; x++) {
			for (int y = 0; y < ySize; y++) {
				GameObject newTile = Instantiate(tile, new Vector3(startX + (xOffset * x), startY + (yOffset * y), 0), tile.transform.rotation);
				tiles[x, y] = newTile;

				newTile.transform.parent = transform;

				List<Sprite> possibleCharacters = new List<Sprite> ();
				possibleCharacters.AddRange (characters);
				possibleCharacters.Remove (previousLeft [y]);
				possibleCharacters.Remove (previousBelow);

				Sprite newSprite = possibleCharacters [Random.Range (0, possibleCharacters.Count)];
				newTile.GetComponent<SpriteRenderer> ().sprite = newSprite;

				previousLeft[y] = newSprite;
				previousBelow = newSprite;
			}
        }
    }

	public IEnumerator FindNullTiles() {
		for (int x = 0; x < xSize; x++) {
			for (int y = 0; y < ySize; y++) {
				if (tiles [x, y].GetComponent<SpriteRenderer> ().sprite == null) {
					yield return StartCoroutine (ShiftTilesDown (x, y));
					//break;
				}
			}
		}

		for (int x = 0; x < xSize; x++) {
			for (int y = 0; y < ySize; y++) {
				tiles [x, y].GetComponent<Tile> ().ClearAllMatches ();
			}
		}
	}

	private IEnumerator ShiftTilesDown(int x, int yStart, float shiftDelay = .03f) {
		IsShifting = true;
		List<SpriteRenderer> renders = new List<SpriteRenderer> ();
		int nullCount = 0;

		for (int y = yStart; y < ySize; y++) {
			SpriteRenderer render = tiles [x, y].GetComponent<SpriteRenderer> ();
			renders.Add (render);

			if (render.sprite == null) {
				nullCount++;
			} else {
				break;
			}
		}

		for (int i = 0; i < nullCount; i++) {
			GUIManager.instance.Score += 10;
			frenzyCurrent += 1.0f;

			yield return new WaitForSeconds (shiftDelay);
			if (renders.Count == 1) {
				renders [0].sprite = GetNewSprite (x, ySize - 1);
			}
			for (int k = 0; k < renders.Count - 1; k++) {
				renders [k].sprite = renders [k + 1].sprite;
				renders [k + 1].sprite = GetNewSprite(x, ySize - 1);
			}
		}

		IsShifting = false;
	}

	private Sprite GetNewSprite(int x, int y) {
		List<Sprite> possibleCharacters = new List<Sprite> ();
		possibleCharacters.AddRange (characters);

		if (x > 0) {
			possibleCharacters.Remove (tiles [x - 1, y].GetComponent<SpriteRenderer> ().sprite);
		}
		if (x < xSize - 1) {
			possibleCharacters.Remove (tiles [x + 1, y].GetComponent<SpriteRenderer> ().sprite);
		}
		if (y > 0) {
			possibleCharacters.Remove (tiles [x, y - 1].GetComponent<SpriteRenderer> ().sprite);
		}

		return possibleCharacters [Random.Range (0, possibleCharacters.Count)];
	}

	private void UpdateFrenzyBar() {
		if (frenzyDisplay < frenzyCurrent) {
			frenzyDisplay += 5 * Time.deltaTime;
			if (frenzyDisplay >= frenzyCurrent)
				frenzyDisplay = frenzyCurrent;
		}
		if (frenzyDisplay > frenzyCurrent) {
			frenzyDisplay -= 5 * Time.deltaTime;
			if (frenzyDisplay <= frenzyCurrent)
				frenzyDisplay = frenzyCurrent;
		}

		frenzyBar.value = frenzyDisplay / frenzyMax;
	}

	private void StartFrenzyMode() {
		frenzyCurrent = frenzyMax;

		IsFrenzy = true;
	}
}
