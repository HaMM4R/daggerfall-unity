using UnityEngine;
using System;
using System.Collections;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Serialization;

namespace DaggerfallWorkshop.Game
{
    public class VRCamera : MonoBehaviour
    {
        private Transform mainCamera;

        void Start()
        {
            mainCamera = GameManager.Instance.MainCamera.transform; 
        }

        public Quaternion ReturnYaw()
        {
            return Quaternion.Euler(0, mainCamera.localEulerAngles.y, 0);
        }
    }
}