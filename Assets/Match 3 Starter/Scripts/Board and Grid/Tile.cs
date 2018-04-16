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

public class Tile : MonoBehaviour {
    private static Color selectedColor = new Color(.5f, .5f, .5f, 1.0f);
    private static Color hintColor = new Color(1.0f, 0.92f, 0.016f, 1.0f);
    private static Tile previousSelected = null;
    private static Tile swappingTile = null;

	public int tileType { get; set; }
    public int xIndex { get; set; }
    public int yIndex { get; set; }

    private SpriteRenderer render;
	private int previousType;

    private bool isSelected = false;
    private bool matchFound = false;
    private bool failedSwap = false;

    public bool isShifting { get; set; }
	public bool isAnimating { get; set; }
	public bool isNull { get; set; }

	private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

	void Awake() {
		render = GetComponent<SpriteRenderer>();

		tileType = 0;

        isShifting = false;
		isAnimating = false;
		isNull = true;
    }

    void Update()
    {
        if (isShifting)
        {
            float step = 10 * Time.deltaTime;
            Vector3 target = BoardManager.instance.CalculatePosition(xIndex, yIndex);

            transform.position = Vector3.MoveTowards(transform.position, target, step);
            if (transform.position == target)
            {
                isShifting = false;
                ClearAllMatches();
            }
        }
    }

	private void Select() {
		isSelected = true;
		render.color = selectedColor;
		previousSelected = gameObject.GetComponent<Tile>();
		SFXManager.instance.PlaySFX(Clip.Select);
	}

	private void Deselect() {
		isSelected = false;
		render.color = Color.white;
		previousSelected = null;
	}

	public void SetType(int type)
	{
		tileType = type;
		GetComponent<Animator> ().SetInteger ("Type", type);
		GetComponent<Animator> ().SetBool ("Exploding", false);
		isNull = false;
	}

    public void SetHint()
    {
        if (isSelected) return;

        render.color = hintColor;
    }

    public void RemoveHint()
    {
        if (isSelected) return;

        render.color = Color.white;
    }

	private void AnimateOn()
	{
		isAnimating = true;
	}

	private void AnimateOff()
	{
		isAnimating = false;
		isNull = true;
	}

	private bool IsSpecialTile() {
		return tileType == 4 || tileType == 5;
	}

	void OnMouseDown() {
		if (BoardManager.instance.IsShifting || BoardManager.instance.IsAnimating) {
			return;
		}

		if (isSelected) {
			Deselect ();
		} else {
			if (previousSelected == null) {
				if (IsSpecialTile()) {
					TriggerSpecialTile();
					return;
				}

				Select ();
			} else {
				if (GetAllAdjacentTiles ().Contains (previousSelected.gameObject)) {
					if (IsSpecialTile ()) {
						previousSelected.Deselect ();
						TriggerSpecialTile();
						return;
					}
                    
                    StopCoroutine(SwapSprite());
                    StartCoroutine(SwapSprite());
				} else {
					previousSelected.Deselect ();

					if (IsSpecialTile()) {
						TriggerSpecialTile();
						return;
					}

					Select ();
				}
			}
		}
	}

    public void MoveTo(int x, int y)
    {
        xIndex = x;
        yIndex = y;

        transform.position = BoardManager.instance.CalculatePosition(x, y);
    }

    public void ShiftTo(int x, int y)
    {
        xIndex = x;
        yIndex = y;
        isShifting = true;

        BoardManager.instance.tiles[x, y] = gameObject;
    }

	public IEnumerator SwapSprite() {
        BoardManager.instance.ResetHint();

        int xTemp = previousSelected.xIndex;
        int yTemp = previousSelected.yIndex;

        swappingTile = previousSelected;
        previousSelected.ShiftTo(xIndex, yIndex);
        previousSelected.Deselect();

        ShiftTo(xTemp, yTemp);
 
        SFXManager.instance.PlaySFX(Clip.Swap);

        yield return new WaitUntil(() => !isShifting && !swappingTile.isShifting);

        if (failedSwap && swappingTile.failedSwap)
        {
            xTemp = swappingTile.xIndex;
            yTemp = swappingTile.yIndex;

            swappingTile.ShiftTo(xIndex, yIndex);
            swappingTile = null;

            ShiftTo(xTemp, yTemp);

            SFXManager.instance.PlaySFX(Clip.Swap);
        }
        else
        {
            swappingTile.failedSwap = false;
            swappingTile = null;
            failedSwap = false;
        }
	}

	private GameObject GetAdjacent(Vector2 castDir) {
		int x = xIndex + (int)castDir.x;
		int y = yIndex + (int)castDir.y;

		if (x > -1 && x < BoardManager.instance.xSize && y > -1 && y < BoardManager.instance.ySize) {
			if (BoardManager.instance.tiles [x, y].GetComponent<Tile> ().isAnimating)
				return null;
			return BoardManager.instance.tiles [x, y];
		} else {
			return null;
		}
	}

	private List<GameObject> GetAllAdjacentTiles() {
		List<GameObject> adjacentTiles = new List<GameObject> ();
		for (int i = 0; i < adjacentDirections.Length; i++) {
			adjacentTiles.Add (GetAdjacent (adjacentDirections [i]));
		}

		return adjacentTiles;
	}

	private List<GameObject> FindMatch(Vector2 castDir) {
		List<GameObject> matchingTiles = new List<GameObject> ();
		GameObject adjacentTile = GetAdjacent (castDir);

		while (adjacentTile != null && !IsSpecialTile() && adjacentTile.GetComponent<Tile>().tileType == tileType) {
			matchingTiles.Add (adjacentTile);
			adjacentTile = adjacentTile.GetComponent<Tile>().GetAdjacent (castDir);
		}

		return matchingTiles;
	}

