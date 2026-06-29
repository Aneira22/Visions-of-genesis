using System.Collections.Generic;

namespace VisionsOfGenesis.Home
{
    public enum StoryEntryType { Battle, Video }

    public class EnemySpawn
    {
        public string unitName;
        public int count;

        public EnemySpawn(string unitName, int count)
        {
            this.unitName = unitName;
            this.count = count;
        }
    }

    public class Reward
    {
        public string name;
        public int amount;

        public Reward(string name, int amount)
        {
            this.name = name;
            this.amount = amount;
        }
    }

    public class StoryEntry
    {
        public string id;
        public string title;
        public StoryEntryType type = StoryEntryType.Battle;
        public int energyCost = 2;
        public int battleCount = 1;
        public string sceneToLoad = "Game";
        public int heroExp;       // EXP for each Vision that fought, every victory
        public int playerExp;     // EXP for the Sincronizador, first completion only
        public int goldReward;    // shop currency, every victory
        public int crystalReward; // gacha currency, first completion only
        public List<string> missions = new List<string>();
        public List<Reward> rewards = new List<Reward>();
        public List<EnemySpawn> enemies = new List<EnemySpawn>();
    }

    public class StoryZone
    {
        public string id;
        public string displayName;
        public float mapX = 0.5f;
        public float mapY = 0.5f;
        public List<StoryEntry> entries = new List<StoryEntry>();
    }

    public class StoryMap
    {
        public string mapName;
        public List<StoryZone> zones = new List<StoryZone>();
    }

    public static class StoryDatabase
    {
        public static StoryMap BuildForestMap()
        {
            var map = new StoryMap { mapName = "Bosque de Lumina" };

            var forest = new StoryZone
            {
                id = "forest",
                displayName = "Bosque de Lumina",
                mapX = 0.42f,
                mapY = 0.52f
            };

            forest.entries.Add(new StoryEntry
            {
                id = "forest_entrance",
                title = "Bosque - Entrada",
                type = StoryEntryType.Battle,
                energyCost = 1,
                battleCount = 1,
                sceneToLoad = "Game",
                heroExp = 100,
                playerExp = 60,
                goldReward = 150,
                crystalReward = 50,
                missions = new List<string>
                {
                    "Completa la batalla",
                    "Gana sin perder ninguna Vision",
                    "Usa una habilidad"
                },
                rewards = new List<Reward>
                {
                    new Reward("Oro", 150),
                    new Reward("Cristales", 50),
                    new Reward("Poción", 2),
                    new Reward("Éter", 1),
                    new Reward("Fragmento Etereo", 2),
                    new Reward("EXP Visiones", 100),
                    new Reward("EXP Sincronizador", 60)
                },
                enemies = new List<EnemySpawn>
                {
                    new EnemySpawn("Corrupt Echo", 1)
                }
            });

            forest.entries.Add(new StoryEntry
            {
                id = "forest_clearing",
                title = "Bosque - Claro corrupto",
                type = StoryEntryType.Battle,
                energyCost = 2,
                battleCount = 1,
                sceneToLoad = "Game",
                heroExp = 150,
                playerExp = 90,
                goldReward = 250,
                crystalReward = 80,
                missions = new List<string>
                {
                    "Derrota al Eco Corrupto",
                    "Termina en 5 rondas o menos"
                },
                rewards = new List<Reward>
                {
                    new Reward("Oro", 250),
                    new Reward("Cristales", 80),
                    new Reward("Fragmento Etereo", 3),
                    new Reward("Nucleo Sombrio", 1),
                    new Reward("Esencia Corrupta", 1),
                    new Reward("EXP Visiones", 150),
                    new Reward("EXP Sincronizador", 90)
                },
                enemies = new List<EnemySpawn>
                {
                    new EnemySpawn("Corrupt Echo", 2)
                }
            });

            forest.entries.Add(new StoryEntry
            {
                id = "forest_prologue",
                title = "Prologo: La Grieta",
                type = StoryEntryType.Video,
                energyCost = 0,
                battleCount = 0,
                sceneToLoad = "",
                missions = new List<string> { "Mira la escena (proximamente)" },
                rewards = new List<Reward> { new Reward("Cristales", 20) }
            });

            map.zones.Add(forest);
            return map;
        }
    }

    public static class StorySelection
    {
        public static StoryEntry SelectedEntry;
    }
}
