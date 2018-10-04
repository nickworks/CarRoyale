using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public Transform target;

    public float sensitivityVertical = 100;
    public float sensitivityHorizontal = 100;

    public bool invertLookY = true;
    public bool invertLookX = false;

    float yaw = 0;
    float pitch = 15;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void LateUpdate () {

        float lookH = Input.GetAxis("LookHorizontal");
        float lookV = Input.GetAxis("LookVertical");

        pitch += lookV * Time.deltaTime * sensitivityVertical * (invertLookY ? -1 : 1);
        yaw += lookH * Time.deltaTime * sensitivityHorizontal * (invertLookX ? -1 : 1);

        yaw *= .95f;

        if (target) ChaseTarget();

    }
    void ChaseTarget()
    {
        //transform.position += (target.position - transform.position) * .5f;
        transform.position = target.position;
        transform.eulerAngles = new Vector3(pitch, yaw + target.eulerAngles.y, 0);
    }
}
