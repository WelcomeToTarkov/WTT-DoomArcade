using System.Collections.Generic;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.Scriptables
{
    [CreateAssetMenu(fileName = "SpriteFont", menuName = "SpriteFont")]
    public class SpriteFont : ScriptableObject
    {
        [SerializeField] private List<char> characters;
        [SerializeField] private List<Sprite> sprites;
        private Dictionary<char, Sprite> charToSpriteMap;

        public int LineHeight => Mathf.RoundToInt(sprites[0].rect.height);
    
        private void EnsureInitialized()
        {
            if (charToSpriteMap == null)
            {
                charToSpriteMap = new Dictionary<char, Sprite>();
                for (int i = 0; i < Mathf.Min(characters.Count, sprites.Count); i++)
                {
                    charToSpriteMap[characters[i]] = sprites[i];
                }
            }
        }

        public Sprite[] StringToSpriteArray(string targetString)
        {
            EnsureInitialized();
            List<Sprite> spriteList = new List<Sprite>();

            foreach (char c in targetString)
            {
                if (charToSpriteMap.TryGetValue(c, out Sprite sprite))
                {
                    spriteList.Add(sprite);
                }
                else
                {
                    continue;
                }
            }

            return spriteList.ToArray();
        }
    }
}
