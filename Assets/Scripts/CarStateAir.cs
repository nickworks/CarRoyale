using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarStateAir : CarState {

    public override CarState Update()
    {
        Rotate();
        return null;
    }

    void Rotate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool isRolling = Input.GetAxis("Slide") > .1f;

        float mult = controller.airTorqueMultiplier * Time.deltaTime;

        controller.body.AddRelativeTorque(Vector3.right * v * mult); // pitch
        if ( isRolling) controller.body.AddRelativeTorque(Vector3.forward * h * mult); // roll
        if (!isRolling) controller.body.AddRelativeTorque(Vector3.up * h * mult); // yaw
    }

}
