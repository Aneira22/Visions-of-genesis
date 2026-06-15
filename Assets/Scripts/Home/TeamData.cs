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
        public Color accent = new Color(0.5f, 0.5f, 0.6f, 1f);
        public int portraitIndex = -1;
    }

    public static class HeroRoster
    {
        public static List<HeroInfo> BuildOwned()
        {
            return new List<HeroInfo>
            {
                new HeroInfo { id = "khael", name = "Khael", role = "Espada",   level = 1, accent = new Color(0.9098039f, 0.2901961f, 0.3529412f, 1f), portraitIndex = 0 },
                new HeroInfo { id = "aria",  name = "Aria",  role = "Sanadora", level = 1, accent = new Color(0.21176471f, 0.7882353f, 0.9411765f, 1f), portraitIndex = 1 },
            };
        }
    }

    public static class TeamState
    {
        public const int PartySize = 5;
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
