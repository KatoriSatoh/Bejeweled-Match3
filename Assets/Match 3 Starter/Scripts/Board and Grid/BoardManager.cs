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

public class BoardManager : MonoBehaviour {
	public static BoardManager instance;
	public List<Sprite> characters = new List<Sprite> ();
	public Sprite explodeSprite = new Sprite();
	public Sprite specialSprite = new Sprite();
	public GameObject tile;
	public int xSize, ySize;

    public GameConfigs GameDefine;

	public GameObject[,] tiles;

	private List<GameObject> frenzyTiles = new List<GameObject> ();
    private GameObject[] hintTiles;

    private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    private float remainingTime, frenzyTimeCurrent, hintTimeCurrent, frenzyDisplay, frenzyCurrent;

    public bool IsAnimating { get; set; }
	public bool IsShifting { get; set; }
	public bool IsFrenzy { get; set; }

	void Start () {
        instance = GetComponent<BoardManager>();

        remainingTime = GameDefine.gameTime;
        frenzyDisplay = frenzyCurrent = frenzyTimeCurrent = hintTimeCurrent = 0.0f;

        IsFrenzy = false;
        
		float boardWidth = GameDefine.tileSize * (xSize - 1);
		float boardHeight = GameDefine.tileSize * (ySize - 1);
		gameObject.transform.position = new Vector3 (-boardWidth / 2 + GameDefine.boardOffsetX, -boardHeight / 2 + GameDefine.boardOffsetY, 0);

        CreateBoard(GameDefine.tileSize);
    }

