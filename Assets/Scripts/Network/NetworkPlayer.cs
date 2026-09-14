using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkPlayer : MonoBehaviour
{   
    [SerializeField]
    Rigidbody rigidbody3D;

    [SerializeField]
    ConfigurableJoint mainJoint;

    [SerializeField]
    Animator animator;

    //Input
    Vector2 moveInputVector = Vector2.zero;
    bool isJumpButtonPressed = false;

    //Controller Settings
    float maxSpeed = 3;

    //States
    bool isGrounded = false;

    //Raycasts
    RaycastHit[] raycastHits = new RaycastHit[10];

    //Syncing of physics objects for animation
    SyncPhysicsObject[] syncPhysicsObjects;

    void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space)) isJumpButtonPressed = true;
    }

    void FixedUpdate() { 
        //Assume we are not grounded
        isGrounded = false;

        //Check if grounded
        int numberOfHits = Physics.SphereCastNonAlloc(rigidbody3D.position, 0.1f, transform.up * (-1), raycastHits, 0.5f);

        //Check for valid results
        for (int i = 0; i < numberOfHits; i++) {
            //Ignore self hits
             if(raycastHits[i].transform.root == transform) continue;

             isGrounded = true;
             break;
        }

        //Apply extra gravity to characters for less floaty fall
        if (!isGrounded) {
            rigidbody3D.AddForce(Vector3.down * 10);
        }

        float inputMagnitude = moveInputVector.magnitude;

        Vector3 localVelocityVsForward = transform.forward * Vector3.Dot(transform.forward, rigidbody3D.linearVelocity);
        float localForwardVelocity = localVelocityVsForward.magnitude;

        if (inputMagnitude != 0) {
            Quaternion desiredDirection = Quaternion.LookRotation(new Vector3(moveInputVector.x, 0, moveInputVector.y * -1), transform.up);

            //Rotate target towards direction
            mainJoint.targetRotation = Quaternion.RotateTowards(mainJoint.targetRotation, desiredDirection, Time.fixedDeltaTime * 300);

            if (localForwardVelocity < maxSpeed) {
                //Move character in direction it's facing
                rigidbody3D.AddForce(transform.forward * inputMagnitude * 30);
            }
        }

        if(isGrounded && isJumpButtonPressed) {
            rigidbody3D.AddForce(Vector3.up * 17, ForceMode.Impulse);
            isJumpButtonPressed = false;
        }

        animator.SetFloat("movementSpeed", localForwardVelocity * 0.4f);

        //Update joints rotation based on animations
        for (int i = 0; i < syncPhysicsObjects.Length; i++) {
            syncPhysicsObjects[i].UpdateJointFromAnimation();
        }
    }


}
