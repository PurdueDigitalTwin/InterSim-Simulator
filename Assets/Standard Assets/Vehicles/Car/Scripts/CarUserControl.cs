/*============================================================================*/
/*  CarUserControl.cs                                       Module for Unity  */
/*                                                                            */
/*  Script for User Control of vehicles based on standard asset.              */
/*																			  */
/*  Version of 04-28-2020                             Copyright by Ziran Wang */
/*	Toyota Motor North America, InfoTech Labs   							  */
/*============================================================================*/

/// Log:
/// 02-04-2019 Ziran Wang
/// 1. In the Unity GUI -> Edit -> Project Settings -> Input -> Horizontal, changed
///    "Type" from "Key or Mouse Button" into "Joystick Axis" to enable the sensitive
///    movement of the Logitech Driving Force steering wheel.
/// 2. Due to the previous change, the steering wheel is too sensitive. Added a weighting
///    factor on the horizontal input "h" to reduce its sensitivity.
///
/// 12-17-2019 Ziran Wang
/// 1. Changed Time.time to Time.timeSinceLevelLoad to enable scene switch to run correctly.
///
/// 02-11-2020 Ziran Wang
/// 1. Deleted "&& (Mathf.Abs(steeringInput) > 0.01)" in the moveSteeringWheel function.
/// 
/// 04-28-2020 Ziran Wang
/// 1. Changed Update() into FixedUpdate() for uniformity with other scripts.
/// 

using System;
using System.IO;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use
        private Rigidbody vehicleRigidbody;
        public Transform steeringWheel;
        public Vector3 rotationAxisEulerAngles = new Vector3(25.0f, 0f, 0f);
        private Vector3 initialSteeringPosition;
        private Vector3 steeringRotationAxis;
        private float maxRotationAngle = 160.0f;
        [HideInInspector] public float h, v;
        private System.IO.StreamWriter fileOutput1;
        private long HeaderFlag1;
        private bool Output1;
        
        private void Start()
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
            string vehicleName = transform.root.gameObject.ToString();
            Output1 = false;
            if (Output1) fileOutput1 = new System.IO.StreamWriter("Output_ControlUser_" + vehicleName + ".csv");
            HeaderFlag1 = 0;
        }

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            if (steeringWheel != null)
            {
                initialSteeringPosition = new Vector3(
                    steeringWheel.localRotation.eulerAngles.x,
                    steeringWheel.localRotation.eulerAngles.y,
                    steeringWheel.localRotation.eulerAngles.z
                );
                steeringRotationAxis = Quaternion.Euler(rotationAxisEulerAngles) * Vector3.up;
                steeringRotationAxis = Quaternion.Euler(initialSteeringPosition) * steeringRotationAxis;
            }
        }


        private void FixedUpdate()
        {
            float speed = vehicleRigidbody.linearVelocity.magnitude;
            // pass the input to the car!
            h = CrossPlatformInputManager.GetAxis("Horizontal"); // This value can be tweaked for steering wheel sensitivity
            v = CrossPlatformInputManager.GetAxis("Vertical");
#if !MOBILE_INPUT
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            m_Car.Move(h, v, v, handbrake);
            moveSteeringWheel(h);
#else
            m_Car.Move(h, v, v, 0f);
#endif
            if (Output1)
            {
                if (HeaderFlag1 == 0)
                {
                    fileOutput1.WriteLine("time, control, a0, a_ref, v0, current_speed, detect, r, r_ref, vp, ap, v_tar, Count, Threshold");
                    HeaderFlag1 = 1;
                }
                fileOutput1.WriteLine(Time.time + "," + "User" + "," + "null" + "," + "null" + "," + "null" + "," + vehicleRigidbody.linearVelocity.magnitude + "," + "null" + ","
                    + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null" + "," + "null");
            }

            Debug.Log("Horizontal input: " + h + ", Vertical input: " + v);
        }

        private void moveSteeringWheel(float steeringInput)
        {
            // SteeringInput between -1 and 1
            if (steeringWheel != null)
            {
                steeringWheel.localRotation = Quaternion.AngleAxis(maxRotationAngle * steeringInput, steeringRotationAxis) * Quaternion.Euler(initialSteeringPosition);
            }
        }
    }
}
