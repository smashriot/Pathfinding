// -------------------------------------------------------------------------------------------------
//  Tilemap.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;

// ------------------------------------------------------------------------
// ------------------------------------------------------------------------
public class Tilemap {

	private GameObject rootTilesGO;
	private TilemapLayer collisionLayer;
	private TilemapLayer backgroundLayer;
	public Vector2 mapDimensions; // in tiles
	public PathFinding pathFinding; // global pf

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public Tilemap(int mapToLoad){

		// create new root for tiles/objects
		this.rootTilesGO = GameObject.Find(Constants.TILEMAP_GO);

		// load/parse the TMX
		this.loadTMX("Maps/mapL" + mapToLoad);
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void loadTMX(string fileName){

		// load xml document
		TextAsset dataAsset = (TextAsset)Resources.Load(fileName, typeof(TextAsset));
		if (!dataAsset){
			Debug.Log("Tilemap: Couldn't load the xml data from: " + fileName);
		}
		else {

			// read contents and unload file
			string fileTMX = dataAsset.ToString();
			Resources.UnloadAsset(dataAsset);

			// parse the tmx
			this.parseTMX(fileTMX);

			// set map size - parent uses this for scrolling
			this.mapDimensions = this.backgroundLayer.getMapDimensions(); // in tiles
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void parseTMX(string fileDataTMX){

		// parse xml string
		XMLReader parser = new XMLReader();
		XMLNode xmlNode = parser.read(fileDataTMX);
		XMLNode rootNode = xmlNode.children[0] as XMLNode;

		// loop through all children
		foreach (XMLNode child in rootNode.children){

			// create tile layer for layer nodes and add to tilemap list array
			if (child.tagName == "layer" && child.children.Count > 0){
				this.createTilemapLayer(child);
			}
		}

		// after tilemap is loaded, setup pathfinding
		if (this.collisionLayer != null && this.backgroundLayer != null){
			this.pathFinding = new PathFinding(this.collisionLayer.getTileArray(), this.backgroundLayer.getTileArray(), this.collisionLayer.tilesWide, this.collisionLayer.tilesHigh);
		}
	}

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	private void createTilemapLayer(XMLNode node){

		// parse TMX
		XMLNode csvData = new XMLNode();
		foreach (XMLNode child in node.children){
			if (child.tagName == "data"){
				csvData = child;
			}
		}

		// make sure encoding is set to csv
		if (csvData.attributes["encoding"] != "csv"){
			Debug.Log("createTilemapLayer: Could not render layer data, encoding set to: " + csvData.attributes["encoding"]);
			return;
		}

		// new tilemap component for each layer
		TilemapLayer tilemapLayer = this.rootTilesGO.AddComponent<TilemapLayer>();
		tilemapLayer.loadLayer(csvData.value, this.rootTilesGO);
		tilemapLayer.layerName = node.attributes["name"];

		// was this the collision layer?  if so, setup pathfinding
		if (node.attributes["name"] == "Collision"){
			// set ref
			this.collisionLayer = tilemapLayer;
		}
		// background layer
		else if (node.attributes["name"] == "Background"){
			this.backgroundLayer = tilemapLayer;
		}
	}
}
