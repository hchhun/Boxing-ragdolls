using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SyncPhysicsObject : MonoBehaviour
{
    Rigidbody rigidbody3D;
    ConfigurableJoint joint;

    [SerializeField]
    Rigidbody animatedRigidBody3D;

    [SerializeField]
    bool syncAnimation = false;

    //Keep track of starting rotation
    Quaternion startLocalRotation;

    void Awake() 
    {
        rigidbody3D = GetComponent<Rigidbody>();
        joint = GetComponent<ConfigurableJoint>();
        startLocalRotation = transform.localRotation;
    }

    public void UpdateJointFromAnimation() 
    {
        if (!syncAnimation) return;

        ConfigurableJointExtensions.SetTargetRotationLocal(joint, animatedRigidBody3D.transform.localRotation, startLocalRotation);
    }
}
