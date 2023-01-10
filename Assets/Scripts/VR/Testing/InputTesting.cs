using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class InputTesting : MonoBehaviour
{
    public List<XRController> controllers = new List<XRController>();
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var inputDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(inputDevices);

        foreach (var device in inputDevices)
        {
            if (fire)
                Debug.Log("true");
           // Debug.Log("test");
            //Debug.Log(string.Format("Device found with name '{0}' and role '{1}'", device.name, device.role.ToString()));
        }

        
    }

    public bool fire
    {
        get
        {
            bool triggerVal;

            controllers[1].inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerVal);
            return triggerVal;
        }
    }
}

