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
    List<float> pressureField;    

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
        pressureField = new List<float>(count);

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
        for (int i=0; i < pressureField.Capacity; i++)
        {
            int ind = index.x * (dimensions.y * dimensions.z) + index.y * dimensions.z + index.z;
            Vector3 centerPosVector = new Vector3(index.x + halfDimension.x, index.y + halfDimension.y, (index.z + halfDimension.z));
            Vector3 depthVector = Vector3.forward;

            pressureField.Add( (dimensions.y - index.y) / (float)dimensions.y * 500.0f );

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

        Debug.Log(string.Format("Field {0} {1} {2} generated, {3} vectors created", dimensions.x, dimensions.y, dimensions.z, pressureField.Count));
    }


    void Visualize()
    {
        Vector3Int index = Vector3Int.zero;
        Vector3 halfStep = posStep * 0.5f;
        foreach (float p in pressureField)
        {
            Vector3 vOrigin = origin + halfStep + new Vector3(index.x * posStep.x, index.y * posStep.y, index.z * posStep.z);

            if (Mathf.Abs(p) <= 0.0001f) {
                //Gizmos.DrawCube(vOrigin, Vector3.one * 0.01f);
            } else {
                if (p>0)
                    Gizmos.color = Color.red;
                else {
                    Gizmos.color = Color.cyan;
                }
                float p01 = Mathf.Abs(p) / 500.0f * density * 0.5f;
                Gizmos.DrawCube(vOrigin, new Vector3(p01, p01, p01) );
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


    float GetPressureAtPoint(Vector3 point)
    {
        Vector3Int vPos = GetFieldPosForPoint(point);

        int index = vPos.x * (dimensions.y * dimensions.z) + vPos.y * dimensions.z + vPos.z;
        
        if ( (index<0) | (index > pressureField.Count) ) {
            Debug.Log("BAD INDEX: " + index + " Ind: " + vPos + " Pos: " + point);
            return 0;
        }
        
        return pressureField[index];
    }


    float GetPressureAtIndex(Vector3Int vecPos)
    {
        int index = vecPos.x * (dimensions.y * dimensions.z) + vecPos.y * dimensions.z + vecPos.z;
        
        if ( (index<0) || (index > pressureField.Count) ) {
            Debug.Log("BAD V POS: " + vecPos);
            return 0;
        }

        return pressureField[index];
    }


    void SetPressureAtFieldPos(Vector3Int vecPos, float pressure)
    {
        int index = vecPos.x * (dimensions.y * dimensions.z) + vecPos.y * dimensions.z + vecPos.z;

        pressureField[index] = pressure;
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
        //ApplyRingsForces();

        ProcessPressureField();
    }


    void ApplyRingsForces()
    {
        foreach(Ring ring in rings)
        {
            float coeff = vectorForce * (1.0f/(float)ring.colliders.GetLength(0));

            foreach(MeshCollider coll in ring.colliders)
            {
                
            }
        }
    }


    void ProcessPressureField()
    {
        int dataCount = pressureField.Count;

        ComputeShader cShader = Resources.Load<ComputeShader>("VectorFieldComputeShader");
        
        ComputeBuffer pressureBuffer = new ComputeBuffer(dataCount, sizeof(float));
        ComputeBuffer newPressureBuffer = new ComputeBuffer(dataCount, sizeof(float));
        pressureBuffer.SetData(pressureField);

        cShader.SetBuffer(0, "pressureField", pressureBuffer);
        cShader.SetBuffer(0, "newPressureField", newPressureBuffer);
        cShader.SetInts("fieldDimensions", new int[] {dimensions.x, dimensions.y, dimensions.z});
        cShader.SetVector("positionStep", posStep);

        cShader.SetVector("jetLocalPosition", (jets[0].transform.position - origin));
        cShader.SetFloat("jetRadius", jets[0].radius);
        cShader.SetVector("jetForce", Vector3.up * vectorForce);
        cShader.SetBool("isJetActive", Input.GetKey(KeyCode.Space));
    
        
        cShader.Dispatch(0, dimensions.x, dimensions.y, dimensions.z);


        float[] data = new float[dataCount];    
        newPressureBuffer.GetData(data);
        pressureField = new List<float>(data);
        
        pressureBuffer.Dispose();
        newPressureBuffer.Dispose();
    }
}
