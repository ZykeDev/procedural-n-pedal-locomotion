using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField] private List<ConstraintController> limbs;

    [SerializeField] private bool useZigzagMotion = true;
    private float zigzagDifference = 1f;

    private int groundMask;
    private Vector3 groundTarget;



    void Awake()
    {
        groundMask = LayerMask.GetMask("Ground");

        limbs = new List<ConstraintController>(GetComponentsInChildren<ConstraintController>());

        // Find local forward vector 
        
    }

    void Start()
    {
        if (useZigzagMotion)
        {
            // FR
            // FL, BL
            // BR
            // ...

            // TODO order them FL, FR, BL, BR

            for (int i = 0; i < limbs.Count; i++)
            {
                if (i % 2 != 0) limbs[i].ForwardTarget(zigzagDifference);
            }
        }
    }



    void Update()
    {
        // Set the entity's height based on the limb tips.
        // Do we update this only after a limb has reached its target?
        UpdateGait();

        // Rotate body based on weigthed limb vectors
        

    }






    private void UpdateGait()
    {

        // Basically, find the target normal of the plane passing from all limb coordinates
        // A plane will always interesct 3 points, but what about the other limbs?
        // We can:
        // Wait until 3 points are disaligned, then create a plane passing through them, and realign the rest n-3.
        // or
        // Always give priority to the top-3 most dialigned points, find the plane, and realign the rest n-3.
        // or
        // Force the distance vector between the body and the ground to never change.
        /*
        List<Vector3> tipCoords = new List<Vector3>();

        for (int i = 0; i < limbs.Count; i++)
        {
            tipCoords.Add(limbs[i].TipTransform.position);
        }
        */

        // Force the distance vector between the body and the ground to never change
        float elevation = 0.5f;
        
        // Update the distance to the ground below
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, Mathf.Infinity, groundMask))
        {
            groundTarget = hit.point;

            Vector3 newPos = new Vector3(transform.position.x, hit.point.y + elevation, transform.position.z);
            transform.position = newPos;
        }



        // Update the rotation to be parallel to the ground below
        if (Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity, groundMask))
        {
            groundTarget = hit.point;

            // If there is a difference in rotation
            if (hit.normal != transform.up)
            {
                Quaternion fromRotation = transform.rotation;
                Quaternion toRotation = Quaternion.FromToRotation(transform.up, hit.normal);

                print(fromRotation + " -> " + toRotation);

                transform.rotation = Quaternion.Slerp(fromRotation, toRotation, Time.deltaTime * 10);
            }
        }       
    }








    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, groundTarget);
    }

}

/*
var adjustSpeed : float = 1;
private var fromRotation : Quaternion;
private var toRotation : Quaternion;
private var targetNormal : Vector3; (up)
private var hit : raycastHit;
private var weight : float = 1;



function FixedUpdate()
{
    if (Physics.Raycast(transform.position, -Vector3.up, hit))
    {
        if (hit.distance > .9) rigidbody.AddForce(-Vector3.up  350000);
        if (hit.normal == transform.up) return;
        if (hit.normal != targetNormal)
        {
            targetNormal = hit.normal;
            fromRotation = transform.rotation;
            torotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            weight = 0;
        }
        if (weight <= 1)
        {
            weight += Time.deltaTime  adjustSpeed;
            tranform.rotation = Quaternion.Slerp(fromRotation, toRotation, weight);
            //Or to smooth the weight
            //tranform.rotation = Quaternion.Slerp(fromRotation, toRotation,
            //                                        Mathf.SmoothStep(weight));
        }
    }/*/