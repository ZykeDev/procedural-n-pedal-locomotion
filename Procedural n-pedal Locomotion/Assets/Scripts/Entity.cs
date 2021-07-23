using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class Entity : MonoBehaviour
{
    [SerializeField] private GameObject body;
    
    [SerializeField, Range(0.1f, 50f), Tooltip("Speed at which to realign the entity's body when waling on slopes.")] 
    private float realignmentSpeed = 25f;

    [SerializeField, Range(0.01f, 1f), Tooltip("Min height difference above which to start rotating the body.")]
    private float realignmentThreshold = 0.1f;

    [SerializeField] private bool useZigzagMotion = true;
    private float zigzagDifference = 1f;

    [SerializeField, Tooltip("Automatically add a Capsule Collider to each bone.")] 
    private bool generateBoneColliders = true;

    private List<ConstraintController> limbs;
    public bool IsUpdatingGait { get; private set; }
    public bool IsRotating { get; private set; }
    private int groundMask;
    private RaycastHit groundHit;

    public Vector3 CenterOfMass { get; private set; }

    void Awake()
    {
        groundMask = LayerMask.GetMask("Ground");

        limbs = new List<ConstraintController>(GetComponentsInChildren<ConstraintController>());

        // Find the center of mass
        CenterOfMass = ComputeCenterOfMass();
    }

    void Start()
    {
        if (useZigzagMotion)
        {
            for (int i = 0; i < limbs.Count; i++)
            {
                if (i % 2 != 0) limbs[i].ForwardTarget(zigzagDifference);
            }
        }

        if (generateBoneColliders)
        {
            GenerateBoneColliders(); 
        }
    }



    void FixedUpdate()
    {
        // Update the center of mass
        UpdateCenterOfMass();

        // Set the entity's height based on the limb tips.
        // Do we update this only after a limb has reached its target?
        UpdateGait();
    }




    private void UpdateGait()
    {
        Vector3 direction = transform.TransformDirection(Vector3.down);

        if (Physics.Raycast(CenterOfMass, direction, out groundHit, Mathf.Infinity, groundMask))
        {
            //Debug.DrawRay(CenterOfMass, direction * groundHit.distance, Color.yellow);

            bool isRotEnough = true;

            if (!AreLegsMoving())
            {
                // Rotate to match the limb positions
                Quaternion targetRot = FindRotation();

                // Only rotate if there is enough of a difference in rotations
                isRotEnough = Mathf.Abs(Quaternion.Angle(body.transform.rotation, targetRot)) <= 1.1f;
                if (!isRotEnough)
                {
                    body.transform.rotation = Quaternion.Lerp(body.transform.rotation, targetRot, Time.deltaTime * realignmentSpeed);
                }
                else
                {
                    body.transform.rotation = targetRot;
                }
            }



            // If there is a difference in height AND its not already rotating
            if (transform.position.y != groundHit.point.y && isRotEnough)
            {
                // Update the height
                Vector3 targetPos = new Vector3(transform.position.x, groundHit.point.y, transform.position.z);

                // Only update it if there is enough of a difference
                bool isPosEnough = Mathf.Abs((transform.position - targetPos).magnitude) <= 0.1f;
                if (!isPosEnough)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * realignmentSpeed);
                }
                else
                {
                    transform.position = targetPos;
                }

            }
        }
    }




    private void UpdateCenterOfMass()
    {
        CenterOfMass = ComputeCenterOfMass();
    }

    private Vector3 ComputeCenterOfMass()
    {
        Vector3 com = transform.position + Vector3.up * 1f;

        return com;
    }


    /// <summary>
    /// Finds the new rotation values along the X and Z axis to remain stable, based on the relative positons of the limbs
    /// </summary>
    /// <returns></returns>
    private Quaternion FindRotation()
    {
        List<float> angles = new List<float>();
        int rotXDirection, rotZDirection;                       // Signs of rotation

        // Find the rotation along X
        float rotX;
        {
            // Find the angle differences between different limbs
            for (int i = 0; i < limbs.Count - 2; i++)
            {
                Vector3 a = limbs[i].transform.position;        // Pos of the first limb tip
                Vector3 b = limbs[i + 2].transform.position;    // Pos of the second limb tip
                Vector3 c;                                      // Pos C to make a right triangle ACB

                // Skip the calculation if the limbs are (almost) at the same height
                if (Mathf.Abs(a.y - b.y) <= realignmentThreshold)
                {
                    angles.Add(0);
                    continue;
                }

                // Make sure C is parallel to the lower point
                if (a.y > b.y)
                {
                    c = new Vector3(a.x, b.y, a.z);
                    rotXDirection = -1;
                }
                else
                {
                    c = new Vector3(b.x, a.y, b.z);
                    rotXDirection = 1;
                }


                // Get the triangle sides
                float hypotenuse = Vector3.Distance(a, b);
                float opposite = Vector3.Distance(a, c);
                float adjacent = Vector3.Distance(b, c);

                // Find both angles and use the smalles one (acute)
                float theta = Mathf.Asin(opposite / hypotenuse) * Mathf.Rad2Deg;
                float gamma = Mathf.Asin(adjacent / hypotenuse) * Mathf.Rad2Deg;

                // Find the lesser angle between the two
                float angle = theta <= gamma ? theta : gamma;

                // Adjust the rotation to match the body
                angle *= rotXDirection;

                angles.Add(angle);
            }

            // Average out the angles
            float angleSum = 0;
            for (int i = 0; i < angles.Count; i++)
            {
                angleSum += angles[i];
            }

            rotX = angleSum / angles.Count;
        }

        // Find the rotation along Z
        float rotZ;
        {
            // Find the angle differences between different limbs
            for (int i = 0; i < limbs.Count - 1; i += 2)
            {
                Vector3 a = limbs[i].transform.position;        // Pos of the first limb tip
                Vector3 b = limbs[i + 1].transform.position;    // Pos of the second limb tip
                Vector3 c;                                      // Pos C to make a right triangle ACB

                // Skip the calculation if the limbs are (almost) at the same height
                if (Mathf.Abs(a.y - b.y) <= realignmentThreshold)
                {
                    angles.Add(0);
                    continue;
                }

                // Make sure C is parallel to the lower point
                if (a.y > b.y)
                {
                    c = new Vector3(a.x, b.y, a.z);
                    rotZDirection = -1;
                }
                else
                {
                    c = new Vector3(b.x, a.y, b.z);
                    rotZDirection = 1;
                }


                // Get the triangle sides
                float hypotenuse = Vector3.Distance(a, b);
                float opposite = Vector3.Distance(a, c);
                float adjacent = Vector3.Distance(b, c);

                // Find both angles and use the smalles one (acute)
                float theta = Mathf.Asin(opposite / hypotenuse) * Mathf.Rad2Deg;
                float gamma = Mathf.Asin(adjacent / hypotenuse) * Mathf.Rad2Deg;

                // Find the lesser angle between the two
                float angle = theta <= gamma ? theta : gamma;

                // Adjust the rotation to match the body
                angle *= rotZDirection;

                angles.Add(angle);
            }

            // Average out the angles
            float angleSum = 0;
            for (int i = 0; i < angles.Count; i++)
            {
                angleSum += angles[i];
            }

            rotZ = angleSum / angles.Count;
        }


        Vector3 eulerAngles = transform.rotation.eulerAngles;
        eulerAngles.x = Mathf.RoundToInt(rotX);
        eulerAngles.z = Mathf.RoundToInt(rotZ);

        Quaternion rotation = Quaternion.Euler(eulerAngles);


        return rotation;
    }


    /// <summary>
    /// Returns true if one or more legs are currently moving
    /// </summary>
    /// <returns></returns>
    private bool AreLegsMoving()
    {
        for (int i = 0; i < limbs.Count; i++)
        {
            if (limbs[i].IsMoving)
            {
                return true;
            }
        }

        return false;
    }
    


    private void GenerateBoneColliders()
    {
        for (int i = 0; i < limbs.Count; i++)
        {
            ConstraintController limb = limbs[i];
            GameObject root = limb.TwoBoneIKConstraint.data.root.gameObject;
            GameObject mid = limb.TwoBoneIKConstraint.data.mid.gameObject;
            GameObject end = limb.TwoBoneIKConstraint.data.tip.parent.gameObject;
            GameObject tip = limb.TwoBoneIKConstraint.data.tip.gameObject;

            Vector3 a = root.transform.localPosition;
            Vector3 b = mid.transform.localPosition;
            Vector3 globalScale = root.transform.lossyScale;


            Vector3 center = (a + b) / 2;                        // Find the midway point to use as the center
            float height = Vector3.Distance(a.DivideBy(globalScale), b.DivideBy(globalScale));
            print(height); 
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = center;
            collider.height = height;
            collider.radius = height / 4;
        }
    }


    /// <summary>
    /// Returns the transform's global scale by recursively multiplying all inherited scales
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    private Vector3 GetInheritedScale(Transform child)
    {
        Vector3 thisScale = child.localScale;

        if (child.parent == null)
        {
            return thisScale;
        }
        else
        {
            Vector3 parentScale = GetInheritedScale(child.parent);

            return Vector3.Scale(thisScale, parentScale);
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(CenterOfMass, .05f);
    }
}