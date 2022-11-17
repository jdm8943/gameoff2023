using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Input : MonoBehaviour
{

    Vector2 vel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {

            vel = Vector2.up * vel;
            /*transform.rotation = transform.rotation * qPitchPlus;

            rotorSpinner.setSpin(RotorSpinner.LiftMode.HIGH);

            vel = Vector3.ProjectOnPlane(windSpeed * transform.up, Vector3.up);*/
        }
    }
}
