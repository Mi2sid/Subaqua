using System;
using UnityEngine;

namespace PM1_Debug
{
    public class Debug_Master : MonoBehaviour
    {
        private void Awake()
        {
            ApplyNewState();
        }

        public void SetDebugState(bool state)
        {
            Vars.DEBUG_MODE = state;
            ApplyNewState();
        }

        private void ApplyNewState()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(Vars.DEBUG_MODE);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Quote))
                SetDebugState(!Vars.DEBUG_MODE);
        }
    }
}