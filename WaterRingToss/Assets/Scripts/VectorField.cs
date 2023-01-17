using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorField : MonoBehaviour
{
    [SerializeField]
    [Range(0, 100)]
    float vectorForce = 1;
    [SerializeField]
    Vector3 origin;
    [SerializeField]
    Vector3 size;
    [SerializeField]
    Vector3Int dimensions;

    [SerializeField]
    bool drawField = false;
    
    bool inited = false;
    List<Vector3> vectors;    

    // sim vals
    Vector3 posStep;

    struct Ring
    {
        public Rigidbody body;
        public Collider[] colliders;
    }

    List<Ring> rings = new List<Ring>();

//================================================================================================================================

    void Start()
    {
        CreateVectorField(origin, size, dimensions);

        FindAndSaveRings();
    }

    void FindAndSaveRings()
    {
        GameObject[] ringsObjects = GameObject.FindGameObjectsWithTag("Ring");
        foreach(GameObject ringObject in ringsObjects) 
        {
            Ring newRing = new Ring();
            newRing.body = ringObject.GetComponent<Rigidbody>();
            newRing.colliders = ringObject.GetComponents<MeshCollider>();
            rings.Add(newRing);
        }

        Debug.Log(string.Format("{0} rings found", rings.Count));
    }

    void CreateVectorField(Vector3 newOrigin, Vector3 newSize, Vector3Int newDimensions)
    {
        origin = newOrigin;
        size = newSize;
        dimensions = newDimensions;
        int count = dimensions.x * dimensions.y * dimensions.z;
        vectors = new List<Vector3>(count);

        posStep = new Vector3(size.x / dimensions.x, size.y / dimensions.y, size.z / dimensions.z);

        //dbg
        GenDebugField();

        inited = true;
    }

    void GenDebugField()
    {
        Vector3Int index = Vector3Int.zero;
        Vector3 halfDimension = new Vector3(-dimensions.x*0.5f, -dimensions.y*0.5f, -dimensions.z*0.5f);
        //Quaternion rotationAdjust = Quaternion.AngleAxis(-45, Vector3.forward);
        for (int i=0; i < vectors.Capacity; i++)
        {
            int ind = index.x * (dimensions.y * dimensions.z) + index.y * dimensions.z + index.z;
            Vector3 centerPosVector = new Vector3(index.x + halfDimension.x, index.y + halfDimension.y, (index.z + halfDimension.z));
            Vector3 depthVector = Vector3.forward;
            Vector3 dirVector = /*rotationAdjust */ Vector3.Cross(depthVector, centerPosVector).normalized;

            vectors.Add( dirVector );

            index.z++;
            if (index.z >= dimensions.z) {
                index.z = 0;
                index.y++;
            }
            if (index.y >= dimensions.y) {
                index.y = 0;
                index.x++;
            }
        }

        Debug.Log(string.Format("Field {0} {1} {2} generated, {3} vectors created", dimensions.x, dimensions.y, dimensions.z, vectors.Count));
    }

    void Visualize()
    {
        Vector3Int index = Vector3Int.zero;
        Vector3 halfStep = posStep * 0.5f;
        foreach (Vector3 v in vectors)
        {
            Vector3 vOrigin = origin + halfStep + new Vector3(index.x * posStep.x, index.y * posStep.y, index.z * posStep.z);

            Gizmos.color = new Color(Mathf.Abs(v.normalized.x), Mathf.Abs(v.normalized.y), Mathf.Abs(v.normalized.z));
            Gizmos.DrawRay(vOrigin, v);

            index.z++;
            if (index.z >= dimensions.z) {
                index.z = 0;
                index.y++;
            }
            if (index.y >= dimensions.y) {
                index.y = 0;
                index.x++;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (inited & drawField)
            Visualize();
    }

    Vector3 GetEffectorAtPoint(Vector3 point)
    {
        Vector3 effectVector = Vector3.zero;

        point -= origin;
        point += posStep * 0.5f;
        point = new Vector3( Mathf.Clamp(point.x / posStep.x, 0, dimensions.x),
                             Mathf.Clamp(point.y / posStep.y, 0, dimensions.y),
                             Mathf.Clamp(point.z / posStep.z, 0, dimensions.z) );
        
        Vector3Int vPos = new Vector3Int( (int)Mathf.Floor(point.x), (int)Mathf.Floor(point.y), (int)Mathf.Floor(point.z) );

        int index = vPos.x * (dimensions.y * dimensions.z) + vPos.y * dimensions.z + vPos.z;
        
        if ( (index<0) | (index > vectors.Count) ) {
            Debug.Log("BAD INDEX: " + index + " Ind: " + vPos + " Pos: " + point);
            return effectVector;
        }
        
        effectVector = vectors[index];

        return effectVector;
    }

    void FixedUpdate()
    {
        foreach(Ring ring in rings)
        {
            float coeff = vectorForce * (1.0f/(float)ring.colliders.GetLength(0));

            foreach(MeshCollider coll in ring.colliders)
            {
                //Vector3 collCenterWorld = ring.body.transform.position + ring.body.transform.rotation * coll.bounds.center;
                Vector3 collCenterWorld = coll.bounds.center;
                Vector3 effector = GetEffectorAtPoint(collCenterWorld);
                
                Debug.DrawRay(collCenterWorld, effector, Color.blue, Time.deltaTime);
                ring.body.AddForceAtPosition(effector * coeff, collCenterWorld);
            }
        }
        
    }
}
