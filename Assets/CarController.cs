using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour {


    public AnimationCurve throttleFalloff;
    Rigidbody body;
    BoxCollider box;
    float maxSpeed = 50;
    float throttleAmount = 4000;
    float frictionMultiplier = 2;

    bool isGrounded = false;
    Vector3 raycastNormal = Vector3.zero;
    Vector3 contactNormal = Vector3.zero;
    float airTorqueMultiplier = 40;

    public Transform steering;

	void Start () {
        body = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();
	}
	
	void FixedUpdate ()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float t = Input.GetAxis("Throttle");

        TurnWheels(h);

        if (isGrounded)
        {
            Accelerate(t * Time.deltaTime * throttleAmount);
            Jump();
            ApplyWheelFriction();
        }
        else
        {
            RotatePitch(v * Time.deltaTime);
            if (Input.GetAxis("Slide") > .1f)
            {
                RotateRoll(-h * Time.deltaTime);
            }
            else
            {
                RotateYaw(h * Time.deltaTime);
            }
        }

        Raycast();
        RollToSurface();

    }

    private void TurnWheels(float h)
    {
        //steering.Rotate(0, 10 * h * Time.deltaTime, 0);
        
        Quaternion b = Quaternion.Euler(0, h * 30, 0);
        steering.localRotation = Quaternion.Slerp(steering.localRotation, b, Time.deltaTime * 10);
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            body.velocity += Vector3.up * 10;
        }
    }
    void Accelerate(float amount)
    {

        float mag = body.velocity.magnitude;
        float percent = mag / maxSpeed;
        percent = Mathf.Clamp(percent, 0, 1);
        float multiplier = throttleFalloff.Evaluate(percent);
        body.AddRelativeForce(steering.forward * amount * multiplier);
        transform.rotation = Quaternion.Slerp(transform.rotation, steering.rotation, amount * multiplier * .1f);
    }
    void Steer()
    {
        float h = Input.GetAxis("Horizontal");

    }
    void RotateYaw(float amount)
    {
        body.AddRelativeTorque(Vector3.up * amount * airTorqueMultiplier);
    }
    void RotatePitch(float amount)
    {
        body.AddRelativeTorque(Vector3.right * amount * airTorqueMultiplier);
    }
    void RotateRoll(float amount)
    {
        body.AddRelativeTorque(Vector3.forward * amount * airTorqueMultiplier);
    }
    void OnCollisionStay(Collision collisionInfo)
    {
        Vector3 avg = Vector3.zero;
        foreach (ContactPoint pt in collisionInfo.contacts)
        {
            Debug.DrawRay(pt.point, pt.normal * .3f);
            avg += pt.normal;
        }
        avg /= collisionInfo.contacts.Length;
        Debug.DrawLine(transform.position, transform.position + avg, Color.red);
        contactNormal = avg;
    }
    void ApplyWheelFriction()
    {
        
        float ratio = Vector3.Dot(body.velocity, transform.forward);
        float friction = ratio;
        body.AddForce(-body.velocity * friction * Time.deltaTime * frictionMultiplier);
        // ratio = sidespeed / (sidespeed + forwardspeed)
        // slidefriction = curve(ratio)
        // groundfriction = curve(groundnormal.z)
        // friction = slidefriction * groundfriction
        // impulse = constraint * friction
    }
    void Raycast()
    {
        Vector3 min = -box.size/2;
        Vector3 max =  box.size/2;
        float skinWidth = .1f;
        Vector3 pt1 = transform.TransformPoint(new Vector3(min.x, min.y+skinWidth, min.z));
        Vector3 pt2 = transform.TransformPoint(new Vector3(min.x, min.y+skinWidth, max.z));
        Vector3 pt3 = transform.TransformPoint(new Vector3(max.x, min.y+skinWidth, min.z));
        Vector3 pt4 = transform.TransformPoint(new Vector3(max.x, min.y+skinWidth, max.z));

        Vector3 dir = -transform.up;

        Debug.DrawRay(pt1, dir);
        Debug.DrawRay(pt2, dir);
        Debug.DrawRay(pt3, dir);
        Debug.DrawRay(pt4, dir);

        RaycastHit hit1;
        RaycastHit hit2;
        RaycastHit hit3;
        RaycastHit hit4;
        float disToGround = 1f;

        if (Physics.Raycast(pt1, dir, out hit1, disToGround)
         && Physics.Raycast(pt2, dir, out hit2, disToGround)
         && Physics.Raycast(pt3, dir, out hit3, disToGround)
         && Physics.Raycast(pt4, dir, out hit4, disToGround))
        {
            isGrounded = true;
            raycastNormal = (hit1.normal + hit2.normal + hit3.normal + hit4.normal)/4;
        } else
        {
            isGrounded = false;
            //print("derp?");
        }
    }
    void RollToSurface()
    {
        // get normal contact points
        // add a torque to roll so that the vehicle's up direction aligns with surface normals
        if (isGrounded)
        {
            Vector3 res = Vector3.Cross(transform.up, raycastNormal);
            body.AddTorque(res * Time.deltaTime * 50, ForceMode.Force);
            //Debug.DrawLine(transform.position, transform.position + normalAverage, Color.red);
        }
        else if(contactNormal.sqrMagnitude > 0)
        {
            Vector3 res = Vector3.Cross(transform.up, contactNormal);
            body.AddTorque(res * Time.deltaTime * 5000, ForceMode.Force);
            contactNormal = Vector3.zero;
        }
    }
}
