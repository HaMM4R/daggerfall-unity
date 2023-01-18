using UnityEngine;
using System;
using System.Collections;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Serialization;

namespace DaggerfallWorkshop.Game
{
    public class VRManager : MonoBehaviour
    {
        private Transform mainCamera;

        [SerializeField]
        private Transform leftHand;
        [SerializeField]
        private Transform rightHand;

        public Transform LeftHand { get { return leftHand; } }
        public Transform RightHand { get { return rightHand; } }

        void Start()
        {
            mainCamera = GameManager.Instance.MainCamera.transform; 
        }

        public Quaternion ReturnCameraYaw()
        {
            return Quaternion.Euler(0, mainCamera.localEulerAngles.y, 0);
        }
    }
}