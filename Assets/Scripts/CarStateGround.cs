using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarStateGround : CarState {

    override public CarState Update()
    {
        Debug.Log("tock");

        // PUT BEHAVIOR HERE!!!

        // PUT TRANSITIONS HERE!!!

        // if (condition is true) return new CarStateAir();

        return null;
    }
}
