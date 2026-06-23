using UnityEngine;

namespace Tattoo
{
    [CreateAssetMenu(menuName = "Tattoo/Pattern", fileName = "Pattern")]
    public class PatternSO : ScriptableObject
    {
        public string       PatternName;
        public EffectShape  Shape;
        public float        PatternMultiplier = 1f;
    }
}
