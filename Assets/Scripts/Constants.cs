// -------------------------------------------------------------------------------------------------
//  Constants.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;

// --------------------------------------------------------------------------------------
public static class Constants {

	// tilemap
	public const string TILEMAP_GO = "TileManager";
	public const string TILEMAP_DEBUG_GO = "TileDebug";
	// tile prefab sprites are set to 1px to 1 world point and are 16px wide
	public const int TILEMAP_TILE_PX_WIDTH = 16;
	public const int TILEMAP_TILE_PX_HEIGHT = 16;

	// objects
	public const string OBJECTS_GO = "ObjectManager";

	// camera
	public const string CAMERA_NAME_MAIN = "Main Camera";
	public static readonly Vector3 CAMERA_POSITION_DEFAULT = new Vector3(0,0,-10);

	// zoom
	public const float CAMERA_ZOOM_INCREMENT = 4;
	public const float CAMERA_ZOOM_DEFAULT = 100;
	public const float CAMERA_ZOOM_MIN = 48;
	public const float CAMERA_ZOOM_MAX = 320;

	// scroll
	public const float CAMERA_SCROLL_INCREMENT = 4;
	public const float CAMERA_SCROLL_TOLERANCE = 150; // in px from screen edge

	// game object tags - these must be defined Edit->ProjectSettings->Tags and Layers
	public const string GAMEOBJECT_TAG_DEFAULT = "default";
	public const string GAMEOBJECT_TAG_WALL = "wall";
	public const string GAMEOBJECT_TAG_CAT = "cat";
	public const string GAMEOBJECT_TAG_FISH = "fish";

	// game object names
	public const string GAMEOBJECT_NAME_WALL = "Wall";
	public const string GAMEOBJECT_NAME_FISH = "Fish";
	public const string GAMEOBJECT_NAME_CAT = "Cat";

	// collision layers, custom start at 8 - these must be defined Edit->ProjectSettings->Tags and Layers
	public const int COLLISION_LAYER_DEFAULT = 0;
	public const int COLLISION_LAYER_WALL = 8;
	public const int COLLISION_LAYER_OBJECTS = 9;
	public const int COLLISION_LAYER_OFF = 10;

	// pathfinding
	public const float PATHFINDING_ANIMAL_UPDATE_INTERVAL = 2.0f;
	public const float PATHFINDING_LOCATE_TARGET_INTERVAL = 1.0f;
	public const float PATHFINDING_LOCATE_TARGET_DIST = 1600.0f;
	public const string PATHFINDING_SQUARE_SPRITE = "particle";
	public const string PATHFINDING_ARROW_SPRITE = "arrow";

	// pf config - these store the vars
	public const string PREFERENCE_PATHFINDING_ADJ_COST = "adjCost";
	public const string PREFERENCE_PATHFINDING_DIAG_COST = "diagCost";
	public const string PREFERENCE_PATHFINDING_BORDER_COST = "borderCost";
	public const string PREFERENCE_PATHFINDING_MAX_CHECKS = "maxChecks";
	public const string PREFERENCE_PATHFINDING_WATER_COST = "waterCost";
	public const string PREFERENCE_PATHFINDING_GRASS_COST = "grassCost";
	public const string PREFERENCE_PATHFINDING_DIRT_COST = "dirtCost";

	// pf costs
	public const int PATHFINDING_CONFIG_DEFAULT_ADJ_COST = 10;
	public const int PATHFINDING_CONFIG_DEFAULT_DIAG_COST = 14;
	public const int PATHFINDING_CONFIG_DEFAULT_BORDER_COST = 20;     // 2x adjacent
	public const int PATHFINDING_CONFIG_DEFAULT_MAX_CHECKS = 500;
	public const int PATHFINDING_CONFIG_FINAL_MAX_CHECKS = 999;     // limited by mesh size with debug labels.

	// tile types
	public const int TILE_ID_WALL = 1;
	public const int TILE_ID_GRASS = 2;
	public const int TILE_ID_WATER = 3;
	public const int TILE_ID_DIRT = 4;
	public const int TILE_ID_LAVA = 5;
	public const int TILE_ID_SAND = 6;

	// animal
	public const int MAX_CATS = 10;
	public const float PHYSICS_ANIMAL_MOVE_FORCE = 30f;
	public const float PHYSICS_ANIMAL_TURNING_RATE = 10.0f;
	public const float CAT_MAX_FISH = 10.0f;
	public const float CAT_FISH_PER_MEAL = 0.5f; // less than 1.0 makes the meter go up slower
	public const float CAT_FISH_DECAY_RATE_FRAME = 0.002f;
	public static Vector3 CAT_SCALE_PER_FISH = new Vector3(0.05f, 0.05f, 0.05f); // max 1.5 scale at 10 fish
	public const float CAT_MOVEMENT_INCREASE_PER_FISH = 0.1f; // max 2.0 at 10 fish
	public const float MOVEMENT_FORCE_MODIFIER_MIN = 1.0f;
	public const float MOVEMENT_FORCE_MODIFIER_MAX = 2.0f;
	public static Color CAT_COLOR_FISH_EATEN_DANGER = new Color(1.0f, 0.5f, 0.5f, 1.0f);
	public static Color CAT_COLOR_FISH_EATEN_WARNING = new Color(1.0f, 0.75f, 0.75f, 1.0f);
	public static Color CAT_COLOR_FISH_EATEN_NORMAL = new Color(1.0f, 1.0f, 1.0f, 1.0f);

} // eof