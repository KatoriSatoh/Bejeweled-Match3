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
    private static Tile previousSelected = null;

    public int xIndex { get; set; }
    public int yIndex { get; set; }

    private SpriteRenderer render;
    private int xTarget, yTarget;
    private bool isSelected = false;
    private bool matchFound = false;
    public bool isMoving { get; set; }

	private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

	void Awake() {
		render = GetComponent<SpriteRenderer>();

        isMoving = false;
    }

    void Update()
    {
        if (isMoving)
        {
            float step = 5 * Time.deltaTime;
            Vector2 offset = gameObject.GetComponent<SpriteRenderer>().bounds.size;
            Vector3 target = new Vector3(BoardManager.instance.gameObject.transform.position.x + (offset.x * xTarget),
                                         BoardManager.instance.gameObject.transform.position.y + (offset.y * yTarget), 0);

            transform.position = Vector3.MoveTowards(transform.position, target, step);
            if (transform.position == target)
            {
                xIndex = xTarget;
                yIndex = yTarget;

                isMoving = false;
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

	private bool IsSpecialTile() {
		return render.sprite == BoardManager.instance.explodeSprite || render.sprite == BoardManager.instance.specialSprite;
	}

	void OnMouseDown() {
		if (render.sprite == null || BoardManager.instance.IsShifting || BoardManager.instance.IsAnimating) {
			return;
		}

		if (isSelected) {
			Deselect ();
		} else {
			if (previousSelected == null) {
				if (IsSpecialTile()) {
					TriggerSpecialTile ();
					return;
				}

				Select ();
			} else {
				if (GetAllAdjacentTiles ().Contains (previousSelected.gameObject)) {
					if (IsSpecialTile ()) {
						previousSelected.Deselect ();
						TriggerSpecialTile ();
						return;
					}

					SwapSprite (previousSelected.render);
				} else {
					previousSelected.GetComponent<Tile> ().Deselect ();

					if (IsSpecialTile()) {
						TriggerSpecialTile ();
						return;
					}

					Select ();
				}
			}
		}
	}

    public void MoveTo(int x, int y)
    {
        xTarget = x;
        yTarget = y;
        isMoving = true;
    }

	public void SwapSprite(SpriteRenderer render2) {
        int xTemp = previousSelected.xIndex;
        int yTemp = previousSelected.yIndex;

        previousSelected.MoveTo(xIndex, yIndex);
        previousSelected.Deselect();

        MoveTo(xTemp, yTemp);

		SFXManager.instance.PlaySFX (Clip.Swap);
	}

	private GameObject GetAdjacent(Vector2 castDir) {
		RaycastHit2D hit = Physics2D.Raycast (transform.position, castDir);
		if (hit.collider != null) {
			return hit.collider.gameObject;
		}

		return null;
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
		RaycastHit2D hit = Physics2D.Raycast (transform.position, castDir);

		while (hit.collider != null && hit.collider.GetComponent<SpriteRenderer> ().sprite == render.sprite) {
			matchingTiles.Add (hit.collider.gameObject);
			hit = Physics2D.Raycast (hit.collider.transform.position, castDir);
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
				matchingTiles [i].GetComponent<SpriteRenderer> ().sprite = null;
			}
			matchFound = true;

			return matchingTiles.Count;
		}
		return 0;
	}

	public void ClearAllMatches() {
		if (render.sprite == null)
			return;

		int h = ClearMatch (new Vector2[2] { Vector2.left, Vector2.right });
		int v = ClearMatch (new Vector2[2] { Vector2.up, Vector2.down });

		if (matchFound) {
			if (h + v < 3) {
				render.sprite = null;
			} else {
				render.sprite = BoardManager.instance.explodeSprite;
				GUIManager.instance.Score += 10;
			}
			matchFound = false;

			StopCoroutine (BoardManager.instance.FindNullTiles ());
			StartCoroutine (BoardManager.instance.FindNullTiles ());

			SFXManager.instance.PlaySFX (Clip.Clear);
		}
	}

	private void ExplodeTilesAround() {
		List<GameObject> aroundTiles = new List<GameObject> ();
		for (int i = 0; i < adjacentDirections.Length; i++) {
			RaycastHit2D hit = Physics2D.Raycast (transform.position, adjacentDirections [i]);
			if (hit.collider != null) {
				aroundTiles.Add (hit.collider.gameObject);

				if (adjacentDirections [i] == Vector2.up || adjacentDirections [i] == Vector2.down) {
					RaycastHit2D left = Physics2D.Raycast (hit.collider.transform.position, Vector2.left);
					RaycastHit2D right = Physics2D.Raycast (hit.collider.transform.position, Vector2.right);

					if (left.collider != null)
						aroundTiles.Add (left.collider.gameObject);
					if (right.collider != null)
						aroundTiles.Add (right.collider.gameObject);
				}
			}
		}
		for (int i = 0; i < aroundTiles.Count; i++) {
			if (aroundTiles [i].GetComponent<Tile> ().IsSpecialTile ()) {
				aroundTiles [i].GetComponent<Tile> ().TriggerSpecialTile ();
			} else {
				aroundTiles [i].GetComponent<SpriteRenderer> ().sprite = null;
			}
		}
	}

	private void ExplodeCrossLine() {
		List<GameObject> crossTiles = new List<GameObject> ();
		for (int i = 0; i < adjacentDirections.Length; i++) {
			RaycastHit2D hit = Physics2D.Raycast (transform.position, adjacentDirections [i]);
			while (hit.collider != null) {
				crossTiles.Add (hit.collider.gameObject);
				hit = Physics2D.Raycast (hit.collider.transform.position, adjacentDirections [i]);
			}
		}
		for (int i = 0; i < crossTiles.Count; i++) {
			if (crossTiles [i].GetComponent<Tile> ().IsSpecialTile ()) {
				crossTiles [i].GetComponent<Tile> ().TriggerSpecialTile ();
			} else {
				crossTiles [i].GetComponent<SpriteRenderer> ().sprite = null;
			}
		}
	}

	private void TriggerSpecialTile() {
		if (render.sprite == null)
			return;

		if (render.sprite == BoardManager.instance.explodeSprite) {
			render.sprite = null;
			ExplodeTilesAround ();
		} else {
			render.sprite = null;
			ExplodeCrossLine ();
		}

		StopCoroutine (BoardManager.instance.FindNullTiles ());
		StartCoroutine (BoardManager.instance.FindNullTiles ());

		SFXManager.instance.PlaySFX (Clip.Clear);
	}
}