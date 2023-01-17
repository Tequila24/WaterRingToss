using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jet : MonoBehaviour
{

    GameObject jet;

    // Start is called before the first frame update
    void Start()
    {
        jet = GameObject.Find("Jet");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
        }
    }
}
