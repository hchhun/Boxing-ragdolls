using UnityEngine;

public class HandGrabHandler : MonoBehaviour
{
    [SerializeField]
    Animator animator;
    
    // Created on the fly
    FixedJoint fixedJoint;

    Rigidbody rigidbody3D;

    NetworkPlayer networkPlayer;

    void Awake() 
    {
        networkPlayer = transform.root.GetComponent<NetworkPlayer>();
        rigidbody3D = GetComponent<Rigidbody>();

        // Change solver iterations to prevent joints from flexing too much. for hands
        rigidbody3D.solverIterations = 255;
    }

    public void UpdateState()
    {
        if (networkPlayer.isGrabbingActive) {
            animator.SetBool("isGrabbing", true);
        }
        else {
            // Check if still carrying something
            if (fixedJoint != null) {
                if (fixedJoint.connectedBody != null) {
                    float forceAmountMultiplier = 0.1f;

                    // Check if grabbing on to another player
                    if (fixedJoint.connectedBody.transform.root.TryGetComponent(out NetworkPlayer otherPlayerNetworkPlayer)) {
                        // check if active if implemented here

                        forceAmountMultiplier = 15;
                    }
                    // Toss object before destroying joint
                    fixedJoint.connectedBody.AddForce((networkPlayer.transform.forward + Vector3.up * 0.25f) * forceAmountMultiplier, ForceMode.Impulse);
                }
            }

            Destroy(fixedJoint);

            animator.SetBool("isCarrying", false);
            animator.SetBool("isGrabbing", false);
        }

        // animator.SetBool("isCarrying", false);
        // animator.SetBool("isGrabbing", false);
    }

    bool TryCarryObject(Collision collision)
    {
        if (!networkPlayer.Object.HasStateAuthority) return false;

        // need to add knocked out condition if implemented

        if (!networkPlayer.isGrabbingActive) return false;

        // Check if we are already carrying an object
        if (fixedJoint != null) return false;

        // Avoid grabbing ownself
        if (collision.transform.root == networkPlayer.transform) return false;

        // Only rigidbodies can be carried
        if (!collision.collider.TryGetComponent(out Rigidbody otherObjectRigidbody)) return false;

        fixedJoint = transform.gameObject.AddComponent<FixedJoint>();

        fixedJoint.connectedBody = otherObjectRigidbody;

        // Take care of anchor point manually
        fixedJoint.autoConfigureConnectedAnchor = false;

        // Transform collision point from world to local space
        fixedJoint.connectedAnchor = collision.transform.InverseTransformPoint(collision.GetContact(0).point);

        // Set animator to carrying
        animator.SetBool("isCarrying", true);
        Utils.DebugLog("stuck"+ collision.gameObject.name);

        return true;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Attempt to carry object
        TryCarryObject(collision);
    }
}
