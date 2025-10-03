using UnityEngine;
using FishNet.Object;
using UnityEngine.Animations.Rigging;
using System.Collections;

public class PlayerLiftController : NetworkBehaviour
{
    public GameObject handTransform;
    private LiftableObject heldObject;

    private CameraSwitch cameraSwitcher;
    private Collider playerCollider;
    private RigBuilder rigbuilder;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform rightHandGripTarget;
    [SerializeField] private Transform leftHandGripTarget;

    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask objectLayer;
    [SerializeField] private float reachDuration = 0.3f;

    private Coroutine ikCoroutine;

    public override void OnStartClient()
    {
        cameraSwitcher = gameObject.GetComponent<CameraSwitch>();
        playerCollider = gameObject.GetComponent<Collider>();
        rigbuilder = gameObject.GetComponent<RigBuilder>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
            {
                TryPickup();
            }
            else
            {
                Drop();
            }
        }
    }

    private void LateUpdate()
    {
        if (heldObject != null)
        {
            leftHandGripTarget.transform.position = heldObject.leftHandPosition.position;
            rightHandGripTarget.transform.position = heldObject.rightHandPosition.position;
        }
    }

    private void TryPickup()
    {
        Collider closestCollider = null;
        float minDistance = float.MaxValue;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange, objectLayer);

        foreach (Collider hit in hitColliders)
        {
            if (hit.gameObject == this.gameObject)
            {
                continue;
            }
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCollider = hit;
            }
        }

        if (closestCollider != null) 
        {
            LiftableObject obj = closestCollider.GetComponent<LiftableObject>();
            if (obj != null)
            {
                if (leftHandGripTarget != null && rightHandGripTarget != null)
                {
                    ServerNotifyPickup(obj.GetComponent<NetworkObject>().ObjectId);
                }
                
                heldObject = obj;
                if (heldObject.gameObject.name == "CameraObject")
                {
                    cameraSwitcher.canSwitchCamera = false;
                    cameraSwitcher.setToVisionCamera();
                }
            }
        }
    }

    private void Drop()
    {
        if (heldObject != null)
        {
            heldObject.DropServerRpc(heldObject.transform.position);
            EnableCollisionsWithHeldObject();
            heldObject = null;
            cameraSwitcher.canSwitchCamera = true;
            
            ServerNotifyDrop();
        }
    }

    [ServerRpc]
    private void ServerNotifyPickup(int objectId)
    {
        ObserversPlayReachAnimation(objectId);
    }

    [ServerRpc]
    private void ServerNotifyDrop()
    {
        ObserversResetIK();
    }

    [ObserversRpc]
    private void ObserversPlayReachAnimation(int objectId)
    {
        if (ikCoroutine != null)
        {
            StopCoroutine(ikCoroutine);
        }

        if (NetworkManager.ServerManager.Objects.Spawned.TryGetValue(objectId, out NetworkObject netObj))
        {
            LiftableObject obj = netObj.GetComponent<LiftableObject>();
            if (obj != null)
            {
                ikCoroutine = StartCoroutine(ReachForObject(obj));
            }
        }
    }

    [ObserversRpc]
    private void ObserversResetIK()
    {
        if (ikCoroutine != null)
        {
            StopCoroutine(ikCoroutine);
        }

        ikCoroutine = StartCoroutine(AnimateIKToTarget(0f));
    }

    private IEnumerator ReachForObject(LiftableObject obj)
    {
        float t = 0f;
        
        while (t < 1f)
        {
            t += Time.deltaTime / reachDuration;
            float currentWeight = Mathf.Lerp(0f, 1f, t);
            
            if (leftHandIK != null) leftHandIK.weight = currentWeight;
            if (rightHandIK != null) rightHandIK.weight = currentWeight;
            
            yield return null;
        }

        if (leftHandIK != null) leftHandIK.weight = 1f;
        if (rightHandIK != null) rightHandIK.weight = 1f;

        if (IsOwner)
        {
            obj.PickupServerRpc(Owner.ClientId, handTransform);
            DisableCollisionsWithHeldObject(obj);
        }
    }

    private IEnumerator AnimateIKToTarget(float targetWeight)
    {
        float startLeft = leftHandIK != null ? leftHandIK.weight : 0f;
        float startRight = rightHandIK != null ? rightHandIK.weight : 0f;
        float t = 0f;
        
        while (t < 1f)
        {
            t += Time.deltaTime / reachDuration;
            
            if (leftHandIK != null) leftHandIK.weight = Mathf.Lerp(startLeft, targetWeight, t);
            if (rightHandIK != null) rightHandIK.weight = Mathf.Lerp(startRight, targetWeight, t);
            
            yield return null;
        }

        if (leftHandIK != null) leftHandIK.weight = targetWeight;
        if (rightHandIK != null) rightHandIK.weight = targetWeight;
    }

    private void DisableCollisionsWithHeldObject(LiftableObject obj)
    {
        Physics.IgnoreCollision(obj.GetComponent<Collider>(), playerCollider, true);
    }

    private void EnableCollisionsWithHeldObject()
    {
        if (heldObject != null)
            Physics.IgnoreCollision(heldObject.GetComponent<Collider>(), playerCollider, false);
    }
}