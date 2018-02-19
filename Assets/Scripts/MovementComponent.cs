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
	private float moveForceModifier = 1.0f;
	private float turningRate = 5.0f;
	private bool useRandomTarget = false;
	private float randomTargetRange = 0;
	private float targetUpdateTime = 0;
	private GameObject targetGO;
	private Rigidbody2D targetRB;
	private PathfindingVisualization pfVis;

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
	public void SetMoveForceModifier(float increaseAmount){

		this.moveForceModifier = 1.0f + increaseAmount;
		this.moveForceModifier = Mathf.Clamp(this.moveForceModifier, Constants.MOVEMENT_FORCE_MODIFIER_MIN, Constants.MOVEMENT_FORCE_MODIFIER_MAX);
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
	// ------------------------------------------------------------------------
	public void setPathfindingVis(PathfindingVisualization pfVis){
		this.pfVis = pfVis;
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
				this.movementRigidbody.AddRelativeForce(this.moveForceModifier * this.moveForceVector);
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
	// ------------------------------------------------------------------------
	public List<Vector2> getPathList(){
		return this.pathList;
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

		// request pf vis to update pf debug (depends if pf vis is tracking this cat)
		this.pfVis.updatePathfinding(this.gameObject);
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
}