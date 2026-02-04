using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSphereScript : MonoBehaviour
{
    private Rigidbody sphereRigidBody;
    public float fuerza = 10f;
    public float movimientoVertical = 1f;
    
    // Start is called before the first frame update
    void Start()
    {
        this.sphereRigidBody = GetComponent<Rigidbody>();
        this.sphereRigidBody.freezeRotation = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
      if(Input.GetKey(KeyCode.W)){
    	 this.sphereRigidBody.AddForce((transform.forward * movimientoVertical) * fuerza);
      }
      else if(Input.GetKey(KeyCode.S)){
    	 this.sphereRigidBody.AddForce((transform.forward * -movimientoVertical) * fuerza);
      }
      else if(Input.GetKey(KeyCode.A)){
    	 this.sphereRigidBody.AddForce((transform.right * -movimientoVertical) * fuerza);
      }
      else if(Input.GetKey(KeyCode.D)){
    	 this.sphereRigidBody.AddForce((transform.right * movimientoVertical) * fuerza);
      }
      else{
         Debug.Log("No-Key pressed");
       }
    }
}
