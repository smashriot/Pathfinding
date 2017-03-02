// -------------------------------------------------------------------------------------------------
//  Cat.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;

public class Cat : MonoBehaviour {

	private MovementComponent movementComponent;
	// set these in the prefab
	public AudioSource audioSource;
	public SpriteRenderer bodySprite;
	public SpriteRenderer shadowSprite;

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void enablePathfinding(PathFinding globalPathFinding){

		// setup pathfinding and the rigidbody reference
		this.movementComponent = MovementComponent.Create(this.gameObject);
		// init(Vector2 newPosition, Rigidbody parentRigidbody, Vector3 newMoveForceVector, float newTurningRate, float newPathfindingUpdateInterval, int newMaxPathfindingChecks){
		int pfMaxChecks = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_MAX_CHECKS, Constants.PATHFINDING_CONFIG_DEFAULT_MAX_CHECKS);
		this.movementComponent.init(Vector2.right * Constants.PHYSICS_ANIMAL_MOVE_FORCE, Constants.PHYSICS_ANIMAL_TURNING_RATE, Constants.PATHFINDING_ANIMAL_UPDATE_INTERVAL, pfMaxChecks);
		this.movementComponent.enablePathfinding(globalPathFinding); // point to main PathFinding of the TileMap
		this.movementComponent.enableDebug(globalPathFinding);
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void FixedUpdate() {

		// time, current pos, move faster if hurt
		if (this.movementComponent != null){
			this.movementComponent.moveOnPath(Time.fixedDeltaTime);

			// keep cat facing left or right (not spinning in a circle)
			this.faceCat();
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void faceCat(){

		// negate the gameobject rotation
		this.bodySprite.transform.localEulerAngles = new Vector3(0,0, -this.transform.eulerAngles.z);
		this.shadowSprite.transform.localEulerAngles = new Vector3(0,0, -this.transform.eulerAngles.z);

		// set facing direction (left or right)
		if (this.transform.eulerAngles.z > 90 && this.transform.eulerAngles.z < 270){
			this.bodySprite.flipX = true;
		}
		else {
			this.bodySprite.flipX = false;
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void OnCollisionEnter2D(Collision2D coll) {

		// hit fish?
		if (coll.gameObject.CompareTag(Constants.GAMEOBJECT_TAG_FISH)){

			// cat ate a fish
			this.movementComponent.retargetPathfinding();

			// meow
			if (this.audioSource != null){
				this.audioSource.Play();
			}
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void OnCollisionStay2D(Collision2D coll){

		// inside wall?
		if (coll.gameObject.CompareTag(Constants.GAMEOBJECT_TAG_WALL)){
			// collider contains the cat's position
			 if (coll.collider.bounds.Contains(transform.position)){
				// remove cat since spawned inside a wall
				Object.Destroy(this.gameObject);
			}
		}
	}
}
