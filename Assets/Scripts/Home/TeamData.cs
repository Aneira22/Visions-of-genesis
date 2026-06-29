using System.Collections.Generic;
using UnityEngine;

namespace VisionsOfGenesis.Home
{
    public class HeroInfo
    {
        public string id;
        public string name;
        public string role;
        public int level = 1;
        public int exp;
        public int stars = 1;
        public Color accent = new Color(0.5f, 0.5f, 0.6f, 1f);
        public int portraitIndex = -1;
    }

    public static class HeroRoster
    {
        // The default roster (starters only). See HeroCatalog for the full hero list.
        public static List<HeroInfo> BuildOwned()
        {
            return HeroCatalog.Starters();
        }
    }

    public static class TeamState
    {
        public const int PartySize = 4;
        public const int PartyCount = 3;

        public static List<HeroInfo> Owned;
        public static HeroInfo[][] Parties;
        public static int CurrentParty;
        public static bool Initialized;

        public static HeroInfo[] Party => Parties[CurrentParty];

        public static void EnsureInit()
        {
            if (Initialized && Parties != null) return;

            Owned = HeroRoster.BuildOwned();

            Parties = new HeroInfo[PartyCount][];
            for (int p = 0; p < PartyCount; p++)
                Parties[p] = new HeroInfo[PartySize];

            for (int i = 0; i < PartySize && i < Owned.Count; i++)
                Parties[0][i] = Owned[i];

            CurrentParty = 0;
            Initialized = true;
        }
    }
}
