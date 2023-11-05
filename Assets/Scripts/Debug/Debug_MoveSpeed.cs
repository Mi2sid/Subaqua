using System;
using TMPro;
using UnityEngine;

namespace PM1_Debug
{
    public class Debug_MoveSpeed : Debug_Parent
    {
        public TMP_Text moveSpeedText;

        private Vector3 lastPos = Vector3.zero;
        private float lastTime = 0;
        
        protected override void UpdateData()
        {
            Vector3 playerPosition = ChunkSystem.inst.target.position;
            float moveSpeed = Vector3.Distance(lastPos, playerPosition) / (Time.time - lastTime);
            moveSpeedText.text = "Speed : " + moveSpeed.ToString("F2") + "m/s";
            lastPos = playerPosition;
            lastTime = Time.time;
        }
    }
}