using System;
using System.Collections;
using UnityEngine;

namespace PM1_Debug
{
    public abstract class Debug_Parent : MonoBehaviour
    {
        [Tooltip("Zero or less to update each frame")] public float updateTime = 0.1f;

        private void Start()
        {
            if (updateTime <= 0)
                StartCoroutine(UpdateDataEachFrame());
            else
                StartCoroutine(UpdateDataTimed());
        }

        protected abstract void UpdateData();

        private IEnumerator UpdateDataEachFrame()
        {
            while (true)
            {
                UpdateData();
                yield return null;
            }
        }

        private IEnumerator UpdateDataTimed()
        {
            while (true)
            {
                UpdateData();
                yield return new WaitForSeconds(updateTime);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnEnable()
        {
            if (updateTime <= 0)
                StartCoroutine(UpdateDataEachFrame());
            else
                StartCoroutine(UpdateDataTimed());
        }
    }
}