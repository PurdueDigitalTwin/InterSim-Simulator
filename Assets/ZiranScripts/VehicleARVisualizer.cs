/*============================================================================*/
/*  VehicleARVisualizer.cs                            Module for Unity  */
/*                                                                            */
/*  Script for visualizing speed in game window in  Unity.                    */
/*																			  */
/*  Version of 04-18-2019                                                     */
/*  Copyright by Ziran Wang                           ryanwang11@hotmail.com  */
/*	University of California, Riverside										  */
/*============================================================================*/

// Log:
// 07-13-2018 Ziran Wang
// 1. Changed kph to mps.
// 
// 12-04-2018 Ziran Wang
// 1. Added the code to get suggested speed from CooperativeMerging.cs.
// 2. Integrated different display mode for head-up display.
//
// 12-12-2018 Ziran Wang
// 1. Added the output file to output the suggested speed and actual speed.
//
// 01-10-2019 Ziran Wang
// 1. Added different colors for warning mode. 
//
// 01-25-2019 Ziran Wang
// 1. Added different colors for the other two modes.
// 2. Added time now to the output file name. No need to delete files for every new run anymore.
// 3. Added "using System" to allow the previous edit.
//
// 02-04-2019 Ziran Wang
// 1. Added acceleration as outputs in csv file.
//
// 04-18-2019 Ziran Wang
// 1. This version is changed from VehicleVelocityVisualizer.cs.
// 2. Added "Environment.NewLine" to make the text into two lines.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class VehicleARVisualizer : MonoBehaviour
{

    public Rigidbody[] vehicles;
    public Text text;
    private System.IO.StreamWriter fileOutput1;
    bool output1 = true;
    long HeaderFlag1;

    private GameObject rampVehicle;
    private string initialMessage = "";

    // Use this for initialization
    void Start()
    {
        text.text = initialMessage;
        HeaderFlag1 = 0; // Initialize the headerflag, so each csv file will only print the header once
    }

    // Update is called once per frame
    void Update()
    {
        string message = "";
        string eachLine = "";
        for (int i = 0; i < vehicles.Length; i++)
        {
            Rigidbody vehicle = vehicles[i];
            
            eachLine = "TARGET" + Environment.NewLine + "LEADER";
            text.color = new Color(1, 0.47f, 0.47f, 1);

            message += eachLine;
        }
        text.text = message;       
    }
}
