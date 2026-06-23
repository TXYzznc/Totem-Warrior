using UnityEngine;

namespace Tattoo
{
    [CreateAssetMenu(menuName = "Tattoo/Color", fileName = "Color")]
    public class ColorSO : ScriptableObject
    {
        public string           ColorName;
        public ElementBehavior  Element;
        public float            ColorMultiplier = 1f;
    }
}