    void Update()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0)
        {
            remainingTime = 0;
        }

        if (hintTimeCurrent == GameDefine.hintTime && !IsFrenzy)
        {
            if ((Time.timeSinceLevelLoad * 1000) % 500 < 250)
            {
                foreach (GameObject hintTile in hintTiles)
                {
                    hintTile.GetComponent<Tile>().SetHint();
                }
            }
            else
            {
                foreach (GameObject hintTile in hintTiles)
                {
                    hintTile.GetComponent<Tile>().RemoveHint();
                }
            }
        }

        if (hintTimeCurrent < GameDefine.hintTime && !IsFrenzy)
        {
            hintTimeCurrent += Time.deltaTime;
            if (hintTimeCurrent >= GameDefine.hintTime)
            {
                hintTimeCurrent = GameDefine.hintTime;
                hintTiles = CheckHintAll();
            }
        }

        if (IsFrenzy && !IsShifting && !IsAnimating)
        {
            frenzyTimeCurrent += Time.deltaTime;
            if (frenzyTimeCurrent >= 1)
            {
                frenzyTimeCurrent -= 1;

                ReturnFrenzyTilesToNormal();
                SpawnFrenzyTiles();
            }

            frenzyCurrent -= GameDefine.frenzyMax / GameDefine.frenzyTime * Time.deltaTime;
            if (frenzyCurrent <= 0)
            {
                frenzyCurrent = 0;
                IsFrenzy = false;

                ReturnFrenzyTilesToNormal();
            }
        }

        IsAnimating = CheckAnimating();

        UpdateFrenzyBar();
        GUIManager.instance.Time = (int)Mathf.Ceil(remainingTime);
    }

	private void CreateBoard (float offset) {
		tiles = new GameObject[xSize, ySize];

        float startX = transform.position.x;
		float startY = transform.position.y;

		Sprite[] previousLeft = new Sprite[ySize];
		Sprite previousBelow = null;

		for (int x = 0; x < xSize; x++) {
			for (int y = 0; y < ySize; y++) {
				GameObject newTile = Instantiate(tile, new Vector3(startX + (offset * x), startY + (offset * y), 0), tile.transform.rotation);
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
        return new Vector3(transform.position.x + GameDefine.tileSize * x, transform.position.y + GameDefine.tileSize * y, 0);
    }

    private bool CheckAnimating()
    {
        foreach (GameObject tile in tiles)
        {
            if (tile.GetComponent<Tile>().isShifting)
            {
                return true;
            }
        }
        return false;
    }

    public IEnumerator ResetFrenzySpawn()
    {
        frenzyTimeCurrent = 0;

        yield return new WaitUntil(() => !IsShifting && !IsAnimating);

        ReturnFrenzyTilesToNormal();
        SpawnFrenzyTiles();
    }

    public IEnumerator FindNullTiles(bool flag = false) {
        yield return new WaitUntil(() => !IsShifting);

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

		if (flag) {
			StopCoroutine (ResetFrenzySpawn ());
			StartCoroutine (ResetFrenzySpawn ());
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
            if (frenzyCurrent >= GameDefine.frenzyMax)
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

		GUIManager.instance.Frenzy = frenzyDisplay / GameDefine.frenzyMax;
	}

	private void StartFrenzyMode() {
		frenzyCurrent = GameDefine.frenzyMax;
		frenzyTimeCurrent = 0.0f;
        hintTimeCurrent = 0;

        IsFrenzy = true;
		SpawnFrenzyTiles ();
	}

	private void SpawnFrenzyTiles() {
		frenzyTiles.Clear ();

		while (frenzyTiles.Count < GameDefine.frenzyTilesSpawn) {
			int x = Random.Range (0, xSize - 1);
			int y = Random.Range (0, ySize - 1);

			if (!frenzyTiles.Contains (tiles [x, y])) {
				frenzyTiles.Add (tiles [x, y]);
			}
		}

		foreach (GameObject tile in frenzyTiles) {
			tile.GetComponent<Tile> ().SetFrenzy ();
		}
	}

	private void ReturnFrenzyTilesToNormal() {
		for (int i = 0; i < frenzyTiles.Count; i++) {
			frenzyTiles [i].GetComponent<Tile> ().SetNormal ();
		}
	}

    private GameObject[] CheckHintOnDirection(GameObject myTile, Vector2 dir)
    {
        int xIndex = myTile.GetComponent<Tile>().xIndex;
        int yIndex = myTile.GetComponent<Tile>().yIndex;
        Sprite tileSprite = myTile.GetComponent<SpriteRenderer>().sprite;

        if (xIndex + (int)dir.x < 0 || yIndex + (int)dir.y < 0 || xIndex + (int)dir.x > xSize - 1 || yIndex + (int)dir.y > ySize - 1) return null;

        GameObject go1, go2;
        if (dir == Vector2.up || dir == Vector2.down)
        {
            // Straight check
            if (yIndex + (int)dir.y * 3 >= 0 && yIndex + (int)dir.y * 3 < ySize)
            {
                go1 = tiles[xIndex, yIndex + (int)dir.y * 2];
                go2 = tiles[xIndex, yIndex + (int)dir.y * 3];

                if (tileSprite == go1.GetComponent<SpriteRenderer>().sprite && tileSprite == go2.GetComponent<SpriteRenderer>().sprite)
                {
                    return new GameObject[] { myTile, go1, go2 };
                }
            }
            // Left side check
            if (xIndex - 2 >= 0)
            {
                go1 = tiles[xIndex - 1, yIndex + (int)dir.y];
                go2 = tiles[xIndex - 2, yIndex + (int)dir.y];

                if (tileSprite == go1.GetComponent<SpriteRenderer>().sprite && tileSprite == go2.GetComponent<SpriteRenderer>().sprite)
                {
                    return new GameObject[] { myTile, go1, go2 };
                }
            }
            // Right side check
            if (xIndex + 2 < xSize)
            {
                go1 = tiles[xIndex + 1, yIndex + (int)dir.y];
                go2 = tiles[xIndex + 2, yIndex + (int)dir.y];

                if (tileSprite == go1.GetComponent<SpriteRenderer>().sprite && tileSprite == go2.GetComponent<SpriteRenderer>().sprite)
                {
                    return new GameObject[] { myTile, go1, go2 };
                }
            }
            // Both side check
            if (xIndex - 1 >= 0 && xIndex + 1 < xSize)
            {
                go1 = tiles[xIndex + 1, yIndex + (int)dir.y];
                go2 = tiles[xIndex - 1, yIndex + (int)dir.y];

                if (tileSprite == go1.GetComponent<SpriteRenderer>().sprite && tileSprite == go2.GetComponent<SpriteRenderer>().sprite)
                {
                    return new GameObject[] { myTile, go1, go2 };
                }
            }
        }
        else
        {
            // Straight check
            if (xIndex + (int)dir.x * 3 >= 0 && xIndex + (int)dir.x * 3 < xSize)
            {
                go1 = tiles[xIndex + (int)dir.x * 2, yIndex];
                go2 = tiles[xIndex + (int)dir.x * 3, yIndex];

                if (tileSprite == go1.GetComponent<SpriteRenderer>().sprite && tileSprite == go2.GetComponent<SpriteRenderer>().sprite)
                {
                    return new GameObject[] { myTile, go1, go2 };
                }
            }
            // Bottom side check
            if (yIndex - 2 >= 0)
            {
                go1 = tiles[xIndex + (int)dir.x, yIndex - 1];
                go2 = tiles[xIndex + (int)dir.x, yIndex - 2];

                if (tileSprite == go1.GetComponent<SpriteRenderer>().sprite && tileSprite == go2.GetComponent<SpriteRenderer>().sprite)
                {
                    return new GameObject[] { myTile, go1, go2 };
                }
            }
            // Top side check
            if (yIndex + 2 < ySize)
            {
                go1 = tiles[xIndex + (int)dir.x, yIndex + 1];
                go2 = tiles[xIndex + (int)dir.x, yIndex + 2];

                if (tileSprite == go1.GetComponent<SpriteRenderer>().sprite && tileSprite == go2.GetComponent<SpriteRenderer>().sprite)
                {
                    return new GameObject[] { myTile, go1, go2 };
                }
            }
            // Both side check
            if (yIndex - 1 >= 0 && yIndex + 1 < ySize)
            {
                go1 = tiles[xIndex + (int)dir.x, yIndex + 1];
                go2 = tiles[xIndex + (int)dir.y, yIndex - 1];

                if (tileSprite == go1.GetComponent<SpriteRenderer>().sprite && tileSprite == go2.GetComponent<SpriteRenderer>().sprite)
                {
                    return new GameObject[] { myTile, go1, go2 };
                }
            }
        }

        return null;
    }

    private GameObject[] CheckHintAll()
    {
        foreach (GameObject tile in tiles)
        {
            foreach (Vector2 dir in adjacentDirections)
            {
                GameObject[] result = CheckHintOnDirection(tile, dir);
                if (result != null) return result;
            }
        }

        return null;
    }

    public void ResetHint()
    {
        hintTimeCurrent = 0;
        foreach (GameObject hint in hintTiles)
        {
            hint.GetComponent<Tile>().RemoveHint();
        }
    }
}
