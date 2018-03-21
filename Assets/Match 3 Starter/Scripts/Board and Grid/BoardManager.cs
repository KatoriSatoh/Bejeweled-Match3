/*
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
	public List<Sprite> characters = new List<Sprite> ();
	public Sprite explodeSprite = new Sprite();
	public Sprite specialSprite = new Sprite();
	public GameObject tile;
	public int xSize, ySize;

	public Slider frenzyBar;

	public GameObject[,] tiles;

	private List<GameObject> frenzyTiles = new List<GameObject> ();
	private List<Sprite> frenzySprites = new List<Sprite> ();

	private float remainingTime, frenzyTimeCurrent, frenzyTimeMax;
	private float frenzyDisplay, frenzyCurrent, frenzyMax;
	private int frenzyTileNumber = 4;

    private Vector2 tileSize;

    public bool IsAnimating { get; set; }
	public bool IsShifting { get; set; }
	public bool IsFrenzy { get; set; }

	void Awake () {
		remainingTime = 30.0f;
		frenzyTimeMax = 5.0f;
		frenzyMax = 10.0f;
		frenzyDisplay = frenzyCurrent = frenzyTimeCurrent = 0.0f;

        tileSize = tile.GetComponent<SpriteRenderer>().bounds.size;

		IsFrenzy = false;
	}

	void Start () {
		instance = GetComponent<BoardManager>();
        
		float boardWidth = tileSize.x * (xSize - 1);
		float boardHeight = tileSize.y * (ySize - 1);
		gameObject.transform.position = new Vector3 (-boardWidth / 2, -boardHeight / 2, 0);

        CreateBoard(tileSize.x, tileSize.y);
    }

	void Update () {
		remainingTime -= Time.deltaTime;
		if (remainingTime <= 0) {
			remainingTime = 0;
		}

		if (IsFrenzy) {
			frenzyCurrent -= frenzyMax / 5 * Time.deltaTime;
			if (frenzyCurrent <= 0)
				frenzyCurrent = 0;
			
			frenzyTimeCurrent += Time.deltaTime;
			if (frenzyTimeCurrent >= 1) {
				frenzyTimeCurrent -= 1;

				ReturnFrenzyTilesToNormal ();
				SpawnFrenzyTiles ();
			}

			frenzyTimeMax -= Time.deltaTime;
			if (frenzyTimeMax <= 0) {
				frenzyTimeMax = 5.0f;
				IsFrenzy = false;

				ReturnFrenzyTilesToNormal ();
			}
		}

        IsAnimating = CheckAnimating();

		UpdateFrenzyBar ();
		GUIManager.instance.Time = (int)Mathf.Ceil (remainingTime);
	}

    private bool CheckAnimating() {
        foreach (GameObject tile in tiles) {
            if (tile.GetComponent<Tile>().isShifting)
            {
                return true;
            }
        }
        return false;
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
                newTile.GetComponent<Tile>().xIndex = x;
                newTile.GetComponent<Tile>().yIndex = y;
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

    public Vector3 CalculatePosition(int x, int y)
    {
        return new Vector3(transform.position.x + tileSize.x * x, transform.position.y + tileSize.y * y, 0);
    }

	public IEnumerator FindNullTiles() {
		for (int x = 0; x < xSize; x++) {
			for (int y = 0; y < ySize; y++) {
				if (tiles [x, y].GetComponent<SpriteRenderer> ().sprite == null)
                {
                    ShiftTilesDown(x, y);
                    break;
				}
			}
		}

        yield return new WaitUntil(() => !IsShifting);

		for (int x = 0; x < xSize; x++) {
			for (int y = 0; y < ySize; y++) {
				tiles [x, y].GetComponent<Tile> ().ClearAllMatches ();
			}
		}
	}

	private void ShiftTilesDown(int x, int yStart) {
		IsShifting = true;

        int shiftIndex = 0;
        List<GameObject> nullTiles = new List<GameObject>();

		for (int y = yStart; y < ySize; y++) {
			if (tiles[x, y].GetComponent<SpriteRenderer>().sprite == null)
            {
                nullTiles.Add(tiles[x, y]);
            }
            else
            {
                tiles[x, y].GetComponent<Tile>().ShiftTo(x, yStart + shiftIndex);
                shiftIndex++;
            }
		}

        shiftIndex = 0;
        foreach (GameObject tile in nullTiles) {
            tile.GetComponent<Tile>().MoveTo(x, ySize + shiftIndex);
            tile.GetComponent<SpriteRenderer>().sprite = GetNewSprite(x, ySize + shiftIndex - nullTiles.Count);
            tile.GetComponent<Tile>().ShiftTo(x, ySize + shiftIndex - nullTiles.Count);
            shiftIndex++;
        }

        GUIManager.instance.Score += nullTiles.Count * 10;
        if (!IsFrenzy)
        {
            frenzyCurrent += nullTiles.Count * 1.0f;
            if (frenzyCurrent >= frenzyMax)
            {
                StartFrenzyMode();
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
		frenzyTimeMax = 5.0f;
		frenzyTimeCurrent = 0.0f;

		IsFrenzy = true;
		SpawnFrenzyTiles ();
	}

	private void SpawnFrenzyTiles() {
		frenzyTiles.Clear ();
		frenzySprites.Clear ();

		while (frenzyTiles.Count < frenzyTileNumber) {
			int x = Random.Range (0, xSize - 1);
			int y = Random.Range (0, ySize - 1);

			if (!frenzyTiles.Contains (tiles [x, y])) {
				frenzyTiles.Add (tiles [x, y]);
				frenzySprites.Add (tiles [x, y].GetComponent<SpriteRenderer> ().sprite);
			}
		}

		foreach (GameObject tile in frenzyTiles) {
			tile.GetComponent<SpriteRenderer> ().sprite = specialSprite;
		}
	}

	private void ReturnFrenzyTilesToNormal() {
		for (int i = 0; i < frenzyTiles.Count; i++) {
			if (frenzyTiles [i].GetComponent<SpriteRenderer> ().sprite == specialSprite)
				frenzyTiles [i].GetComponent<SpriteRenderer> ().sprite = frenzySprites [i];
		}
	}
}
