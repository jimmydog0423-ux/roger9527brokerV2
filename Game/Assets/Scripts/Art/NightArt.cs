using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SSD
{
    /// <summary>夜間街景、店面與地點插畫。</summary>
    static class NightArt
    {
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static bool Paint(Image image, string id)
        {
            if (!Sprites.TryGetValue(id, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>("Night/" + id);
                Sprites.Add(id, sprite);
            }
            image.sprite = sprite;
            SceneMotion.Attach(image, sprite != null ? id : null);
            if (sprite == null) return false;
            image.color = Pal.White;
            return true;
        }
    }
}
