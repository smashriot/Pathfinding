// -------------------------------------------------------------------------------------------------
//  Fish.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;

public class Fish : MonoBehaviour {

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void OnCollisionEnter2D(Collision2D coll) {

        // fish hit cat OR wall (to avoid being stuck in walls)
        if (coll.gameObject.CompareTag(Constants.GAMEOBJECT_TAG_CAT) ||
            coll.gameObject.CompareTag(Constants.GAMEOBJECT_TAG_WALL) ){
            // remove fish since eaten
            Object.Destroy(this.gameObject);
        }
    }
}
