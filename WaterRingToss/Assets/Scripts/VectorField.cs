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
    float density = 1;
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

    List<SphereCollider> jets = new List<SphereCollider>();

//================================================================================================================================


    void Start()
    {
        Vector3Int newDimensions = new Vector3Int( (int)Mathf.Floor(size.x / density), (int)Mathf.Floor(size.y / density), (int)Mathf.Floor(size.z / density) );
        CreateVectorField(origin, size, newDimensions);

        FindAndSaveRings();

        FindAndSaveJets();
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

    
    void FindAndSaveJets()
    {
        GameObject[] jetsObjects = GameObject.FindGameObjectsWithTag("Jet");
        foreach(GameObject jetObject in jetsObjects) 
        {
            SphereCollider coll = jetObject.GetComponent<SphereCollider>();
            if (coll != null)
                jets.Add(coll);
        }
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
        Quaternion rotationAdjust = Quaternion.AngleAxis(-40, Vector3.forward);
        for (int i=0; i < vectors.Capacity; i++)
        {
            int ind = index.x * (dimensions.y * dimensions.z) + index.y * dimensions.z + index.z;
            Vector3 centerPosVector = new Vector3(index.x + halfDimension.x, index.y + halfDimension.y, (index.z + halfDimension.z));
            //Vector3 depthVector = Vector3.forward;
            //Vector3 dirVector = rotationAdjust * Vector3.Cross(centerPosVector, depthVector).normalized;

            vectors.Add( Vector3.zero );

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


    void ResetField()
    {
        for (int i=0; i < vectors.Count; i++)
        {
            vectors[i] = Vector3.zero;
        }
    }


    void Visualize()
    {
        Vector3Int index = Vector3Int.zero;
        Vector3 halfStep = posStep * 0.5f;
        foreach (Vector3 v in vectors)
        {
            Vector3 vOrigin = origin + halfStep + new Vector3(index.x * posStep.x, index.y * posStep.y, index.z * posStep.z);

            if ( float.IsNaN(v.x) || float.IsNaN(v.x) || float.IsNaN(v.x) ) {
                Gizmos.color = Color.magenta;
                Gizmos.DrawCube(vOrigin, Vector3.one * density * 0.5f);
            } else {

                if (v.sqrMagnitude <= 0.001f) {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(vOrigin, Vector3.one * 0.01f);
                } else {
                    Gizmos.color = new Color(Mathf.Abs(v.normalized.x), Mathf.Abs(v.normalized.y), Mathf.Abs(v.normalized.z));
                    Gizmos.DrawRay(vOrigin, v * density);
                }
                
            }


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
        
        Vector3Int vPos = GetFieldPosForPoint(point);

        int index = vPos.x * (dimensions.y * dimensions.z) + vPos.y * dimensions.z + vPos.z;
        
        if ( (index<0) | (index > vectors.Count) ) {
            Debug.Log("BAD INDEX: " + index + " Ind: " + vPos + " Pos: " + point);
            return effectVector;
        }
        
        effectVector = vectors[index];

        return effectVector;
    }


    Vector3 GetVectorAtIndex(Vector3Int vecPos)
    {
        int index = vecPos.x * (dimensions.y * dimensions.z) + vecPos.y * dimensions.z + vecPos.z;
        
        if ( (index<0) || (index > vectors.Count) ) {
            Debug.Log("BAD V POS: " + vecPos);
            return Vector3.zero;
        }

        return vectors[index];
    }


    void SetVectorAtIndex(Vector3Int vecPos, Vector3 vector)
    {
        int index = vecPos.x * (dimensions.y * dimensions.z) + vecPos.y * dimensions.z + vecPos.z;

        vectors[index] = vector;
    }


    Vector3Int GetFieldPosForPoint(Vector3 position)
    {
        Vector3Int vecPos = Vector3Int.zero;

        // local coord
        position -= origin;

        // half-step to center
        position += posStep * 0.5f;

        //clamp position to field size
        position = new Vector3( Mathf.Clamp(position.x / posStep.x, 0, dimensions.x-1),
                                Mathf.Clamp(position.y / posStep.y, 0, dimensions.y-1),
                                Mathf.Clamp(position.z / posStep.z, 0, dimensions.z-1) );

        vecPos = new Vector3Int( (int)Mathf.Floor(position.x), (int)Mathf.Floor(position.y), (int)Mathf.Floor(position.z) );

        return vecPos;
    }


    bool IsValidIndex(Vector3Int ind)
    {
        if ( (ind.x < 0) || (ind.x >= dimensions.x) )
            return false;
        
        if ( (ind.y < 0) || (ind.y >= dimensions.y) )
            return false;

        if ( (ind.z < 0) || (ind.z >= dimensions.z) )
            return false;

        return true;
    }


    void FixedUpdate()
    {
        ApplyRingsForces();

        ProcessVectorField();

        if (Input.GetKey(KeyCode.Alpha1))
            ResetField();
        
    }

    
    void OnGUI()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Physics.gravity = new Vector3(-10, 0, 0);
        
        if (Input.GetKeyDown(KeyCode.RightArrow))
            Physics.gravity = new Vector3(10, 0, 0);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            Physics.gravity = new Vector3(0, 0, 10);
        
        if (Input.GetKeyDown(KeyCode.DownArrow))
            Physics.gravity = new Vector3(0, 0, -10);

        if ( Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow) ||
             Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow) )
            Physics.gravity = new Vector3(0, -9.81f, 0);
    }


    void ApplyRingsForces()
    {
        foreach(Ring ring in rings)
        {
            float coeff = vectorForce * (1.0f/(float)ring.colliders.GetLength(0));

            foreach(MeshCollider coll in ring.colliders)
            {
                Vector3 collCenterWorld = coll.bounds.center;
                Vector3 effector = GetEffectorAtPoint(collCenterWorld);
                
                Debug.DrawRay(collCenterWorld, effector, Color.blue, Time.deltaTime);
                ring.body.AddForceAtPosition(effector * coeff, collCenterWorld);
            }
        }
    }


    void ProcessVectorField()
    {
        int dataCount = vectors.Count;

        ComputeShader cShader = Resources.Load<ComputeShader>("VectorFieldComputeShader");
        
        ComputeBuffer vectorsBuffer = new ComputeBuffer(dataCount, sizeof(float)*3);
        ComputeBuffer newVectorsBuffer = new ComputeBuffer(dataCount, sizeof(float)*3);
        vectorsBuffer.SetData(vectors);

        cShader.SetBuffer(0, "vectorField", vectorsBuffer);
        cShader.SetBuffer(0, "newVectorField", newVectorsBuffer);
        cShader.SetInts("fieldDimensions", new int[] {dimensions.x, dimensions.y, dimensions.z});
        cShader.SetVector("positionStep", posStep);
        cShader.SetInt("vectorsCount", dataCount);

        cShader.SetVector("jetLocalPosition", (jets[0].transform.position - origin));
        cShader.SetFloat("jetRadius", jets[0].radius);
        cShader.SetVector("jetForce", Vector3.up * vectorForce);
        cShader.SetBool("isJetActive", Input.GetKey(KeyCode.Space));

        cShader.Dispatch(0, dimensions.x, dimensions.y, dimensions.z);
        
        Vector3[] data = new Vector3[dataCount];
        newVectorsBuffer.GetData(data);
        vectors = new List<Vector3>(data);
        
        vectorsBuffer.Dispose();
        newVectorsBuffer.Dispose();

        string strVal = "";
        for (int i = 0; i < dimensions.z; i++)
        {
            strVal += GetVectorAtIndex(new Vector3Int(2, 2, i)) + " ";
        }
        Debug.Log(strVal);
    }


    void ProcessJets()
    {
        foreach (SphereCollider jet in jets)
        {
            // get jet position in vector field
            Vector3Int jetFieldPosition = GetFieldPosForPoint(jet.transform.position);

            // set new vector for position
            SetVectorAtIndex(jetFieldPosition, Vector3.one * 1000);
        }
    }
}
