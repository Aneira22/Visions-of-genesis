using VisionsOfGenesis.Data;

namespace VisionsOfGenesis.Combat
{
    // Elemental affinity: how much damage an attack of one element does to a
    // target of another. Foundation for the future elemental-chain (spark) combos.
    //
    // Relationships (attacker is super-effective vs target -> x1.5; the reverse
    // resists -> x0.5):
    //   Fire  > Earth > Wind > Water > Fire   (a four-element cycle)
    //   Light <> Dark                         (mutual: each devastates the other)
    // Neutral is never strong nor weak (always x1.0).
    public static class Elements
    {
        public const float WeakMultiplier    = 1.5f;
        public const float ResistMultiplier  = 0.5f;
        public const float NeutralMultiplier = 1f;

        // True when `attacker` is super-effective against `target`.
        static bool Beats(Element attacker, Element target)
        {
            switch (attacker)
            {
                case Element.Fire:  return target == Element.Earth;
                case Element.Earth: return target == Element.Wind;
                case Element.Wind:  return target == Element.Water;
                case Element.Water: return target == Element.Fire;
                case Element.Light: return target == Element.Dark;
                case Element.Dark:  return target == Element.Light;
                default:            return false; // Neutral
            }
        }

        public static float Affinity(Element attack, Element target)
        {
            if (Beats(attack, target)) return WeakMultiplier;
            if (Beats(target, attack)) return ResistMultiplier;
            return NeutralMultiplier;
        }

        // Short on-screen feedback for a resolved hit, or "" for a neutral one.
        public static string Label(float affinity)
        {
            if (affinity > 1f) return "¡Débil!";
            if (affinity < 1f) return "Resiste";
            return "";
        }
    }
}
