// -------------------------------------------------------------------------------------------------
//  GameMain.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.EventSystems; // for EventSystem

// ------------------------------------------------------------------------
// ------------------------------------------------------------------------
public class GameMain : MonoBehaviour {

	public GameUI gameUI;
	public EventSystem eventSystem;
	private Camera mainCamera;
	private Tilemap tilemap;
	private Vector2 mouseScreenPos;
	private Vector2 mouseWorldPos;
	private GameObject catPrefab;
	private GameObject fishPrefab;
	private GameObject rootObjectsGO;
	private int catsSpawned = 0;

	// ------------------------------------------------------------------------
	// Use this for initialization
	// ------------------------------------------------------------------------
	public void Start(){

		// init pf config
		this.initConfig();

		// load Prefabs
		this.catPrefab = (GameObject)Resources.Load("Prefabs/Cat");
		this.fishPrefab = (GameObject)Resources.Load("Prefabs/Fish");

		// find the main world camera
		GameObject cameraObject = GameObject.Find(Constants.CAMERA_NAME_MAIN);
		this.mainCamera = cameraObject.GetComponent<Camera>();

		// start the game on map 1
		this.loadMap(1);
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void initConfig(){

		// if not set or set to zero
		if (PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_ADJ_COST, 0) == 0){
			PlayerPrefs.SetInt(Constants.PREFERENCE_PATHFINDING_ADJ_COST, Constants.PATHFINDING_CONFIG_DEFAULT_ADJ_COST);
			PlayerPrefs.SetInt(Constants.PREFERENCE_PATHFINDING_DIAG_COST, Constants.PATHFINDING_CONFIG_DEFAULT_DIAG_COST);
			PlayerPrefs.SetInt(Constants.PREFERENCE_PATHFINDING_BORDER_COST, Constants.PATHFINDING_CONFIG_DEFAULT_BORDER_COST);
			PlayerPrefs.SetInt(Constants.PREFERENCE_PATHFINDING_MAX_CHECKS, Constants.PATHFINDING_CONFIG_DEFAULT_MAX_CHECKS);
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void loadMap(int mapNumber){

		// nuke the remains of anything old and create new
		this.resetWorld();

		// reset vars
		this.catsSpawned = 0;
		Time.timeScale = 1.0f; // in case paused

		// create the new tilemap
		this.tilemap = new Tilemap(mapNumber);

		// update camera pan for this map to top left
		this.panCamera();

		// reset game ui state
		this.gameUI.reset();
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void resetWorld(){

		// destroy root objects
		Object.DestroyImmediate(GameObject.Find(Constants.TILEMAP_GO));
		Object.DestroyImmediate(GameObject.Find(Constants.OBJECTS_GO));
		Object.DestroyImmediate(GameObject.Find(Constants.TILEMAP_DEBUG_GO));

		// create new root for tiles/objects
		new GameObject(Constants.TILEMAP_GO);
		this.rootObjectsGO = new GameObject(Constants.OBJECTS_GO);
		// TILEMAP_DEBUG_GO will be created in movement component as needed

		// reset camera size/postiion
		this.mainCamera.orthographicSize = Constants.CAMERA_ZOOM_DEFAULT;
		this.mainCamera.transform.position = Constants.CAMERA_POSITION_DEFAULT;
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void Update(){

		// World mouse pos: where the cursor is over the map
		this.mouseScreenPos = Input.mousePosition; // 0,0 is bottom left of screen
		   this.mouseWorldPos = this.mainCamera.ScreenToWorldPoint(Input.mousePosition); // where in the world the mouse clicked

		this.handleMouseInput();
		this.handleKeyboardInput();
		this.updateCamera();
	}

	// ------------------------------------------------------------------------
	// note: this is simple (and very terrible) camera code
	// ------------------------------------------------------------------------
	private void updateCamera(){

		// update cam, but make sure mouse is not over any UI elements
		if (!eventSystem.IsPointerOverGameObject()){
			this.zoomCamera();
			this.panCamera();
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void zoomCamera(){

		// zoom camera out
		if (Input.GetAxis("Mouse ScrollWheel") < 0){
			this.mainCamera.orthographicSize += Constants.CAMERA_ZOOM_INCREMENT;
		}
		// zoom camera in
		else if (Input.GetAxis("Mouse ScrollWheel") > 0){
			this.mainCamera.orthographicSize -= Constants.CAMERA_ZOOM_INCREMENT;
		}

		// -----------------
		// range check min/max
		if (this.mainCamera.orthographicSize > Constants.CAMERA_ZOOM_MAX){ this.mainCamera.orthographicSize = Constants.CAMERA_ZOOM_MAX; }
		else if (this.mainCamera.orthographicSize < Constants.CAMERA_ZOOM_MIN){ this.mainCamera.orthographicSize = Constants.CAMERA_ZOOM_MIN; }

		// AND range check to see if camera is pulled back to be bigger than map width or height
		float cameraHeight = this.mainCamera.orthographicSize;
		float cameraWidth = cameraHeight * this.mainCamera.pixelWidth / this.mainCamera.pixelHeight;

		// width check
		if (cameraWidth * 2 > this.tilemap.mapDimensions.x){
			this.mainCamera.orthographicSize = 0.5f * this.tilemap.mapDimensions.x * this.mainCamera.pixelHeight / this.mainCamera.pixelWidth;
		}

		// AND height check
		if (cameraHeight * 2 > this.tilemap.mapDimensions.y){
			this.mainCamera.orthographicSize = 0.5f * this.tilemap.mapDimensions.y;
		}
	}

	// ------------------------------------------------------------------------
	// screen 0,0 is bottom left.
	// ------------------------------------------------------------------------
	private void panCamera(){

		// pan camera if mouse is near screen edge AND inside window
		Vector3 cameraPosition = this.mainCamera.transform.position;

		// pan camera left
		if (this.mouseScreenPos.x < Constants.CAMERA_SCROLL_TOLERANCE && this.mouseScreenPos.x > 0){
			// eases into the scroll as mouse gets closer to edge
			float edgeDistanceNormalized = this.mouseScreenPos.x / Constants.CAMERA_SCROLL_TOLERANCE;              // this is 1.0 at edge, 0.0 far from edge
			cameraPosition.x -= Mathf.Lerp(Constants.CAMERA_SCROLL_INCREMENT, 0.0f, edgeDistanceNormalized);      // interpolate between full amount and zero
		}
		// pan camera right
		else if (this.mouseScreenPos.x > (this.mainCamera.pixelWidth - Constants.CAMERA_SCROLL_TOLERANCE) && this.mouseScreenPos.x < this.mainCamera.pixelWidth){
			// eases into the scroll as mouse gets closer to edge
			float edgeDistanceNormalized = (this.mainCamera.pixelWidth - this.mouseScreenPos.x) / Constants.CAMERA_SCROLL_TOLERANCE; // FLIPPED: this is 0.0 at edge, 1.0 far from edge
			cameraPosition.x += Mathf.Lerp(Constants.CAMERA_SCROLL_INCREMENT, 0.0f, edgeDistanceNormalized);      // interpolate between full amount and zero
		}

		// pan camera up
		if (this.mouseScreenPos.y > (this.mainCamera.pixelHeight - Constants.CAMERA_SCROLL_TOLERANCE) && this.mouseScreenPos.y < this.mainCamera.pixelHeight){
			// eases into the scroll as mouse gets closer to edge
			float edgeDistanceNormalized = (this.mainCamera.pixelHeight - this.mouseScreenPos.y) / Constants.CAMERA_SCROLL_TOLERANCE; // FLIPPED: this is 0.0 at edge, 1.0 far from edge
			cameraPosition.y += Mathf.Lerp(Constants.CAMERA_SCROLL_INCREMENT, 0.0f, edgeDistanceNormalized);      // interpolate between full amount and zero
		}
		// pan camera down
		else if (this.mouseScreenPos.y < Constants.CAMERA_SCROLL_TOLERANCE && this.mouseScreenPos.y > 0){
			// eases into the scroll as mouse gets closer to edge
			float edgeDistanceNormalized = this.mouseScreenPos.y / Constants.CAMERA_SCROLL_TOLERANCE;              // this is 1.0 at edge, 0.0 far from edge
			cameraPosition.y -= Mathf.Lerp(Constants.CAMERA_SCROLL_INCREMENT, 0.0f, edgeDistanceNormalized);      // interpolate between full amount and zero
		}

		// -----------------
		// range check pan: world 0,0 is top left
		float cameraHeight = this.mainCamera.orthographicSize;
		float cameraWidth = cameraHeight * this.mainCamera.pixelWidth / this.mainCamera.pixelHeight;

		// X: left edge
		if (cameraPosition.x - cameraWidth < 0){
			cameraPosition.x = cameraWidth;
		}
		// right edge
		else if (cameraPosition.x + cameraWidth > this.tilemap.mapDimensions.x){
			cameraPosition.x = this.tilemap.mapDimensions.x - cameraWidth;
		}

		// Y: top edge
		if (cameraPosition.y + cameraHeight > 0){
			cameraPosition.y = -cameraHeight;
		}
		// bottom edge (need to negate since y is flipped map)
		else if (cameraPosition.y - cameraHeight < -this.tilemap.mapDimensions.y){
			cameraPosition.y = -this.tilemap.mapDimensions.y + cameraHeight;
		}

		// -----------------
		// assign new position back to camera
		this.mainCamera.transform.position = cameraPosition;
	}

	// ------------------------------------------------------------------------
	// call before camera is checked in updateCamera
	// ------------------------------------------------------------------------
	public void handleKeyboardInput(){

		// was the escape key hit? then quit
		if (Input.GetKeyDown(KeyCode.Escape)){
			Application.Quit();
		}

		// maps M+1..5
		if (Input.GetKey(KeyCode.M)){
			if (Input.GetKeyDown(KeyCode.Alpha1)){      this.loadMap(1); }
			else if (Input.GetKeyDown(KeyCode.Alpha2)){ this.loadMap(2); }
			else if (Input.GetKeyDown(KeyCode.Alpha3)){ this.loadMap(3); }
			else if (Input.GetKeyDown(KeyCode.Alpha4)){ this.loadMap(4); }
			else if (Input.GetKeyDown(KeyCode.Alpha5)){ this.loadMap(5); }
		}

		// these keys can all be pressed at once

		// WASD/Arrows to move map
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)){
			Vector3 cameraPosition = this.mainCamera.transform.position;
			cameraPosition.x -= Constants.CAMERA_SCROLL_INCREMENT;
			this.mainCamera.transform.position = cameraPosition;
		}
		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)){
			Vector3 cameraPosition = this.mainCamera.transform.position;
			cameraPosition.x += Constants.CAMERA_SCROLL_INCREMENT;
			this.mainCamera.transform.position = cameraPosition;
		}
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)){
			 Vector3 cameraPosition = this.mainCamera.transform.position;
			cameraPosition.y += Constants.CAMERA_SCROLL_INCREMENT;
			this.mainCamera.transform.position = cameraPosition;
		}
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)){
			Vector3 cameraPosition = this.mainCamera.transform.position;
			cameraPosition.y -= Constants.CAMERA_SCROLL_INCREMENT;
			this.mainCamera.transform.position = cameraPosition;
		}

		// camera zoom in/out
		// - bind
		if (Input.GetKey(KeyCode.Minus)){
			this.mainCamera.orthographicSize += Constants.CAMERA_ZOOM_INCREMENT;
		}
		// =/+ bind shifted/not shifted
		if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.Plus)){
			this.mainCamera.orthographicSize -= Constants.CAMERA_ZOOM_INCREMENT;
		}

		// c = cat
		if (Input.GetKeyUp(KeyCode.C)){
			this.spawnCat();
		}

		// f = fish
		if (Input.GetKeyUp(KeyCode.F)){
			this.spawnFish();
		}

		// v = pf visualize
		if (Input.GetKeyUp(KeyCode.V)){
			if (this.gameUI != null){
				this.gameUI.togglePathfindingClicked();
			}
		}

		// p = pause
		if (Input.GetKeyUp(KeyCode.P)){
			this.gameUI.togglePauseClicked();
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void handleMouseInput(){

		// left click = visualize cat path
		if (Input.GetMouseButtonUp(0)){
			// this prevents clicking a UI element and spawning a cat underneath
			if (!eventSystem.IsPointerOverGameObject()){
				this.spawnCat();
			}
		}

		// right click = fish
		if (Input.GetMouseButtonUp(1)){
			// this prevents clicking a UI element and spawning a fish underneath
			if (!eventSystem.IsPointerOverGameObject()){
				this.spawnFish();
			}
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void spawnCat(){

		// if clicked inside the map border
		if (this.isInsideMap(this.mouseWorldPos)){

			// if still under spawn cap
			if (this.catsSpawned < Constants.MAX_CATS){
				this.catsSpawned++;

				// instantiate
				GameObject catInstance = (GameObject)Instantiate(catPrefab, new Vector3 (this.mouseWorldPos.x, this.mouseWorldPos.y, 0f), Quaternion.identity);
				catInstance.transform.SetParent(this.rootObjectsGO.transform);
				Cat catComponent = (Cat)catInstance.GetComponent(typeof(Cat));
				catComponent.enablePathfinding(this.tilemap.pathFinding);
			}
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void spawnFish(){

		// if clicked inside the map border
		if (this.isInsideMap(this.mouseWorldPos)){
			GameObject fishInstance = (GameObject)Instantiate(fishPrefab, new Vector3 (this.mouseWorldPos.x, this.mouseWorldPos.y, 0f), Quaternion.identity);
			fishInstance.transform.SetParent(this.rootObjectsGO.transform);
		}
	}

	// ------------------------------------------------------------------------
	// make sure spawn point is inside the map AND the 1 tile border around map
	// ------------------------------------------------------------------------
	private bool isInsideMap(Vector3 clickPos){

		clickPos.y = -clickPos.y; // flip since y is flipped between map/world

		// if click is inside the map's 1 tile border then ok to spawn
		if (clickPos.x > 1 &&
			clickPos.x < this.tilemap.mapDimensions.x - 1 &&
			clickPos.y > 1 &&
			clickPos.y < this.tilemap.mapDimensions.y - 1){
				return true;
		}

		return false;
	}

} // eof