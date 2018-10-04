using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour {


    public AnimationCurve throttleFalloff;
    Rigidbody body;
    BoxCollider box;
    float maxSpeed = 1;
    float throttleAmount = 16000;
    float frictionMultiplier = 2;

    bool isGrounded = false;
    Vector3 raycastNormal = Vector3.zero;
    Vector3 contactNormal = Vector3.zero;
    float airTorqueMultiplier = 40;

    float turnAmt = 0;

    public Transform steering;

    public Vector2 wheelSpacing = new Vector2(.4f, .75f);
    public Transform wheel1;
    public Transform wheel2;
    public Transform wheel3;
    public Transform wheel4;

    void OnValidate()
    {
        Vector2 w = wheelSpacing;
        wheel1.localPosition = new Vector3(-w.x, 0, w.y);
        wheel2.localPosition = new Vector3( w.x, 0, w.y);
        wheel3.localPosition = new Vector3(-w.x, 0, -w.y);
        wheel4.localPosition = new Vector3( w.x, 0, -w.y);
    }

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

            float percent = body.velocity.magnitude / maxSpeed;
            percent = Mathf.Clamp(percent, 0, 1);
            float throttle = throttleFalloff.Evaluate(percent);
            throttle *= t * throttleAmount * Time.deltaTime;

            Accelerate(wheel1, t);
            Accelerate(wheel2, t);
            Accelerate(wheel3, t);
            Accelerate(wheel4, t);
            Jump();
            
            ApplyWheelFriction(wheel1);
            ApplyWheelFriction(wheel2);
            ApplyWheelFriction(wheel3);
            ApplyWheelFriction(wheel4);
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
        turnAmt = h * 20;
        Quaternion b = Quaternion.Euler(0, turnAmt, 0);
        steering.localRotation = Quaternion.Slerp(steering.localRotation, b, Time.deltaTime * 10);
        wheel2.localRotation = wheel1.localRotation = steering.localRotation;
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            body.velocity += Vector3.up * 10;
        }
    }
    void Accelerate(Transform tire, float throttle)
    {
        body.AddForceAtPosition(tire.forward * throttle, tire.position, ForceMode.Acceleration);
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
    void ApplyWheelFriction(Transform tire)
    {
        // NO LONGITUDINAL FRICTION

        // LATERAL FRICTION:

        Vector3 localVelocity = tire.InverseTransformDirection(body.velocity);

        // look at speed of tire (in world space?)
        // ratio = sidespeed / (sidespeed + forwardspeed)
        float bottom = (localVelocity.x + localVelocity.z);
        if (bottom == 0) return;

        float ratio = localVelocity.x / bottom;
        // (0 means the wheel is going forward, 1 means its going sideways)
        // slidefriction = curve(ratio) (values: 1 -> .2 | 0 -> 1)
        float slidefriction = Mathf.Lerp(1, .2f, ratio);

        // NO SLIPPING DOWN SLOPES:
        // groundfriction = curve(groundnormal.z)
        // friction = slidefriction * groundfriction
        float friction = slidefriction;
        // impulse = constant * friction
        // apply to "wheel"
        // apply friction force aligned with center of mass

        Vector3 force = -body.velocity / 4; // FIXME: find forces necessary to bring car to a stop (position and rotational velocity)
        

        body.AddForceAtPosition(force * friction * Time.deltaTime, tire.position, ForceMode.Acceleration);

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
