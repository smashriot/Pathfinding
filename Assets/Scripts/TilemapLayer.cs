// -------------------------------------------------------------------------------------------------
//  TilemapLayer.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using System;

public class TilemapLayer : MonoBehaviour {

	private GameObject rootGameObject;
	private int[] tileArray;
	public string layerName;
	public int tilesWide;
	public int tilesHigh;

	// prefabs
	public GameObject dirtPrefab;
	public GameObject grassPrefab;
	public GameObject lavaPrefab;
	public GameObject sandPrefab;
	public GameObject wallPrefab;
	public GameObject waterPrefab;

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public void loadLayer(string text, GameObject rootGO){

		// load prefabs
		this.dirtPrefab = (GameObject)Resources.Load("Prefabs/Dirt");
		this.grassPrefab = (GameObject)Resources.Load("Prefabs/Grass");
		this.lavaPrefab = (GameObject)Resources.Load("Prefabs/Lava");
		this.sandPrefab = (GameObject)Resources.Load("Prefabs/Sand");
		this.wallPrefab = (GameObject)Resources.Load("Prefabs/Wall");     // has a collider
		this.waterPrefab = (GameObject)Resources.Load("Prefabs/Water");

		// parent holding all the tiles
		this.rootGameObject = rootGO;

		// parse map into tileArray
		this.parseMap(text);

		// build the tiles from tileArray
		if (this.rootGameObject != null){
			this.buildTiles();
		}
		else {
			Debug.Log("TilemapLayer: NULL Root GameObject");
		}
	}

	// ------------------------------------------------------------------------
	// return size in tiles
	// ------------------------------------------------------------------------
	public Vector2 getMapDimensions(){
		return new Vector2(this.tilesWide * Constants.TILEMAP_TILE_PX_WIDTH, this.tilesHigh * Constants.TILEMAP_TILE_PX_HEIGHT); // in px
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public int[] getTileArray(){
		return tileArray;
	}

	// ------------------------------------------------------------------------
	// parse the CSV into tile IDs
	// ------------------------------------------------------------------------
	private void parseMap(string text){

		string[] lines = text.Split('\n');
		int x = 0;
		int y = 0;

		// set width/height
		string[] firstLine = lines[0].Split(',');
		this.tilesWide = firstLine.GetLength(0);
		if (firstLine[firstLine.GetLength(0)-1] == ""){
			this.tilesWide -= 1;
		}
		this.tilesHigh = lines.GetLength(0);

		// set array
		this.tileArray = new int[this.tilesWide * this.tilesHigh];

		// check each line -- replace foreach
		foreach (string line in lines){
			if (line != ""){ // skip empty rows

				// split into individual numbers
				string[] tiles = line.Split(',');

				x = 0;
				foreach (string tile in tiles){
					if (tile != ""){
						// keep track of all tiles
						int tileNum = int.Parse(tile);
						this.tileArray[x + (y*this.tilesWide)] = tileNum;
						x++;
					}
				}

				y++;
			}
		} // end lines
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	protected void buildTiles(){

		// make array of sprites
		for (int x = 0; x < this.tilesWide; x++){
			for (int y = 0; y < this.tilesHigh; y++){
				int tileNum = this.tileArray[x + (y*this.tilesWide)];

				// a non-zero tile
				if (tileNum > 0){

					// offset sprite coordinates
					float xPos = (x * Constants.TILEMAP_TILE_PX_WIDTH) + (0.5f * Constants.TILEMAP_TILE_PX_WIDTH);
					float yPos = -(y * Constants.TILEMAP_TILE_PX_HEIGHT) - (0.5f * Constants.TILEMAP_TILE_PX_HEIGHT); // negate since tilemapper y is flipped

					GameObject tilePrefab = null;
					// create the tile
					switch (tileNum){
						case Constants.TILE_ID_WALL:
							tilePrefab = wallPrefab; break;
						case Constants.TILE_ID_GRASS:
							tilePrefab = grassPrefab; break;
						case Constants.TILE_ID_WATER:
							tilePrefab = waterPrefab; break;
						case Constants.TILE_ID_DIRT:
							tilePrefab = dirtPrefab; break;
						//case Constants.TILE_ID_LAVA:
						//	tilePrefab = lavaPrefab; break;
						//case Constants.TILE_ID_SAND:
						//	tilePrefab = sandPrefab; break;
						default:
							tilePrefab = grassPrefab; break;
					}

					// create it
					if (tilePrefab != null){
						// instantiate
						GameObject tileInstance = (GameObject)Instantiate(tilePrefab, new Vector3 (xPos, yPos, 0f), Quaternion.identity);

						// parent to TileManager
						tileInstance.transform.SetParent(this.rootGameObject.transform);
					}
					else {
						Debug.Log("buildTiles: Unmapped Tile ID:" + tileNum + " Prefab:" + tilePrefab);
					}
				} // end tilenum > 0
			} // end y
		} // end x
	}
}