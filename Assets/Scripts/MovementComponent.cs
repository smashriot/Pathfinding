// -------------------------------------------------------------------------------------------------
//  MovementComponent.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;

public class MovementComponent : MonoBehaviour {

	private Rigidbody2D movementRigidbody;
	private Transform movementTransform;
	private bool performPathfinding = false;
	private PathFinding pathFinding;
	private float pathfindingUpdateInterval = 5.0f;
	private float lastPathFindingUpdate = 5.0f;     // so it will tick first update
	private int maxPathfindingChecks = 50;
	private List<Vector2> pathList = new List<Vector2>();
	private float targetFacingAngle = 0.0f;
	private Vector2 currentTargetPosition = new Vector2(0,0);
	private Vector3 moveForceVector = new Vector3(0,0,0);
	private float turningRate = 5.0f;
	private bool useRandomTarget = false;
	private float randomTargetRange = 0;
	private float targetUpdateTime = 0;
	private GameObject targetGO;
	private Rigidbody2D targetRB;

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public static MovementComponent Create(GameObject gameObject){

		MovementComponent movementComponent = gameObject.AddComponent<MovementComponent>();

		return movementComponent;
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void init(Vector2 newMoveForceVector, float newTurningRate, float newPathfindingUpdateInterval, int newMaxPathfindingChecks){
		this.movementRigidbody = GetComponentInParent<Rigidbody2D>();
		this.movementTransform = GetComponentInParent<Transform>();
		this.moveForceVector = newMoveForceVector;
		this.turningRate = newTurningRate;
		this.pathfindingUpdateInterval = newPathfindingUpdateInterval;
		this.lastPathFindingUpdate = this.pathfindingUpdateInterval;
		this.maxPathfindingChecks = newMaxPathfindingChecks;
		if (this.maxPathfindingChecks > Constants.PATHFINDING_CONFIG_FINAL_MAX_CHECKS){ this.maxPathfindingChecks = Constants.PATHFINDING_CONFIG_FINAL_MAX_CHECKS; }
		targetUpdateTime = UnityEngine.Random.Range(0, Constants.PATHFINDING_LOCATE_TARGET_INTERVAL);
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void FixedUpdate(){

		this.targetUpdateTime += Time.fixedDeltaTime;
		this.locateTarget();
	}

	// ------------------------------------------------------------------------
	// does a spherecast on target layer and sets reference
	// ------------------------------------------------------------------------
	private void locateTarget(){

		// if there is a GO, make sure it's still active. if not, reset so it will update next
		// this will ensure when a target is destroyed an enemy will immediately look for a new target
		if (this.targetGO != null && this.targetRB != null && !this.targetGO.activeSelf){
			this.targetGO = null;          // remove target reference
			this.targetRB = null;
			this.targetUpdateTime = Constants.PATHFINDING_LOCATE_TARGET_INTERVAL; // so it ticks now
		}

		// if it's been over a X Seconds, then get a new one.
		if (this.targetUpdateTime >= Constants.PATHFINDING_LOCATE_TARGET_INTERVAL){
			this.targetUpdateTime = 0.0f;

			// public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, int layerMask = DefaultRaycastLayers, float minDepth = -Mathf.Infinity, float maxDepth = Mathf.Infinity);
			Collider2D[] hitColliders = Physics2D.OverlapCircleAll(this.movementTransform.position, Constants.PATHFINDING_LOCATE_TARGET_DIST, (1 << Constants.COLLISION_LAYER_OBJECTS));

			// something was found in the sphere
			if (hitColliders.Length > 0){

				float closestColliderDistance = 1000f;
				Collider2D closestCollider = null;

				// for each hit
				for (int i=0; i < hitColliders.Length; i++){

					// make sure what it found is active
					if (hitColliders[i].transform.gameObject.activeSelf){

						if (hitColliders[i].transform.gameObject.CompareTag(Constants.GAMEOBJECT_TAG_FISH)){

							// find distance from enemy to whatever it just found
							float distance = Vector3.Distance(this.movementTransform.position, hitColliders[i].transform.position);

							// find closest collider
							if (distance < closestColliderDistance){
								closestCollider = hitColliders[i];
								closestColliderDistance = distance;
							}
						}
					}
				   }

				   // found a hit collider?
				   if (closestCollider != null){
					   this.targetGO = closestCollider.transform.gameObject;
					   this.targetRB = this.targetGO.GetComponent<Rigidbody2D>();

					Vector2 newPos = this.targetRB.position;
					this.setTargetPosition(newPos.x, newPos.y);
				   }
			}
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void enablePathfinding(PathFinding pathFindingParent){
		 this.performPathfinding = true;
		 this.pathFinding = pathFindingParent;
	}

	// ------------------------------------------------------------------------
	// negate targetY since path map is y flipped
	// ------------------------------------------------------------------------
	public void setTargetPosition(float targetX, float targetY){
		 this.currentTargetPosition.x = targetX;
		this.currentTargetPosition.y = targetY;
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void setRandomTargetPosition(float randomRange){

		 // moves between half of max and max
		 float randomX = UnityEngine.Random.Range(randomRange * 0.5f, randomRange);
		float randomY = UnityEngine.Random.Range(randomRange * 0.5f, randomRange);

		// sign flip
		if (UnityEngine.Random.Range(0,10) % 2 == 0){ randomX = -randomX; }
		if (UnityEngine.Random.Range(0,10) % 2 == 0){ randomY = -randomY; }

		// final pos
		this.currentTargetPosition.x = this.movementTransform.position.x + randomX;
		this.currentTargetPosition.y = this.movementTransform.position.y + randomY;
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void enableRandomTarget(float newRandomRange){

		this.useRandomTarget = true;
		this.randomTargetRange = newRandomRange;
	}

	// ------------------------------------------------------------------------
	// call in fixedupdate only.  face/move enemy
	// ------------------------------------------------------------------------
	public void moveOnPath(float dt){

		// is pathfinding enabled?
		if (this.performPathfinding){

			// refresh to a new target?
			this.refreshPathfinding(dt);

			if (this.pathList.Count >= 1){

				// face new target and move
				this.movementTransform.eulerAngles = new Vector3(0,0, targetFacingAngle);
				this.movementRigidbody.AddRelativeForce(this.moveForceVector);
			 }
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void retargetPathfinding(){

		lastPathFindingUpdate = this.pathfindingUpdateInterval;
	}

	// ------------------------------------------------------------------------
	// determine best path to target every X seconds
	// ------------------------------------------------------------------------
	private void refreshPathfinding(float dt){

		// time to generate a new path?
		lastPathFindingUpdate += dt;
		if (lastPathFindingUpdate >= this.pathfindingUpdateInterval){
			lastPathFindingUpdate -= this.pathfindingUpdateInterval;

			// using a random target?
			if (this.useRandomTarget){
				this.setRandomTargetPosition(this.randomTargetRange);
			}

			// find path from enemy to target using max node checks.  flip y since tiles are flipped
			bool pathFound = pathFinding.determinePath(pathFinding.convertPointsToNode(new Vector2(this.movementTransform.position.x, -this.movementTransform.position.y)),
													   pathFinding.convertPointsToNode(new Vector2(currentTargetPosition.x, -currentTargetPosition.y)),
													   this.maxPathfindingChecks);

			// save the path if found
			if (pathFound){
				this.savePath();
			}
		}

		// after generating(or not), update facing based on new position
		this.facePath(this.currentTargetPosition);
	}

	// ------------------------------------------------------------------------
	// takes path from pathfinding and saves it locally.
	// ------------------------------------------------------------------------
	private void savePath(){

		// path list is a LIFO
		if (this.pathFinding.pathList.Count > 0){

			// clear old path
			this.pathList.Clear();

			// add new
			for (int i=0; i<this.pathFinding.pathList.Count; i++){
				PathFindingNode pathNode = (PathFindingNode)pathFinding.pathList[i];
				// add the target as the pixel position
				//Vector2 pathPos = new Vector2(pathNode.nodePosition.x + 0.5f, -pathNode.nodePosition.y - 0.5f); // MAYBE +0.5 here?????
				Vector2 pathPos = new Vector2(Constants.TILEMAP_TILE_PX_WIDTH * pathNode.nodePosition.x + Constants.TILEMAP_TILE_PX_WIDTH * 0.5f,
											 -Constants.TILEMAP_TILE_PX_HEIGHT * pathNode.nodePosition.y - Constants.TILEMAP_TILE_PX_HEIGHT * 0.5f);
				this.pathList.Add(pathPos);
			}
		}

		// debug pathfinding
		if (this.pathfindingDebug){
			if (this.rootPFDebugGO != null){
				this.updatePathDebug();
			}
			else {
				this.pathfindingDebug = false; // turn off
			}
		}
	}

	// ------------------------------------------------------------------------
	// faces the enemy towards the next node in pathfinding
	// ------------------------------------------------------------------------
	private void facePath(Vector2 targetPosition){

		// pathList is a LIFO. override target position if there is a pathfinding node
		if (this.pathList.Count >= 1){
			targetPosition = this.pathList[this.pathList.Count-1];

			// if we are near enough to this node remove it.
			if (pathFinding.areVectorsEqualIntTolerance(this.movementTransform.position, targetPosition, 10)){

				pathList.RemoveAt(this.pathList.Count-1);
			}
		}

		// calc diff vector
		float pathNodePosX = targetPosition.x - this.movementTransform.position.x;
		float pathNodePosY = targetPosition.y - this.movementTransform.position.y;

		// face, then thrust relative: atan2 is Y then X?
		float targetAngle = Mathf.Atan2(pathNodePosY, pathNodePosX) * Mathf.Rad2Deg; // new way ship is pointing/thrusting.  slowly rotate to it

		 // smooth the turn to the target angle
		 this.targetFacingAngle = Mathf.MoveTowardsAngle(this.targetFacingAngle, targetAngle, this.turningRate); // rate is the last parm
	 }

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public float getFacingAngle(){

		return this.movementTransform.eulerAngles.z;
	}


	// ************************************************************************
	// PATHFINDING DEBUG FUNCTIONS BELOW
	// NOTE: This debug section is wasteful/slow since each cat creates its own
	// debug tiles. In a game, this block of code should be deleted/unused, and
	// is simply included for clarity of showing the algorithm.
	// ************************************************************************

	private GameObject rootPFDebugGO;
	private bool pathfindingDebug = false;         // set via enableTargetDebug
	private const int PF_PATH_DEBUG_SIZE = Constants.PATHFINDING_CONFIG_FINAL_MAX_CHECKS;
	private int PF_BORDER_COST = 0;
	private GameObject debugTilePrefab;
	private List<GameObject> debugTileList = new List<GameObject>();

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void Awake(){
		this.PF_BORDER_COST = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_BORDER_COST, Constants.PATHFINDING_CONFIG_DEFAULT_BORDER_COST);
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void enableDebug(PathFinding pathFindingParent){
		 this.pathFinding = pathFindingParent;
		this.pathfindingDebug = true;

		// create new root for tiles/objects
		Object.DestroyImmediate(GameObject.Find(Constants.TILEMAP_DEBUG_GO));
		this.rootPFDebugGO = new GameObject(Constants.TILEMAP_DEBUG_GO);

		// prefab
		this.debugTilePrefab = (GameObject)Resources.Load("Prefabs/DebugTile");

		for (int i=0; i<PF_PATH_DEBUG_SIZE; i++){
			GameObject tileInstance = (GameObject)Instantiate(debugTilePrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
			tileInstance.transform.SetParent(this.rootPFDebugGO.transform); // parent to holder
			SpriteRenderer spriteRenderer =    tileInstance.GetComponent<SpriteRenderer>();
			spriteRenderer.color = new Color(1.0f, 0.0f, 0.0f, 0.25f);
			tileInstance.SetActive(false);
			debugTileList.Add(tileInstance);
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void clearPathDebug(){

		// disable path squares
		 for (int i=0; i<this.debugTileList.Count; i++){
			GameObject tileInstance = this.debugTileList[i];
			tileInstance.SetActive(false);
		}
	}

	// ------------------------------------------------------------------------
	// updates sprite positions etc and sets vis
	// ------------------------------------------------------------------------
	private void updatePathDebug(){

		// turn all off
		this.clearPathDebug();

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
				this.visualizeNode(pathNode, i+this.pathList.Count, false, true);
			}
		}

		// visualize the closed list
		if (this.pathFinding.closedList.Count > 0){
			for (int i=0; i<this.pathFinding.closedList.Count; i++){
				PathFindingNode pathNode = this.pathFinding.closedList[i];
				// need to pass this.pathFinding.openList.Count so it takes sprites/labels from the pool after open
				this.visualizeNode(pathNode, i+this.pathList.Count+this.pathFinding.openList.Count, false, false);
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
					if (openList){ textMesh.text = "OPEN  S:" + pathNode.getNodeScore() + "\n\n\n\nA:" + pathNode.nodeScoreAbs + "   E:" + pathNode.nodeScoreEst; }
					else { textMesh.text = "CLOSE S:" + pathNode.getNodeScore() + "\n\n\n\nA:" + pathNode.nodeScoreAbs + "   E:" + pathNode.nodeScoreEst; }
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