using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CarState {

    protected CarController controller;

    abstract public CarState Update();
    virtual public void OnBegin(CarController controller)
    {
        this.controller = controller;
    }
    virtual public void OnEnd() { }

}
