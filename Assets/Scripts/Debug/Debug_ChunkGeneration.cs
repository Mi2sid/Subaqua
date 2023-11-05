using TMPro;
using UnityEngine;

namespace PM1_Debug
{
    public class Debug_ChunkGeneration : Debug_Parent
    {
        public TMP_Text cachedNbText;
        public TMP_Text genStackNbText;

        protected override void UpdateData()
        {
            cachedNbText.text = ChunkSystem.inst.GetChunksSize().ToString();
            genStackNbText.text = ChunkSystem.inst.GetGenQueueSize().ToString();
        }
    }
}