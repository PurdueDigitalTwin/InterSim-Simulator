using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetectedObjectReporter : MonoBehaviour {

    public Text textfield;

	// Use this for initialization
	private void Start ()
    {
        if (textfield != null)
        {
            textfield.text = "";
        }
    }
	
	// Update is called once per frame
	private void Update ()
    {
        if (textfield != null)
        {
            textfield.text = "";
            foreach (Transform child in transform)
            {
                RadarCast radarCast = child.gameObject.GetComponent<RadarCast>();
                if (radarCast != null)
                {
                    textfield.text += radarCast.DetectedObjectsToString();
                }
            }
        }
	}
}
