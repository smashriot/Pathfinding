// -------------------------------------------------------------------------------------------------
//  PathFindingNode.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using System;

public class PathFindingNode {

    // cost for a border node (touches a wall) which tries to keep path off the wall
    private int PFN_BORDER_COST = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_BORDER_COST, Constants.PATHFINDING_CONFIG_DEFAULT_BORDER_COST);
    private int PF_COST_ADJACENT = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_ADJ_COST, Constants.PATHFINDING_CONFIG_DEFAULT_ADJ_COST);
    public int nodeNumber = 0; // for debug
    public Vector2 nodePosition = new Vector2(0,0);
    public int nodeScoreAbs = 0;    // score it took to get here
    public int nodeScoreEst = 0;    // the estimate from how far to the goal
    public bool borderNode = false; // if this is a border node, extra cost may be added in
    public PathFindingNode nodeParent = null;

    // -------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------
    public PathFindingNode(Vector2 position, bool newBorderNode = false) : base(){
        this.init(position, newBorderNode);
    }

    // -------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------
    public void init(Vector2 position, bool newBorderNode = false){

        this.nodePosition = position;
        this.borderNode = newBorderNode;
        this.nodeScoreAbs = 0;
        this.nodeScoreEst = 0;
        this.nodeParent = null;
    }

    // -------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------
    public void setNodeParent(PathFindingNode parent){
        this.nodeParent = parent;
    }

    // -------------------------------------------------------------------------------------------------
    // estimate used here is the Manhattan Distance estimate, which is how many nodes need to move
    // horizontally and vertically to reach target node from current node
    // -------------------------------------------------------------------------------------------------
    public void setScoreEstimate(Vector2 endPosition){
        this.nodeScoreEst = (int)(Math.Abs(nodePosition.x - endPosition.x) + Math.Abs(nodePosition.y - endPosition.y));
        this.nodeScoreEst *= PF_COST_ADJACENT; // convert # tiles to cost
        if (this.borderNode){ this.nodeScoreEst += PFN_BORDER_COST; }
    }

    // -------------------------------------------------------------------------------------------------
    // abs score is the cost to reach this node
    // -------------------------------------------------------------------------------------------------
    public void setScoreAbs(int score){
        this.nodeScoreAbs = score;
        if (this.borderNode){ this.nodeScoreAbs += PFN_BORDER_COST; }
    }

    // -------------------------------------------------------------------------------------------------
    // combo of abs and est
    // -------------------------------------------------------------------------------------------------
    public int getNodeScore(){
        return this.nodeScoreAbs + this.nodeScoreEst;
    }
}