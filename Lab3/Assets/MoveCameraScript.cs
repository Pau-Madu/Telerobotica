using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraStabilizer : MonoBehaviour
{
    public Transform target;
    public Vector3 posicionRelativa = new Vector3(0f, 2f, -5f);
    public Vector3 rotacionFija = new Vector3(25f, 0f, 0f);

    void LateUpdate()
    {
        if(target != null){
            transform.position = target.position + posicionRelativa;
            transform.rotation = Quaternion.Euler(rotacionFija);
        }
    }
}

