using System;
using UnityEngine;

namespace Aqua
{
    [Serializable]
    public struct ColorPalette4
    {
        public Color32 Background;
        public Color32 Content;
        public Color32 Shadow;
        public Color32 Highlight;

        public ColorPalette4(Color32 inContent, Color32 inBackground)
        {
            Background = inBackground;
            Content = inContent;
            Shadow = Color.Lerp(Background, Color.black, 0.25f);
            Highlight = Color.Lerp(Background, Color.white, 0.25f);
        }
    }
}