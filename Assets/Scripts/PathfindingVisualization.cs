// -------------------------------------------------------------------------------------------------
//  PathfindingVisualization.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2017/03/02
//  Copyright 2017 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;

public class PathfindingVisualization : MonoBehaviour {

	private GameObject rootPFDebugGO;
	private const int PF_PATH_DEBUG_SIZE = Constants.PATHFINDING_CONFIG_FINAL_MAX_CHECKS;
	private int PF_BORDER_COST = 0;
	private GameObject debugTilePrefab;
	private List<GameObject> debugTileList;
	private PathFinding pathFinding; // linked pathfinding

	private GameObject cat;
	private MovementComponent catMovementComponent;

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void Awake(){
		this.PF_BORDER_COST = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_BORDER_COST, Constants.PATHFINDING_CONFIG_DEFAULT_BORDER_COST);
		this.createDebugTiles();
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void attachPathfinding(PathFinding pathFindingParent){
		this.pathFinding = pathFindingParent;
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void trackCat(GameObject cat){

		this.cat = cat;
		this.catMovementComponent = cat.GetComponent<MovementComponent>();
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void createDebugTiles(){

		this.debugTileList = new List<GameObject>();
		this.rootPFDebugGO = GameObject.Find(Constants.TILEMAP_DEBUG_GO);

		// prefab
		this.debugTilePrefab = (GameObject)Resources.Load("Prefabs/DebugTile");
		for (int i=0; i<PF_PATH_DEBUG_SIZE; i++){
			GameObject tileInstance = (GameObject)Instantiate(debugTilePrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
			tileInstance.transform.SetParent(this.rootPFDebugGO.transform); // parent to holder
			SpriteRenderer spriteRenderer = tileInstance.GetComponent<SpriteRenderer>();
			spriteRenderer.color = new Color(1.0f, 0.0f, 0.0f, 0.25f);
			tileInstance.SetActive(false);
			debugTileList.Add(tileInstance);
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void clearPathDebug(){

		// disable path squares
		for (int i=0; i<this.debugTileList.Count; i++){
			GameObject tileInstance = this.debugTileList[i];
			tileInstance.SetActive(false);
		}
	}

	// ------------------------------------------------------------------------
	// updates sprite positions etc and sets vis
	// ------------------------------------------------------------------------
	public void updatePathfinding(GameObject sourceCat){

		if (sourceCat == this.cat && this.pathFinding != null){

			// turn all off
			this.clearPathDebug();

			// get the cat specific pathList
			List<Vector2> pathList = this.catMovementComponent.getPathList();

			// visualize the path list
			if (this.pathFinding.pathList.Count > 0){
				for (int i=0; i<this.pathFinding.pathList.Count; i++){
					PathFindingNode pathNode = this.pathFinding.pathList[i];
					this.visualizeNode(pathNode, i, true, true);
				}
			}

			// visualize the open list
			if (this.pathFinding.openList.Count > 0){
				for (int i=0; i<this.pathFinding.openList.Count; i++){
					PathFindingNode pathNode = this.pathFinding.openList[i];
					this.visualizeNode(pathNode, i+pathList.Count, false, true);
				}
			}

			// visualize the closed list
			if (this.pathFinding.closedList.Count > 0){
				for (int i=0; i<this.pathFinding.closedList.Count; i++){
					PathFindingNode pathNode = this.pathFinding.closedList[i];
					// need to pass this.pathFinding.openList.Count so it takes sprites/labels from the pool after open
					this.visualizeNode(pathNode, i+pathList.Count+this.pathFinding.openList.Count, false, false);
				}
			}
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void visualizeNode(PathFindingNode pathNode, int i, bool isPath, bool openList){
		//Vector2 pathPos = new Vector2(pathNode.nodePosition.x + 0.5f, -pathNode.nodePosition.y - 0.5f);
		Vector2 pathPos = new Vector2(Constants.TILEMAP_TILE_PX_WIDTH * pathNode.nodePosition.x + Constants.TILEMAP_TILE_PX_WIDTH * 0.5f,
										-Constants.TILEMAP_TILE_PX_HEIGHT * pathNode.nodePosition.y - Constants.TILEMAP_TILE_PX_HEIGHT * 0.5f);

		// get a tile prefab
		if (i < this.debugTileList.Count){
			// TILE COLOR--------
			GameObject tileInstance = this.debugTileList[i];
			// enable
			tileInstance.SetActive(true);
			// position
			tileInstance.transform.position = pathPos;
			// color/sort sprite
			SpriteRenderer spriteRenderer =    tileInstance.GetComponent<SpriteRenderer>();

			// path followed - this is an overlay. closed node will be below this sprite
			if (isPath){
				spriteRenderer.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
				spriteRenderer.sortingOrder = 10;
			}
			// border node AND border nodes enabled (cost > 0) = yellow
			else if (pathNode.borderNode && PF_BORDER_COST > 0){
				spriteRenderer.color = new Color(1.0f, 1.0f, 0.0f, 0.25f);
				spriteRenderer.sortingOrder = 5;
			}
			// open - green
			else if (openList){
				spriteRenderer.color = new Color(0.0f, 1.0f, 0.0f, 0.25f);
				spriteRenderer.sortingOrder = 5;
			}
			// closed - red
			else {
				spriteRenderer.color = new Color(1.0f, 0.0f, 0.0f, 0.25f);
				spriteRenderer.sortingOrder = 5;
			}

			// ARROW ROTATION---------
			GameObject arrowObject = tileInstance.transform.Find("arrow").gameObject;
			SpriteRenderer arrowSR = arrowObject.GetComponent<SpriteRenderer>();
			arrowSR.enabled = true;

			// turn off arrow for path overlay
			if (isPath){
				arrowSR.enabled = false;
			}
			// update arrow direction
			else {
				// direction to parent
				float diffX = 0f;
				float diffY = 0f;
				if (pathNode.nodeParent != null){
					diffX = pathNode.nodeParent.nodePosition.x - pathNode.nodePosition.x;
					diffY = pathNode.nodeParent.nodePosition.y - pathNode.nodePosition.y;
				}
				// set angle to parent
				//arrowSR.transform.rotation = Quaternion.Euler(0, 0, 90f + Mathf.Atan2(diffY, diffX) * Mathf.Rad2Deg);
				arrowSR.transform.rotation = Quaternion.Euler(0, 0, -90 + Mathf.Atan2(-diffY, diffX) * Mathf.Rad2Deg);
			}

			// LABELS. need to fix sorting on mesh renderer so it's in front of sprites
			MeshRenderer textRenderer = tileInstance.transform.Find("text").gameObject.GetComponent<MeshRenderer>();
			textRenderer.sortingLayerName = "Debug";
			textRenderer.sortingOrder = 100;
			textRenderer.enabled = true;

			 // turn off text for path overlay
			if (isPath){
				textRenderer.enabled = false;
			}
			// update text
			else {
				// update text
				TextMesh textMesh = tileInstance.transform.Find("text").gameObject.GetComponent<TextMesh>();
				// full info close
				if (pathNode.nodeNumber < 300){
					if (openList){ textMesh.text = "OPEN  S:" + pathNode.getNodeScore() + "\n\nT:" + pathNode.nodeScoreExtra + "             \n\nA:" + pathNode.nodeScoreAbs + "   E:" + pathNode.nodeScoreEst; }
					else { textMesh.text = "CLOSE S:" + pathNode.getNodeScore() + "\n\nT:" + pathNode.nodeScoreExtra + "             \n\nA:" + pathNode.nodeScoreAbs + "   E:" + pathNode.nodeScoreEst; }
				}
				// lesser info farther away
				else {
					if (openList){ textMesh.text = "OPEN  S:" + pathNode.getNodeScore() + "\n\n\n\n\n"; }
					else { textMesh.text = "CLOSE S:" + pathNode.getNodeScore() + "\n\n\n\n\n"; }
				}
			}
		}
	}
}