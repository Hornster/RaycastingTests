using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed;
    [SerializeField]
    private float movementSpeed;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float timeElapsed = Time.deltaTime;
        float rotation = GetRotationInput();

        gameObject.transform.Rotate(new Vector3(0.0f, 0.0f, 1.0f), rotation*timeElapsed);

        gameObject.transform.localPosition = GetMovingInput()*timeElapsed + gameObject.transform.localPosition;
    }

    private Vector3 GetMovingInput()
    {
        Vector3 movement = new Vector3();
        movement.x = Input.GetAxisRaw("Horizontal") * movementSpeed;
        movement.y = Input.GetAxisRaw("Vertical") * movementSpeed;

        return movement;
    }

    private float GetRotationInput()
    {
        var rotation = 0.0f;
        if (Input.GetButton("q"))
        {
            rotation = rotationSpeed;
        }
        else if (Input.GetButton("e"))
        {
            rotation = -rotationSpeed;
        }


        return rotation;
    }
}
