namespace Tattoo.Data
{
    /// <summary>一次效果应用的结果。供 UI 与日志展示。</summary>
    public struct EffectResult
    {
        public string Element;
        public string Shape;
        public string Part;
        public float  Damage;
        public int    HitCount;
        public string Status;
        public float  SynergyMul;
        /// <summary>额外标注，如 "Intercepted/PendingTrigger" 或 "ConsumedPending"。</summary>
        public string Note;
    }
}
