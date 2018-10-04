using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debuggy : MonoBehaviour {

    public Vector3 a;
    public Vector3 b;

    void Start () {
		
	}
	
	void Update () {
		
	}

    void OnDrawGizmos()
    {
        Vector3 p = transform.position;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(p, p+a);
        Gizmos.DrawLine(p, p+b);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(p, p+Vector3.Cross(a, b));
        Gizmos.DrawLine(p, p+Vector3.Cross(b, a));

    }
}
