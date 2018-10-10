using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarStateGround : CarState {

    override public CarState Update()
    {
        float t = Input.GetAxis("Throttle");
        float throttle = controller.GetThrottleFalloff(t < 0);

        controller.Accelerate(throttle * t * Time.deltaTime);
        controller.Jump();

        if (!controller.turnOffFriction)
        {
            controller.ApplyWheelFriction();
        }

        // PUT TRANSITIONS HERE!!!

        // if (condition is true) return new CarStateAir();

        return null;
    }
}
