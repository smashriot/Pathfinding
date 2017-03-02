// -------------------------------------------------------------------------------------------------
//  PathFinding.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
//
// NOTE: This A* code is favoring simplicity/readbility over speed to clearly teach core concepts
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PathFinding {

	// config via preference
	private int PF_COST_ADJACENT = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_ADJ_COST, Constants.PATHFINDING_CONFIG_DEFAULT_ADJ_COST);
	private int PF_COST_DIAGONAL = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_DIAG_COST, Constants.PATHFINDING_CONFIG_DEFAULT_DIAG_COST);
	private const int PF_NODE_WALL = 0;
	private const int PF_NODE_BORDER = 1;
	private const int PF_NODE_OPEN = 2;
	private int[] nodeArray;        // array of ints if walkable or not: 0: not walkable, 1: border, 2: walkable
	private int nodeArrayWidth = 0;
	private int nodeArrayHeight = 0;
	private int nodeNumber = 0;     // used for debug

	// state+path arrays
	public List<PathFindingNode> openList = new List<PathFindingNode>();
	public List<PathFindingNode> closedList = new List<PathFindingNode>();
	public List<PathFindingNode> pathList = new List<PathFindingNode>();

	// ------------------------------------------------------------------------
	// ------------------------------------------------------------------------
	public PathFinding(int[] baseNodeArray, int width, int height){

		// node array
		this.nodeArrayWidth = width;
		this.nodeArrayHeight = height;
		this.nodeArray = new int[(int)(this.nodeArrayWidth * this.nodeArrayHeight)];

		// set each nodeArray value to wall/open based on source array
		for (int y = 0; y < this.nodeArrayHeight; y++){
			for (int x = 0; x < this.nodeArrayWidth; x++){
				int baseNodeID = baseNodeArray[(int)(x + (y * this.nodeArrayWidth))]; // array of 0/1s
				Debug.Log("X:" + x + " Y:" + y + " TILEID:" + baseNodeID);

				// not walkable
				if (baseNodeID > 0){
					this.nodeArray[(int)(x + (y * this.nodeArrayWidth))] = PF_NODE_WALL;
				}
				else {
					this.nodeArray[(int)(x + (y * this.nodeArrayWidth))] = PF_NODE_OPEN;
				}
			}
		}

		// set which nodes are border nodes now
		for (int x = 0; x < this.nodeArrayWidth; x++){
			for (int y = 0; y < this.nodeArrayHeight; y++){
				// for each open node
				if (this.getNodeAtPos(x,y) == PF_NODE_OPEN){
					// check if it is a border node and set if so
					if (this.determineIfBorderNode(x, y)){
						this.nodeArray[(int)(x + (y * this.nodeArrayWidth))] = PF_NODE_BORDER;
					}
				}
			}
		}
	}

	// -------------------------------------------------------------------------------------------------
	// set bool y/n on walkable - used to update the PF map during play (e.g. wall was destroyed)
	// -------------------------------------------------------------------------------------------------
	public void setNodeWalkable(Vector2 nodePos, bool walkable = true){

		if (this.isNodeInRange(nodePos)){

			int nodeX = (int)nodePos.x;
			int nodeY = (int)nodePos.y;
			int nodeArrayPos = this.getNodeArrayPos(nodeX,nodeY);

			// set as open for now. will check border below
			if (walkable){
				this.nodeArray[nodeArrayPos] = PF_NODE_OPEN;
			}
			// not walkable anymore
			else {
				this.nodeArray[nodeArrayPos] = PF_NODE_WALL;
			}

			// update the surrounding nodes
			this.updateSurroundingNodesWalkable(nodePos);
		}
	}

	// -------------------------------------------------------------------------------------------------
	// check/set border then need to sample all 10 points (center + 9 around) and update too..
	// -------------------------------------------------------------------------------------------------
	private void updateSurroundingNodesWalkable(Vector2 nodePos){

		int nodeX = (int)nodePos.x;
		int nodeY = (int)nodePos.y;

		// recheck this node and surrounding nodes to see if it's a border tile since center was just set walkable
		for (int y=nodeY-1; y<=nodeY+1; y++){
			for (int x=nodeX-1; x<=nodeX+1; x++){

				// for each open node (walk/border) - not wall
				if (this.getNodeAtPos(x,y) != PF_NODE_WALL){
					// check if it is a border node and set if so
					if (this.determineIfBorderNode(x, y)){
						this.nodeArray[(int)(x + (y * this.nodeArrayWidth))] = PF_NODE_BORDER;
					}
					// not a border so set open
					else {
						this.nodeArray[(int)(x + (y * this.nodeArrayWidth))] = PF_NODE_OPEN;
					}
				} // end open
			} // end x
		} // end y
	}

	// -------------------------------------------------------------------------------------------------
	// bool y/n walkable
	// -------------------------------------------------------------------------------------------------
	public bool isNodeWalkable(int x, int y){

		// NOT A WALL
		if (this.getNodeAtPos(x,y) != PF_NODE_WALL){
			return true;
		}

		return false;
	}

	// -------------------------------------------------------------------------------------------------
	// touches a node that is not walkable - reads prestored value from array
	// -------------------------------------------------------------------------------------------------
	private bool isBorderNode(int x, int y){

		if (this.getNodeAtPos(x,y) == PF_NODE_BORDER){
			return true;
		}

		return false;
	}

	// -------------------------------------------------------------------------------------------------
	// touches a node that is not walkable
	// -------------------------------------------------------------------------------------------------
	private bool determineIfBorderNode(int x, int y){

		// indicate a border if any of the sides are not walkable
		if (!this.isNodeWalkable(x,   y+1) ||  // N
			!this.isNodeWalkable(x+1, y+1) ||  // NE
			!this.isNodeWalkable(x+1, y) ||    // E
			!this.isNodeWalkable(x+1, y-1) ||  // SE
			!this.isNodeWalkable(x,   y-1) ||  // S
			!this.isNodeWalkable(x-1, y-1) ||  // SW
			!this.isNodeWalkable(x-1, y) ||    // W
			!this.isNodeWalkable(x-1, y+1)){   // NW

			return true; // border node
		}

		return false; // not a border node
	}

	// -------------------------------------------------------------------------------------------------
	// Determine Path from [(1600.0, -704.0)] to [(672.0, -512.0)] in 50 node checks
	// -------------------------------------------------------------------------------------------------
	public Vector2 convertPointsToNode(Vector2 position){

		// convert world point to node position
		Vector2 nodePos = new Vector2((int)(position.x / Constants.TILEMAP_TILE_PX_WIDTH), (int)(position.y / Constants.TILEMAP_TILE_PX_HEIGHT));
		//Vector2 nodePos = new Vector2((int)position.x , (int)position.y);

		// range check
		if (nodePos.x < 0) nodePos.x = 0;
		if (nodePos.y < 0) nodePos.y = 0;
		if (nodePos.x >= this.nodeArrayWidth) nodePos.x = this.nodeArrayWidth-1;
		if (nodePos.y >= this.nodeArrayHeight) nodePos.y = this.nodeArrayHeight-1;

		return nodePos;
	}

	// -------------------------------------------------------------------------------------------------
	// -------------------------------------------------------------------------------------------------
	private bool areVectorsEqualInt(Vector2 firstVector, Vector2 secondVector){

		return ((int)firstVector.x == (int)secondVector.x) && ((int)firstVector.y == (int)secondVector.y);
	}

	// -------------------------------------------------------------------------------------------------
	// check to see if the first vector is equal to the second vector +/- tolerance
	// -------------------------------------------------------------------------------------------------
	public bool areVectorsEqualIntTolerance(Vector2 firstVector, Vector2 secondVector, int tolerance){

		return ( ((int)firstVector.x >= (int)secondVector.x - tolerance) &&
				 ((int)firstVector.x <= (int)secondVector.x + tolerance) &&
				 ((int)firstVector.y >= (int)secondVector.y - tolerance) &&
				 ((int)firstVector.y <= (int)secondVector.y + tolerance) );
	}

	// -------------------------------------------------------------------------------------------------
	// -------------------------------------------------------------------------------------------------
	private int getNodeAtPos(int x, int y){

		// first check if in range
		if (this.isNodeInRange(new Vector2(x, y))){

			return this.nodeArray[this.getNodeArrayPos(x,y)];
		}

		return PF_NODE_WALL;
	}

	// -------------------------------------------------------------------------------------------------
	// -------------------------------------------------------------------------------------------------
	private int getNodeArrayPos(int x, int y){

		 return (int)(x + (y * this.nodeArrayWidth));
	}

	// -------------------------------------------------------------------------------------------------
	// -------------------------------------------------------------------------------------------------
	private bool isNodeInRange(Vector2 nodePos){

		int x = (int)nodePos.x;
		int y = (int)nodePos.y;

		// array size check
		int nodeArrayPos = this.getNodeArrayPos(x,y);
		if (nodeArrayPos >= this.nodeArray.Length){
			return false;
		}

		// check again
		if (x < 0) return false;
		if (y < 0) return false;
		if (x >= this.nodeArrayWidth) return false;
		if (y >= this.nodeArrayHeight) return false;

		return true;
	}

	// *************************************************************************************************
	// ACTIVE PF FUNCTIONS
	// *************************************************************************************************

	// -------------------------------------------------------------------------------------------------
	// -------------------------------------------------------------------------------------------------
	public bool determinePath(Vector2 startPosition, Vector2 endPosition, int maxNodeChecks){

		// clear working lists
		this.openList.Clear();
		this.closedList.Clear();
		this.pathList.Clear();

		// reset
		bool pathGenerated = false;

		// range check
		if (endPosition.x < 0) endPosition.x = 0;
		if (endPosition.y < 0) endPosition.y = 0;
		if (endPosition.x >= this.nodeArrayWidth) endPosition.x = this.nodeArrayWidth - 1;
		if (endPosition.y >= this.nodeArrayHeight) endPosition.y = this.nodeArrayHeight - 1;

		// make sure end node is walkable
		if (this.isNodeWalkable((int)endPosition.x, (int)endPosition.y)){

			// find the path from start node
			PathFindingNode currentNode = new PathFindingNode(startPosition);
			currentNode.setScoreEstimate(endPosition);
			currentNode.setScoreAbs(1);

			// add this initial node to the open list
			currentNode.nodeNumber = this.nodeNumber = 0;
			this.openList.Add(currentNode);

			// loop until match: loop while still nodes in openList and the current node != dest node
			int movesRemaining = maxNodeChecks;
			while ((currentNode != null) && (this.openList.Count > 0) && !this.areVectorsEqualInt(currentNode.nodePosition, endPosition) && (movesRemaining-- > 0)){

				// remove from open and add to closed
				this.openList.Remove(currentNode);
				this.closedList.Add(currentNode);

				// add surrounding nodes to open list. up down left right cost (10) and diagonal cost (14)
				// addNeighborNode(PathFindingNode parentNode, int absScore, Vector2 nodePosition, Vector2 endPosition)
				this.addNeighborNode(currentNode, PF_COST_ADJACENT, new Vector2(currentNode.nodePosition.x,     currentNode.nodePosition.y + 1), endPosition); // N
				this.addNeighborNode(currentNode, PF_COST_DIAGONAL, new Vector2(currentNode.nodePosition.x + 1, currentNode.nodePosition.y + 1), endPosition); // NE
				this.addNeighborNode(currentNode, PF_COST_ADJACENT, new Vector2(currentNode.nodePosition.x + 1, currentNode.nodePosition.y),     endPosition); // E
				this.addNeighborNode(currentNode, PF_COST_DIAGONAL, new Vector2(currentNode.nodePosition.x + 1, currentNode.nodePosition.y - 1), endPosition); // SE
				this.addNeighborNode(currentNode, PF_COST_ADJACENT, new Vector2(currentNode.nodePosition.x,     currentNode.nodePosition.y - 1), endPosition); // S
				this.addNeighborNode(currentNode, PF_COST_DIAGONAL, new Vector2(currentNode.nodePosition.x - 1, currentNode.nodePosition.y - 1), endPosition); // SW
				this.addNeighborNode(currentNode, PF_COST_ADJACENT, new Vector2(currentNode.nodePosition.x - 1, currentNode.nodePosition.y),     endPosition); // W
				this.addNeighborNode(currentNode, PF_COST_DIAGONAL, new Vector2(currentNode.nodePosition.x - 1, currentNode.nodePosition.y + 1), endPosition); // NW

				// get lowest score on open list and add to closed list
				currentNode = this.getLowestOpenListNode();
			}

			// we end on a match?
			if (currentNode != null && this.areVectorsEqualInt(currentNode.nodePosition, endPosition)){

				// remove from open and add to closed
				this.openList.Remove(currentNode);
				this.closedList.Add(currentNode);

				// generate the pathList array. set TRUE if good
				pathGenerated = this.generatePathArray(startPosition);
			}
		}

		// return T/F on if we generated a path
		return pathGenerated;
	}

	// -------------------------------------------------------------------------------------------------
	// currentNode is the current node trying to add a child
	// -------------------------------------------------------------------------------------------------
	private void addNeighborNode(PathFindingNode currentNode, int absCost, Vector2 nodePosition, Vector2 endPosition){

		// range check -- if anything is out of bounds, return
		if (this.isNodeInRange(nodePosition)){

			// is the node walkable?  if this is not the end node, then expand out if requested (e.g. for big ships). but final node is tight (no expansion)
			if (this.isNodeWalkable((int)nodePosition.x, (int)nodePosition.y) || this.areVectorsEqualInt(nodePosition, endPosition)){

				// not on closed list?
				if (!this.isNodeOnClosedList(nodePosition)){

					// not on open list
					if (!this.isNodeOnOpenList(nodePosition)){

						// is this a border node?
						bool borderNode = this.isBorderNode((int)nodePosition.x, (int)nodePosition.y);

						// create new node
						PathFindingNode newNode = new PathFindingNode(nodePosition, borderNode);
						newNode.setScoreEstimate(endPosition);
						newNode.setScoreAbs(currentNode.nodeScoreAbs + absCost); // absCost is the PF_COST_ADJACENT/PF_COST_DIAGONAL
						newNode.setNodeParent(currentNode); // set currentNode as parent of the new node

						// add this node to the open list
						newNode.nodeNumber = this.nodeNumber++;
						this.openList.Add(newNode);
					} // end not open
					// couldn't add node since its already on open list. check ABS score, maybe its a better route
					else {
						PathFindingNode adjOpenNode = this.getNodeFromOpenList(nodePosition);
						// does the adj node on the open list have better ABS score than current parent?
						if (adjOpenNode.nodeScoreAbs < currentNode.nodeParent.nodeScoreAbs){
							//Debug.Log("MOVING NODE:" + currentNode.nodePosition + " FROM:" + currentNode.nodeParent.nodePosition + " TO:" + adjOpenNode.nodePosition);
							currentNode.setNodeParent(adjOpenNode); // set currentNode as parent of the new node
							currentNode.setScoreAbs(adjOpenNode.nodeScoreAbs + absCost); // absCost is the PF_COST_ADJACENT/PF_COST_DIAGONAL
							// estimate stays the same.
						}
					} // end on open
				} // end not closed
			} // end is walkable
		} // end is in range
	}

	// -------------------------------------------------------------------------------------------------
	// -------------------------------------------------------------------------------------------------
	private bool isNodeOnClosedList(Vector2 targetPosition){

		for (int i=0; i<this.closedList.Count; i++){
			PathFindingNode closedListNode = (PathFindingNode)this.closedList[i];
			// found a match?
			if (this.areVectorsEqualInt(targetPosition, closedListNode.nodePosition)){
				return true;
			}
		}

		return false;
	}

	// -------------------------------------------------------------------------------------------------
	// -------------------------------------------------------------------------------------------------
	 private bool isNodeOnOpenList(Vector2 targetPosition){

		for (int i=0; i<this.openList.Count; i++){
			PathFindingNode openListNode = (PathFindingNode)this.openList[i];
			// found a match?
			if (areVectorsEqualInt(targetPosition, openListNode.nodePosition)){
				return true;
			}
		}

		return false;
	}

	 // -------------------------------------------------------------------------------------------------
	 // -------------------------------------------------------------------------------------------------
	 private PathFindingNode getNodeFromOpenList(Vector2 targetPosition){

		for (int i=0; i<this.openList.Count; i++){
			PathFindingNode openListNode = (PathFindingNode)this.openList[i];
			// found a match?
			if (areVectorsEqualInt(targetPosition, openListNode.nodePosition)){
				return openListNode;
			}
		}

		return null;
	}

	// -------------------------------------------------------------------------------------------------
	// this finds current lowest scoring open node
	// -------------------------------------------------------------------------------------------------
	private PathFindingNode getLowestOpenListNode(){

		int lowestPos = 0;
		int lowestScore = 0;

		// we have some reference, so copy the array
		if (this.openList.Count > 0){

			// check each object in the open list
			for (int i=0; i<this.openList.Count; i++){
				PathFindingNode openNode = (PathFindingNode)this.openList[i];

				// initial: set lowest to this first node
				if (i == 0){
					lowestScore = openNode.getNodeScore();
					lowestPos = i;
				}
				// find a node with lower score than the current lowest
				else if (openNode.getNodeScore() <= lowestScore){
					lowestScore = openNode.getNodeScore();
					lowestPos = i;
				}
			}

			// return the lowest
			return (PathFindingNode)this.openList[lowestPos];
		}

		// nothing remaining in openList
		return null;
	}

	// -------------------------------------------------------------------------------------------------
	// this makes a LIFO queue from destination to start (and stores in this.pathList). last object is the next to go to.
	// -------------------------------------------------------------------------------------------------
	private bool generatePathArray(Vector2 startPosition){

		bool pathGenerated = false;

		if (this.closedList.Count > 0){

			// get the node and then it's parent, when we find a match, the last entry in the clsosed list contains the final (dest) node.  work back from that.
			PathFindingNode nodeNode = (PathFindingNode)this.closedList[this.closedList.Count-1];
			PathFindingNode parentNode = nodeNode.nodeParent;

			// add destination to path list
			this.pathList.Add(nodeNode);

			// while we aren't at the origin and we have a parent node.  start node has a null parent.
			while (parentNode != null){

				// add to path list
				this.pathList.Add(parentNode);

				// get parent of the current nodeNode.parent
				parentNode = parentNode.nodeParent;
			}

			// we generated a path
			pathGenerated = true;
		}

		return pathGenerated;
	}

} // eof