	private int ClearMatch(Vector2[] paths) {
		List<GameObject> matchingTiles = new List<GameObject> ();
		for (int i = 0; i < paths.Length; i++) {
			matchingTiles.AddRange (FindMatch (paths [i]));
		}
		if (matchingTiles.Count >= 2) {
			for (int i = 0; i < matchingTiles.Count; i++) {
				if (matchingTiles [i].GetComponent<Tile> ().isAnimating || matchingTiles [i].GetComponent<Tile> ().isNull)
					continue;
				matchingTiles[i].GetComponent<Animator>().SetBool("Exploding", true);
				matchingTiles [i].GetComponent<Tile> ().AnimateOn ();
			}
			matchFound = true;
			return matchingTiles.Count;
		}

		return 0;
	}

	public void ClearAllMatches() {
		if (isNull)
			return;

		int h = ClearMatch (new Vector2[2] { Vector2.left, Vector2.right });
		int v = ClearMatch (new Vector2[2] { Vector2.up, Vector2.down });

		if (matchFound) {
			if (h + v < 3) {
				if (!isAnimating && !isNull) {
					GetComponent<Animator> ().SetBool ("Exploding", true);
					AnimateOn ();
				}
			} else {
//				if (h + v > 3) {
//					ComboMgr.instance.AddCombo (h + v + 1);
//					GUIManager.instance.Score += h + v + 1;
//				}

				SetType (4);
				GUIManager.instance.Score += 10;
			}
			matchFound = false;

			BoardManager.instance.IsAnimating = true;
			StopCoroutine (BoardManager.instance.FindNullTiles ());
			StartCoroutine (BoardManager.instance.FindNullTiles ());

			SFXManager.instance.PlaySFX (Clip.Clear);
		}
        else if (swappingTile != null)
        {
            failedSwap = true;
        }
	}

	private void ExplodeTilesAround() {
		List<GameObject> aroundTiles = new List<GameObject> ();
		for (int i = 0; i < adjacentDirections.Length; i++) {
			GameObject adjacentTile = GetAdjacent (adjacentDirections [i]);
			if (adjacentTile != null) {
				aroundTiles.Add (adjacentTile);

				if (adjacentDirections [i] == Vector2.up || adjacentDirections [i] == Vector2.down) {
					GameObject leftTile = adjacentTile.GetComponent<Tile>().GetAdjacent (Vector2.left);
					GameObject rightTile = adjacentTile.GetComponent<Tile>().GetAdjacent (Vector2.right);

					if (leftTile != null)
						aroundTiles.Add (leftTile);
					if (rightTile != null)
						aroundTiles.Add (rightTile);
				}
			}
		}
		for (int i = 0; i < aroundTiles.Count; i++) {
			if (aroundTiles [i].GetComponent<Tile> ().isAnimating || aroundTiles [i].GetComponent<Tile> ().isNull)
				continue;

			if (aroundTiles [i].GetComponent<Tile> ().IsSpecialTile ()) {
				aroundTiles [i].GetComponent<Tile> ().TriggerSpecialTile ();
			} else {
				aroundTiles [i].GetComponent<Animator> ().SetBool ("Exploding", true);
				aroundTiles [i].GetComponent<Tile> ().AnimateOn ();
			}
		}
	}

	private void ExplodeCrossLine() {
		List<GameObject> crossTiles = new List<GameObject> ();
		for (int i = 0; i < adjacentDirections.Length; i++) {
			GameObject adjacentTile = GetAdjacent (adjacentDirections [i]);
			while (adjacentTile != null) {
				crossTiles.Add (adjacentTile);
				adjacentTile = adjacentTile.GetComponent<Tile>().GetAdjacent (adjacentDirections [i]);
			}
		}
		for (int i = 0; i < crossTiles.Count; i++) {
			if (crossTiles [i].GetComponent<Tile> ().isAnimating || crossTiles [i].GetComponent<Tile> ().isNull)
				continue;
			
			if (crossTiles [i].GetComponent<Tile> ().IsSpecialTile ()) {
				crossTiles [i].GetComponent<Tile> ().TriggerSpecialTile ();
			} else {
				crossTiles [i].GetComponent<Animator> ().SetBool ("Exploding", true);
				crossTiles [i].GetComponent<Tile> ().AnimateOn ();
			}
		}
	}

	private void TriggerSpecialTile() {
		if (isNull)
			return;

        BoardManager.instance.ResetHint();

        bool isSpecialTriggered = false;

		if (tileType == 4) {
			if (!isAnimating && !isNull) {
				GetComponent<Animator> ().SetBool ("Exploding", true);
				AnimateOn ();
			}

			ExplodeTilesAround ();
		} else {
			if (!isAnimating && !isNull) {
				GetComponent<Animator> ().SetBool ("Exploding", true);
				AnimateOn ();
			}

			ExplodeCrossLine ();

			isSpecialTriggered = true;
		}

		BoardManager.instance.IsAnimating = true;
		StopCoroutine (BoardManager.instance.FindNullTiles (isSpecialTriggered));
		StartCoroutine (BoardManager.instance.FindNullTiles (isSpecialTriggered));

		SFXManager.instance.PlaySFX (Clip.Clear);
	}

	public void SetFrenzy() {
		previousType = tileType;
		SetType (5);
	}

	public void SetNormal() {
		if (tileType == 5)
			SetType (previousType);
	}
}