using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{   
    public static NetworkPlayer Local {get; set;}

    [SerializeField]
    Rigidbody rigidbody3D;

    [SerializeField]
    NetworkRigidbody networkRigidBody3D;

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

    [Networked, Capacity(10)] public NetworkArray<Quaternion> networkPhysicsSyncedRotations { get; }

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

    public override void FixedUpdateNetwork() { 
        Vector3 localVelocityVsForward = Vector3.zero;
        float localForwardVelocity = 0;

        if (Object.HasStateAuthority)
        {
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

            localVelocityVsForward = transform.forward * Vector3.Dot(transform.forward, rigidbody3D.linearVelocity);
            localForwardVelocity = localVelocityVsForward.magnitude;

            if (transform.position.y < -10) {
                networkRigidBody3D.Teleport(new Vector3(0f, 5f, 0f), Quaternion.identity);
            }
            
        }


        if (GetInput(out NetworkInputData networkInputData)) {
            float inputMagnitude = networkInputData.movementInput.magnitude;

            if (inputMagnitude != 0) {
                Quaternion desiredDirection = Quaternion.LookRotation(new Vector3(networkInputData.movementInput.x, 0, networkInputData.movementInput.y * -1), transform.up);

                //Rotate target towards direction
                mainJoint.targetRotation = Quaternion.RotateTowards(mainJoint.targetRotation, desiredDirection, Runner.DeltaTime * 300);

                if (localForwardVelocity < maxSpeed) {
                    //Move character in direction it's facing
                    rigidbody3D.AddForce(transform.forward * inputMagnitude * 30);
                }
            }

            if(isGrounded && networkInputData.isJumpPressed) {
                rigidbody3D.AddForce(Vector3.up * 17, ForceMode.Impulse);
                networkInputData.isJumpPressed = false;
                // isJumpButtonPressed = false;
            }

        }

        

        if (Object.HasStateAuthority) {
            
            animator.SetFloat("movementSpeed", localForwardVelocity * 0.4f);

            //Update joints rotation based on animations
            for (int i = 0; i < syncPhysicsObjects.Length; i++) {
                syncPhysicsObjects[i].UpdateJointFromAnimation();
                networkPhysicsSyncedRotations.Set(i, syncPhysicsObjects[i].transform.localRotation);

            }


        //     //CAN PROBS CODE FALL DEATH HERE
        //     // if (transform.position.y < -10) {
        //     //     networkRigidBody3D.Teleport(new Vector3(0f, 5f, 0f), Quaternion.identity);
        //     // }
        }

        // if (transform.position.y < -10) {
        //         networkRigidBody3D.Teleport(new Vector3(0f, 5f, 0f), Quaternion.identity);
                
        // }
    }

    public override void Render() 
    {   
        if (!Object.HasStateAuthority) {
            var interpolated = new NetworkBehaviourBufferInterpolator(this);

            for (int i = 0; i < syncPhysicsObjects.Length; i++) {
                syncPhysicsObjects[i].transform.localRotation = Quaternion.Slerp(syncPhysicsObjects[i].transform.localRotation, networkPhysicsSyncedRotations.Get(i), interpolated.Alpha);
            }
        }
    }

    public NetworkInputData GetNetworkInput() 
    {
        NetworkInputData networkInputData = new NetworkInputData();

        //Move data
        networkInputData.movementInput = moveInputVector;

        if (isJumpButtonPressed) {
            networkInputData.isJumpPressed = true;
        }

        //Reset jump button
        isJumpButtonPressed = false;
        // networkInputData.isJumpPressed = false;

        return networkInputData;
    }

    public override void Spawned() 
    {   
        if (Object.HasInputAuthority)
        {
            Local = this;
            Utils.DebugLog("Spawned player with input authority");
        }
        else Utils.DebugLog("Spawned player without input authority");

        if (!Object.HasStateAuthority)
        {
            foreach (var sync in syncPhysicsObjects)
            {
                var rb = sync.GetComponent<Rigidbody>();
                if (rb == null || rb == rigidbody3D) continue;   // leave the root alone

                rb.isKinematic = true;
            }   
        }
    }

    public void PlayerLeft(PlayerRef player) 
    {

    }


}
