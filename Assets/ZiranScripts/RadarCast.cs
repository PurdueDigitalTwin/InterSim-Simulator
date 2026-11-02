// Log:
// 06-25-2018 Ziran Wang
// 1. Commented "Debug.DrawLine(ray.origin, ray.origin + ray.direction * DitectionLength, Color.red)" to disable the red line shown for radar.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarCast : MonoBehaviour
{
    public class DetectedObject
    {
        public EntityId id;
        public string name;
        public float distance;
        public Vector3 relativePosition;
        public Vector3 relativeVelocity;

        public DetectedObject (EntityId id, string name, float distance, Vector3 relativePosition, Vector3 relativeVelocity)
        {
            this.id = id;
            this.name = name;
            this.distance = distance;
            this.relativePosition = relativePosition;
            this.relativeVelocity = relativeVelocity;
        }
    }
    
    public float minDitectionLength = 0.2f;
    public float maxDitectionLength = 200.0f;
    public float range = 18.0f;
    public int segments = 18;
    public bool fixedAngle = false;
    public bool onlyDetectableObjects = false;

    private Rigidbody parentRigitbody;

    [System.NonSerialized]
    public List<DetectedObject> detectedObjects = new List<DetectedObject>();

    [System.NonSerialized]
    public Vector3 radarEquipedAngleVector;

    private void Start()
    {
        parentRigitbody = GetComponentInParent<Rigidbody>();

        radarEquipedAngleVector = parentRigitbody.transform.InverseTransformDirection(transform.TransformDirection(Vector3.forward));
    }
    
    private void Update ()
    {
        detectedObjects.Clear();

        float angluarBias = -0.5f * range;
        float angularSegment = range / segments;

        for (int i = 0; i <= segments; i++)
        {
            Quaternion directionalOffset = Quaternion.Euler(0.0f, (angularSegment * i + angluarBias), 0.0f);
            Vector3 originalOffset = new Vector3(0.0f, 0.0f, (float)minDitectionLength);

            Vector3 offsetVector;
            if (fixedAngle)
            {
                offsetVector = Quaternion.Euler(0.0f, transform.rotation.eulerAngles.y, 0.0f) * directionalOffset * originalOffset;
            }
            else
            {
                offsetVector = transform.rotation * directionalOffset * originalOffset;
            }
            Vector3 origin = offsetVector + transform.position;
            Vector3 direction = Vector3.Normalize(offsetVector);

            Ray ray = new Ray(origin, direction);
            RaycastHit hit;
            float DitectionLength = maxDitectionLength - minDitectionLength;

            Debug.DrawLine(ray.origin, ray.origin + ray.direction * DitectionLength, Color.red);

            if (Physics.Raycast(ray, out hit, DitectionLength))
            {
                if (onlyDetectableObjects && hit.collider.tag != "RadarDetectable" && hit.collider.tag != "Player")
                {
                    continue;
                }

                EntityId id = hit.collider.transform.root.gameObject.GetEntityId();
                string name = hit.collider.transform.root.gameObject.ToString();
                Vector3 relativePosition = parentRigitbody.transform.InverseTransformPoint(hit.point);
                Rigidbody objectRigitbody = hit.collider.GetComponentInParent<Rigidbody>();
                Vector3 relativeVelocity = parentRigitbody.transform.InverseTransformVector(objectRigitbody.linearVelocity - parentRigitbody.linearVelocity);
                float distance = relativePosition.magnitude;

                DetectedObject previouslyDetectedObject = detectedObjects.Find(x => x.id == id);
                if (previouslyDetectedObject == null)
                { // first time : simlpy add
                    detectedObjects.Add(new DetectedObject(id, name, distance, relativePosition, relativeVelocity));
                }
                else
                { // second time : compare distances
                    if (distance < previouslyDetectedObject.distance)
                    {
                        previouslyDetectedObject.distance = distance;
                        previouslyDetectedObject.relativePosition = relativePosition;
                        previouslyDetectedObject.relativeVelocity = relativeVelocity;
                    }
                }
            }
        }
    }

    public string DetectedObjectsToString()
    {
        string messageString = "";
        foreach (DetectedObject detectedObject in detectedObjects)
        {
            messageString += detectedObject.name + " : " + detectedObject.relativePosition.ToString() + " : " + detectedObject.distance.ToString("0.00") + " [m]\n";
        }
        return messageString;
    }
}
