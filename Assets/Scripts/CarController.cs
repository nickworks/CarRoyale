using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour {


    public AnimationCurve throttleFalloff;
    Rigidbody body;
    BoxCollider box;
    public float maxSpeed = 1;
    public float throttleAmount = 32000;
    public AnimationCurve frictionFalloff;

    bool isGrounded = false;
    Vector3 raycastNormal = Vector3.zero;
    Vector3 contactNormal = Vector3.zero;
    float airTorqueMultiplier = 40;

    float turnAmt = 0;

    public Transform steering;

    public float wheelMaxTurn = 20;
    public Vector2 wheelSpacing = new Vector2(.4f, .75f);
    public Transform wheel1;
    public Transform wheel2;
    public Transform wheel3;
    public Transform wheel4;
    public bool turnOffFriction = false;


    private CarState carState;

    void OnValidate()
    {
        Vector2 w = wheelSpacing;
        wheel1.localPosition = new Vector3(-w.x, 0, w.y);
        wheel2.localPosition = new Vector3( w.x, 0, w.y);
        wheel3.localPosition = new Vector3(-w.x, 0, -w.y);
        wheel4.localPosition = new Vector3( w.x, 0, -w.y);
    }

    void Start () {
        SwitchCarState(new CarStateGround());

        body = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();
	}
	
	void FixedUpdate ()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float t = Input.GetAxis("Throttle");

        if (carState != null)
        {
            CarState newState = carState.Update();
            SwitchCarState(newState);
        }

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
            if (!turnOffFriction)
            {
                Vector3 stoppingForce = -body.velocity / 4;
                Vector3 localAngularVelocity = transform.InverseTransformVector(body.angularVelocity);
                float stoppingTorque = localAngularVelocity.y / 4;

                ApplyWheelFriction(wheel1, stoppingForce, stoppingTorque);
                ApplyWheelFriction(wheel2, stoppingForce, stoppingTorque);
                ApplyWheelFriction(wheel3, stoppingForce, stoppingTorque);
                ApplyWheelFriction(wheel4, stoppingForce, stoppingTorque);
            }
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

    private void SwitchCarState(CarState newState)
    {
        if (newState == null) return;
        if (carState != null) carState.OnEnd();
        carState = newState;
        carState.OnBegin(this);
    }

    private void TurnWheels(float h)
    {
        //steering.Rotate(0, 10 * h * Time.deltaTime, 0);
        turnAmt = h * wheelMaxTurn;
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
    void ApplyWheelFriction(Transform tire, Vector3 stoppingForce, float stoppingTorque)
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
        float slidefriction = frictionFalloff.Evaluate(ratio);

        // NO SLIPPING DOWN SLOPES:
        // groundfriction = curve(groundnormal.z)
        // friction = slidefriction * groundfriction
        float friction = slidefriction;
        // impulse = constant * friction
        // apply to "wheel"
        // apply friction force aligned with center of mass

        // find direction to apply torque-force:
        Vector3 center = transform.position + body.centerOfMass;
        Vector3 torqueDir = Vector3.Cross(transform.up, (center - tire.position).normalized);

        // add stopping force and stopping torque-force:
        Vector3 combinedForces = (stoppingTorque * torqueDir + stoppingForce);

        // try to stop spinning:
        body.AddForceAtPosition(combinedForces * friction * Time.deltaTime, tire.position, ForceMode.Impulse);

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
