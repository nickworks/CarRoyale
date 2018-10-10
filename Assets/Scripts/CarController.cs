using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour {


    public AnimationCurve throttleFalloff;
    public Rigidbody body { get; private set; }
    public BoxCollider box { get; private set; }
    public float maxSpeed = 1;
    public float throttleAmount = 3000;
    public AnimationCurve frictionFalloff;

    bool isGrounded = false;
    Vector3 raycastNormal = Vector3.zero;
    Vector3 contactNormal = Vector3.zero;
    public float airTorqueMultiplier = 40;

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

        TurnWheels();

        if (carState != null)
        {
            CarState newState = carState.Update();
            SwitchCarState(newState);
        }
        
        Raycast();
        //RollToSurface();
    }

    private void SwitchCarState(CarState newState)
    {
        if (newState == null) return;
        if (carState != null) carState.OnEnd();
        carState = newState;
        carState.OnBegin(this);
    }

    private void TurnWheels()
    {
        float h = Input.GetAxis("Horizontal");
        turnAmt = h * wheelMaxTurn;
        Quaternion b = Quaternion.Euler(0, turnAmt, 0);
        steering.localRotation = Quaternion.Slerp(steering.localRotation, b, Time.deltaTime * 10);
        wheel2.localRotation = wheel1.localRotation = steering.localRotation;
    }

    public void Jump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            body.velocity += Vector3.up * 10;
        }
    }
    public void Accelerate(float forceAmount)
    {
        Accelerate(wheel1, forceAmount);
        Accelerate(wheel2, forceAmount);
        Accelerate(wheel3, forceAmount);
        Accelerate(wheel4, forceAmount);
    }
    public void Accelerate(Transform tire, float forceAmount)
    {
        body.AddForceAtPosition(tire.forward * forceAmount, tire.position, ForceMode.Acceleration);
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
    public void ApplyWheelFriction()
    {
        ApplyWheelFriction(wheel1);
        ApplyWheelFriction(wheel2);
        ApplyWheelFriction(wheel3);
        ApplyWheelFriction(wheel4);
    }
    void ApplyWheelFriction(Transform tire)
    {
        // NO LONGITUDINAL FRICTION

        // LATERAL FRICTION:
        // look at speed of tire (in world space?)
        Vector3 localVelocity = tire.InverseTransformDirection(body.velocity);        
        // ratio = sidespeed / (sidespeed + forwardspeed)
        float bottom = Mathf.Abs(localVelocity.x) + Mathf.Abs(localVelocity.z);
        if (bottom == 0) return;

        float ratio = Mathf.Abs(localVelocity.x / bottom);
        // (0 means the wheel is going forward, 1 means its going sideways)
        // slidefriction = curve(ratio) (values: 1 -> .2 | 0 -> 1)
        float slideFriction = frictionFalloff.Evaluate(ratio);

        // NO SLIPPING DOWN SLOPES:
        // groundfriction = curve(groundnormal.z)
        // friction = slidefriction * groundfriction
        float friction = slideFriction;// * slideFriction;
        // impulse = constant * friction
        // apply to "wheel"
        // apply friction force aligned with center of mass

        Vector3 worldVelocityOfTire = body.GetPointVelocity(tire.position);
        Vector3 impulse = -worldVelocityOfTire * friction;
        // try to stop spinning:
        body.AddForceAtPosition(impulse, tire.position, ForceMode.Acceleration);
        
        Debug.DrawRay(tire.position, impulse, Color.red);
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
    public float GetThrottleFalloff(bool backwards = false)
    {
        float speed = body.velocity.magnitude * Vector3.Dot(body.velocity.normalized, transform.forward); // get forward speed (what about backward?)
        if (backwards) speed = -speed;
        float percent = Mathf.Clamp(speed / maxSpeed, 0, 1);
        float throttle = throttleFalloff.Evaluate(percent) * throttleAmount;
        return throttle;
    }
}
