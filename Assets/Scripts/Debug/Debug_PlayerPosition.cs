using TMPro;
using UnityEngine;

namespace PM1_Debug
{
    public class Debug_PlayerPosition : Debug_Parent
    {
        public TMP_Text worldPos;
        public TMP_Text chunkPos;

        protected override void UpdateData()
        {
            worldPos.text = ChunkSystem.inst.target.position.ToString();
            chunkPos.text = ChunkSystem.inst.GetCurrentChunkPos().ToString();
        }
    }
}