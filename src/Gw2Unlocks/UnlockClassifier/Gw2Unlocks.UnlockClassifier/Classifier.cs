using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Bits;
using GuildWars2.Hero.Achievements.Categories;
using GuildWars2.Hero.Achievements.Groups;
using GuildWars2.Hero.Achievements.Rewards;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using Gw2Unlocks.Api;
using Gw2Unlocks.WikiProcessing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Gw2Unlocks.UnlockClassifier.ClassifyConfigExtensions;
using Node = Gw2Unlocks.WikiProcessing.Node;

namespace Gw2Unlocks.UnlockClassifier;

public class Classifier(IGw2ApiSource apiSource, IGw2WikiProcessingSource wikiProcessingSource, ILogger<Classifier> logger) : IClassifier
{
    private AcquisitionGraph? graph;
    private Collection<Miniature>? miniatures;
    private Collection<Item>? items;
    private Collection<EquipmentSkin>? skins;
    private Collection<Achievement>? achievements;
    private Collection<AchievementCategory>? achievementCategories;
    private Collection<Title>? titles;
    private Collection<Novelty>? novelties;
    private Dictionary<int, List<UnlockCriteriaContext<AchievementCategoryCriteria>>>? achievementCriteriaByAchievementId;
    private Dictionary<int, AchievementCategory>? achievementCategoryByAchievementId;
    private Dictionary<int, string>? skinLookup;
    private Dictionary<int, string>? itemLookup;
    private Dictionary<string, List<Edge>>? edgesByFrom;
    private Dictionary<string, List<Edge>>? edgesByTo;
    private ClassifyConfig? classifyConfig;

    private readonly List<CurrencyCriteria> commonCurrencies = [new CurrencyCriteria("Coin"), new CurrencyCriteria("Karma"), new CurrencyCriteria("Research Note")];
    private readonly List<string> itemsToIgnore = ["Spirit Shard", "Pile of Bloodstone Dust", "Dragonite Ore", "Empyreal Fragment"];

    private static ClassifyConfig CreateConfig()
    {
        return new ClassifyConfig
        {
            UnlockGroups =
            [
                new()
                {
                    Name = "Heart of Thorns",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Verdant Brink", UnlockCriteria = [
                            new ZoneCriteria("Verdant Brink"),
                            new CurrencyCriteria("Airship Part")
                            ] },
                        new() { Name = "Auric Basin", UnlockCriteria = [
                            new ZoneCriteria("Auric Basin"),
                            new CurrencyCriteria("Lump of Aurillium"),
                            new CraftingMaterialCriteria("Auric Ingot")
                            ] },
                        new() { Name = "Tangled Depths", UnlockCriteria = [
                            new ZoneCriteria("Tangled Depths"),
                            new CurrencyCriteria("Ley Line Crystal"),
                            new TokenCriteria("Chak Egg"),
                            ] },
                        new() { Name = "Dragon's Stand", UnlockCriteria = [ new ZoneCriteria("Dragon's Stand"), new TokenCriteria("Crystalline Ore"),
                            // some items in DS sell items for the other HoT currencies
                            new CurrencyCriteria("Airship Part"), new CurrencyCriteria("Lump of Aurillium"), new CurrencyCriteria("Ley Line Crystal")
                            ] },
                    ]
                },

                new()
                {
                    Name = "Path of Fire",
                    UnlockCriteria = [
                        new CraftingMaterialCriteria("Sliver of Twitching Forgemetal"),
                        new CraftingMaterialCriteria("Congealed Putrescence"),
                        new CraftingMaterialCriteria("Eye of Kormir"),
                        new CraftingMaterialCriteria("Ley-Infused Sand"),
                        new CraftingMaterialCriteria("Powdered Rose Quartz"),
                        new CraftingMaterialCriteria("Eye of Kormir"),
                        new CurrencyCriteria("Trade Contract"),
                        new CurrencyCriteria("Elegy Mosaic"),
                    ],
                    UnlockCategories =
                    [
                        new() {
                            Name = "Crystal Oasis",
                            UnlockCriteria = [
                                new ZoneCriteria("Crystal Oasis"), new CurrencyCriteria("Casino Coin")
                            ]
                        },
                        new() { Name = "Desert Highlands", UnlockCriteria = [ new ZoneCriteria("Desert Highlands") ] },
                        new() { Name = "Elon Riverlands", UnlockCriteria = [ new ZoneCriteria("Elon Riverlands") ] },
                        new() { Name = "The Desolation", UnlockCriteria = [ new ZoneCriteria("The Desolation") ] },
                        new() { Name = "Domain of Vabbi", UnlockCriteria = [ new ZoneCriteria("Domain of Vabbi") ] },
                    ]
                },

                new()
                {
                    Name = "End of Dragons",
                    UnlockCriteria = [ new CurrencyCriteria("Ancient Coin") ],
                    UnlockCategories =
                    [
                        new() { Name = "Seitung Province", UnlockCriteria = [ new ZoneCriteria("Seitung Province") ] },
                        new() { Name = "New Kaineng City", UnlockCriteria = [ new ZoneCriteria("New Kaineng City") ] },
                        new() { Name = "The Echovald Wilds", UnlockCriteria = [ new ZoneCriteria("The Echovald Wilds") ] },
                        new() { Name = "Arborstone", UnlockCriteria = [ new ZoneCriteria("Arborstone"), new CurrencyCriteria("Canach Coin") ] },
                        new() { Name = "Dragon's End", UnlockCriteria = [ new ZoneCriteria("Dragon's End") ] },
                        new() { Name = "Gyala Delve", UnlockCriteria = [ new ZoneCriteria("Gyala Delve") ] },
                    ]
                },

                new()
                {
                    Name = "Secrets of the Obscure",
                    UnlockCriteria = [ new CurrencyCriteria("Ancient Coin") ],
                    UnlockCategories =
                    [
                        new() { Name = "Skywatch Archipelago", UnlockCriteria = [ new ZoneCriteria("Skywatch Archipelago"), new CurrencyCriteria("Static Charge") ] },
                        new() { Name = "The Wizard's Tower", UnlockCriteria = [ new ZoneCriteria("The Wizard's Tower") ] },
                        new() { Name = "Amnytas", UnlockCriteria = [ new ZoneCriteria("Amnytas"), new CurrencyCriteria("Pinch of Stardust") ] },
                        new() { Name = "Inner Nayos", UnlockCriteria = [ new ZoneCriteria("Inner Nayos"), new CurrencyCriteria("Calcified Gasp") ] },
                    ]
                },

                new()
                {
                    Name = "Janthir Wilds",
                    UnlockCriteria = [ new CurrencyCriteria("Ancient Coin") ],
                    UnlockCategories =
                    [
                        new() { Name = "Lowland Shore",
                            UnlockCriteria = [
                                new ZoneCriteria("Lowland Shore"),
                                new TokenCriteria("Curious Lowland Honeycomb")
                            ]
                        },
                        new() { Name = "Janthir Syntri",
                            UnlockCriteria = [
                                new ZoneCriteria("Janthir Syntri"),
                                new TokenCriteria("Curious Mursaat Currency")
                            ]
                        },
                        new() { Name = "Mistburned Barrens",
                            UnlockCriteria = [
                                new ZoneCriteria("Mistburned Barrens"),
                                new TokenCriteria("Curious Mursaat Ruin Shard")
                            ]
                        },
                        new() { Name = "Bava Nisos",
                            UnlockCriteria = [
                                new ZoneCriteria("Bava Nisos"),
                                new TokenCriteria("Curious Mursaat Remnants")
                            ]
                        },
                    ]
                },

                new()
                {
                    Name = "Visions of Eternity",
                    UnlockCriteria = [ new CurrencyCriteria("Unusual Coin") ],
                    UnlockCategories =
                    [
                        new() { Name = "Shipwreck Strand",
                            UnlockCriteria = [
                                new ZoneCriteria("Shipwreck Strand"),
                                new CurrencyCriteria("Aether-Rich Sap"),
                                new TokenCriteria("Chromatic Sap")
                            ]
                        },
                        new() { Name = "Starlit Weald",
                            UnlockCriteria = [
                                new ZoneCriteria("Starlit Weald"),
                                new CurrencyCriteria("Antiquated Ducat"),
                                new TokenCriteria("Raw Enchanting Stone")
                            ]
                        },
                        new() { Name = "Eternity's Garden",
                            UnlockCriteria = [
                                new ZoneCriteria("Eternity's Garden"),
                                new TokenCriteria("Shadowstone Fragment")
                            ]
                        }                        
                    ]
                },

                new()
                {
                    Name = "LW Season 1",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Season 1", UnlockCriteria = [
                            new TokenCriteria("Found Heirloom"),
                            ] },
                    ]
                },

                new()
                {
                    Name = "LW Season 2",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Dry Top", UnlockCriteria = [ new ZoneCriteria("Dry Top"), new CurrencyCriteria("Unidentified Fossilized Insect") ] },
                        new() { Name = "The Silverwastes", UnlockCriteria = [ new ZoneCriteria("The Silverwastes") ] },
                    ]
                },

                new()
                {
                    Name = "LW Season 3",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Bloodstone Fen",
                            UnlockCriteria = [
                                new ZoneCriteria("Bloodstone Fen"),
                                new TokenCriteria("Blood Ruby")
                            ]
                        },
                        new() { Name = "Ember Bay",
                            UnlockCriteria = [
                                new ZoneCriteria("Ember Bay"),
                                new TokenCriteria("Petrified Wood")
                            ]
                        },
                        new() { Name = "Bitterfrost Frontier",
                            UnlockCriteria = [
                                new ZoneCriteria("Bitterfrost Frontier"),
                                new TokenCriteria("Fresh Winterberry")
                            ]
                        },
                        new() { Name = "Lake Doric",
                            UnlockCriteria = [
                                new ZoneCriteria("Lake Doric"),
                                new TokenCriteria("Jade Shard")
                            ]
                        },
                        new() { Name = "Draconis Mons",
                            UnlockCriteria = [
                                new ZoneCriteria("Draconis Mons"),
                                new TokenCriteria("Fire Orchid Blossom")
                            ]
                        },
                        new() { Name = "Siren's Landing",
                            UnlockCriteria = [
                                new ZoneCriteria("Siren's Landing"),
                                new TokenCriteria("Orrian Pearl")
                            ]
                        },
                    ]
                },

                new()
                {
                    Name = "LW Season 4",
                    UnlockCriteria = [
                        new CurrencyCriteria("Volatile Magic")
                    ],
                    UnlockCategories =
                    [
                        new() { Name = "Domain of Istan",
                            UnlockCriteria = [
                                new ZoneCriteria("Domain of Istan"),
                                new TokenCriteria("Kralkatite Ore")
                            ]
                        },
                        new() { Name = "Sandswept Isles",
                            UnlockCriteria = [
                                new ZoneCriteria("Sandswept Isles"),
                                new TokenCriteria("Difluorite Crystal")
                            ]
                        },
                        new() { Name = "Domain of Kourna",
                            UnlockCriteria = [
                            new ZoneCriteria("Domain of Kourna"),
                            new TokenCriteria("Inscribed Shard")
                            ]
                        },
                        new() { Name = "Jahai Bluffs",
                            UnlockCriteria = [
                                new ZoneCriteria("Jahai Bluffs"),
                                new TokenCriteria("Lump of Mistonium")
                            ]
                        },
                        new() { Name = "Thunderhead Peaks",
                            UnlockCriteria = [
                                new ZoneCriteria("Thunderhead Peaks"),
                                new TokenCriteria("Branded Mass")
                            ]
                        },
                        new() { Name = "Dragonfall",
                            UnlockCriteria = [
                                new ZoneCriteria("Dragonfall"),
                                new TokenCriteria("Mistborn Mote")
                            ]
                        },
                    ]
                },

                new()
                {
                    Name = "Icebrood Saga",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Grothmar Valley",
                            UnlockCriteria = [
                                new ZoneCriteria("Grothmar Valley"),
                                new TokenCriteria("Hatched Chili")
                            ]
                        },
                        new() { Name = "Bjora Marches",
                            UnlockCriteria = [
                                new ZoneCriteria("Bjora Marches"),
                                new TokenCriteria("Eternal Ice Shard")
                            ]
                        },
                        new() { Name = "Drizzlewood Coast",
                            UnlockCriteria = [
                                new ZoneCriteria("Drizzlewood Coast")
                            ]
                        },
                    ]
                },

                new()
                {
                    Name = "Dungeons",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Ascalonian Catacombs", UnlockCriteria = [  ] },
                        new() { Name = "Caudecus's Manor", UnlockCriteria = [  ] },
                        new() { Name = "Twilight Arbor", UnlockCriteria = [  ] },
                        new() { Name = "Sorrow's Embrace", UnlockCriteria = [  ] },
                        new() { Name = "Citadel of Flame", UnlockCriteria = [  ] },
                        new() { Name = "Honor of the Waves", UnlockCriteria = [  ] },
                        new() { Name = "Crucible of Eternity", UnlockCriteria = [  ] },
                        new() { Name = "The Ruined City of Arah", UnlockCriteria = [  ] },
                    ]
                },

                new()
                {
                    Name = "Raid Encounters",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Old Lion's Court", UnlockCriteria = [  ] },
                        new() { Name = "Shiverpeaks Pass", UnlockCriteria = [  ] },
                        new() { Name = "Voice of the Fallen and Claw of the Fallen", UnlockCriteria = [  ] },
                        new() { Name = "Fraenir of Jormag", UnlockCriteria = [  ] },
                        new() { Name = "Boneskinner", UnlockCriteria = [  ] },
                        new() { Name = "Whisper of Jormag", UnlockCriteria = [  ] },
                        new() { Name = "Forging Steel", UnlockCriteria = [  ] },
                        new() { Name = "Cold War", UnlockCriteria = [  ] },
                        new() { Name = "Aetherblade Hideout", UnlockCriteria = [  ] },
                        new() { Name = "Xunlai Jade Junkyard", UnlockCriteria = [  ] },
                        new() { Name = "Kaineng Overlook", UnlockCriteria = [  ] },
                        new() { Name = "Harvest Temple", UnlockCriteria = [  ] },
                        new() { Name = "Cosmic Observatory", UnlockCriteria = [  ] },
                        new() { Name = "Temple of Febe", UnlockCriteria = [  ] },
                        new() { Name = "Guardian's Glade", UnlockCriteria = [  ] },
                    ]
                },

                new()
                {
                    Name = "Raids",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Spirit Vale", UnlockCriteria = [  ] },
                        new() { Name = "Salvation Pass", UnlockCriteria = [  ] },
                        new() { Name = "Stronghold of the Faithful", UnlockCriteria = [  ] },
                        new() { Name = "Bastion of the Penitent", UnlockCriteria = [  ] },
                        new() { Name = "Hall of Chains", UnlockCriteria = [  ] },
                        new() { Name = "Mythwright Gambit", UnlockCriteria = [  ] },
                        new() { Name = "The Key of Ahdashim", UnlockCriteria = [  ] },
                    ]
                },

                new()
                {
                    Name = "PvP / WvW",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "PvP", UnlockCriteria = [
                            new TokenCriteria("Shards of Glory"),
                        ] },
                        new() { Name = "WvW", UnlockCriteria = [
                            new TokenCriteria("Memories of Battle"),
                        ] },
                    ]
                },

                new()
                {
                    Name = "Festivals",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Lunar New Year", UnlockCriteria = [
                            new TokenCriteria("Token of the Dragon Ball Champion"),
                            new TokenCriteria("Token of the Celestial Champion"),
                            new AchievementCategoryCriteria("Lunar New Year Dailies"),
                            new AchievementCategoryCriteria("New Year's Customs"),
                            new AchievementCategoryCriteria("Dragon Ball"),
                            ] },
                        new() { Name = "Super Adventure Box", UnlockCriteria = [
                            new TokenCriteria("Bauble"),
                            new TokenCriteria("Bauble Bubble"),
                            new TokenCriteria("Crimson Assassin Token"),
                            new TokenCriteria("King Toad Z-1"),
                            new TokenCriteria("King Toad Z-2"),
                            new TokenCriteria("King Toad Z-3"),
                            new TokenCriteria("Storm Wizard Z-1"),
                            new TokenCriteria("Storm Wizard Z-2"),
                            new TokenCriteria("Storm Wizard Z-3"),
                            new AchievementCategoryCriteria("Super Adventure Box: World 1"),
                            new AchievementCategoryCriteria("Super Adventure Box: World 2"),
                            new AchievementCategoryCriteria("Super Adventure Box: Quality Testing"),
                            new AchievementCategoryCriteria("Super Adventure Box: Tribulation Mode"),
                            new AchievementCategoryCriteria("Super Adventure Box: Nostalgia"),
                            ] },
                        new() { Name = "Dragon Bash", UnlockCriteria = [
                            new TokenCriteria("Piece of Zhaitaffy"),
                            new TokenCriteria("Jorbreaker"),
                            new AchievementCategoryCriteria("Dragon Bash"),
                            new AchievementCategoryCriteria("Dragon Bash Feats"),
                            ] },
                        new() { Name = "Festival of the Four Winds", UnlockCriteria = [
                            new TokenCriteria("Festival Token"),
                            new TokenCriteria("Favor of the Festival"),
                            new AchievementCategoryCriteria("Festival of the Four Winds"),
                            new AchievementCategoryCriteria("Crown Pavilion"),
                            new AchievementCategoryCriteria("The Queen's Gauntlet"),
                            new AchievementCategoryCriteria("Four Winds Customs"),
                            ] },
                        new() { Name = "Halloween", UnlockCriteria = [
                            new TokenCriteria("Piece of Candy Corn"),
                            new TokenCriteria("Chattering Skull"),
                            new TokenCriteria("Nougat Center"),
                            new TokenCriteria("Plastic Fangs"),
                            new TokenCriteria("Candy Corn Cob"),
                            new TokenCriteria("Gibbering Skull"),
                            new TokenCriteria("Tyria's Best Nougat Center"),
                            new TokenCriteria("High-Quality Plastic Fangs"),
                            new TokenCriteria("Tattered Bat Wing"),
                            new AchievementCategoryCriteria("Halloween Rituals"),
                            new AchievementCategoryCriteria("Shadow of the Mad King"),
                            new AchievementCategoryCriteria("Lunatic Wardrobe"),
                            ] },
                        new() { Name = "Wintersday", UnlockCriteria = [
                            new TokenCriteria("Snow Diamond"),
                            new TokenCriteria("Snowflake"),
                            new AchievementCategoryCriteria("Wintersday Traditions"),
                            new AchievementCategoryCriteria("The Wondrous Workshop of Toymaker Tixx"),
                            new AchievementCategoryCriteria("Winter's Presence"),
                            ] },
                    ]
                },

                new()
                {
                    Name = "Cities",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Divinity's Reach", UnlockCriteria = [  ] },
                        new() { Name = "The Grove", UnlockCriteria = [  ] },
                        new() { Name = "Hoelbrak", UnlockCriteria = [  ] },
                        new() { Name = "Rata Sum", UnlockCriteria = [  ] },
                        new() { Name = "Black Citadel", UnlockCriteria = [  ] },
                        new() { Name = "Lion's Arch", UnlockCriteria = [  ] },
                        new() { Name = "Eye of the North", UnlockCriteria = [  ] },
                    ]
                },

                new()
                {
                    Name = "Other",
                    UnlockCriteria = [  ],
                    UnlockCategories =
                    [
                        new() { Name = "Elite Specializations", UnlockCriteria = [  ] },
                        new() { Name = "Guild", UnlockCriteria = [  ] },
                        new() { Name = "Mystic Forge", UnlockCriteria = [  ] },
                        new() { Name = "Crafting", UnlockCriteria = [  ] },
                        new() { Name = "Black Lion Claim Ticket", UnlockCriteria = [
                            new TokenCriteria("Black Lion Claim Ticket", false),
                            new AchievementCategoryCriteria("Black Lion Collections"),
                            ] },
                        new() { Name = "Black Lion Statuette", UnlockCriteria = [
                            new TokenCriteria("Black Lion Statuette", false),
                            ] },
                        new() { Name = "Gathering Tools", UnlockCriteria = [  ] },
                        new() { Name = "Gem Store", UnlockCriteria = [
                            new CurrencyCriteria("Gem", false)                         
                            ] },
                        new() { Name = "General", UnlockCriteria = [  ] },
                        new() { Name = "Fractals", UnlockCriteria = [
                            new TokenCriteria("Fractal Research Page"),
                            new TokenCriteria("Golden Fractal Relic"),
                            new TokenCriteria("Integrated Fractal Matrix"),
                            new TokenCriteria("Stabilizing Matrix"),
                            new AchievementCategoryCriteria("Fractals of the Mists"),
                            ] },
                        new() { Name = "Wizard's Vault", UnlockCriteria = [  ] },
                    ]
                },
            ]
        };
    }

    private readonly List<string> containersToIgnore = [
        "Cold-Forged Exotic Weapon",
        "Unidentified Gear"
        ];

    private IEnumerable<UnlockCriteriaContext<TokenCriteria>>? tokenCriteria;
    private IEnumerable<UnlockCriteriaContext<CurrencyCriteria>>? currencyCriteriaWithoutZoneSpecification;
    private IEnumerable<UnlockCriteriaContext<TokenCriteria>>? tokenCriteriaWithoutZoneSpecification;
    private IEnumerable<UnlockCriteriaContext<CraftingMaterialCriteria>>? craftingMaterialCriteria;

    public async Task<ClassifyConfig> ClassifyUnlocks(CancellationToken cancellationToken, params string[] unlocksToLookup)
    {
        await Init(cancellationToken);

        var nodes = graph!.Nodes.ToList();
        if (unlocksToLookup != null && unlocksToLookup.Length > 0)
        {
            nodes = [.. nodes.Where(n => unlocksToLookup.Contains(n.Key, StringComparer.OrdinalIgnoreCase))];
        }
        var achievementNodes = nodes.Where(n => n.Value.Type == NodeType.Achievement);
        if (unlocksToLookup != null && unlocksToLookup.Length > 0)
        {
            achievementNodes = [.. achievementNodes.Where(n => unlocksToLookup.Contains(n.Key, StringComparer.OrdinalIgnoreCase))];
        }

        int iNode = 0;
        foreach (var (key, node) in nodes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Cancellation requested, returning partial nodes result.");
                break;
            }

            if (!ShouldClassify(node))
            {
                logger.LogDebug("Skipping node {key} ({type})", key, node.Type);
                iNode++;
                continue;
            }
            logger.LogInformation("Finding node {key} ({type})", key, node.Type);

            ClassifyUnlock(key);

            iNode++;
            logger.LogInformation("node {progress}/{total}", iNode, nodes.Count);
        }

        var unlocksFromGraph = classifyConfig!.GetUnlocks().ToList();
        var unlockByName = unlocksFromGraph.ToDictionary(u => u.Unlock.Name);

        int iAchi = 0;
        foreach (var (keyAchi, nodeAchi) in achievementNodes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Cancellation requested, returning partial achi result.");
                break;
            }

            if (!nodeAchi.Metadata.TryGetValue("achievementId", out var achievementId) || !int.TryParse(achievementId, out var achievementIdInt))
            {
                logger.LogDebug("Skipping achi {key} ({type})", keyAchi, nodeAchi.Type);
                continue;
            }
            logger.LogInformation("Finding node {key} ({type})", keyAchi, nodeAchi.Type);

            ClassifyAchievement(unlockByName, keyAchi, nodeAchi, achievementIdInt);
            iAchi++;
            logger.LogInformation("achi {progress}/{total}", iAchi, nodes.Count);
        }

        foreach (var group in classifyConfig!.UnlockGroups)
        {

            logger.LogInformation("Group: {groupName} ({unlockCount} unlocks)", group.Name, group.Unlocks.Count);
            foreach (var unlock in group.Unlocks)
            {
                FillInApiData(unlock);
            }
            foreach (var category in group.UnlockCategories)
            {
                logger.LogInformation("  Category: {categoryName} ({unlockCount} unlocks)", category.Name, category.Unlocks.Count);
                foreach (var unlock in category.Unlocks)
                {
                    FillInApiData(unlock);
                }
            }
        }

        return classifyConfig;
    }

    private void ClassifyAchievement(Dictionary<string, UnlockContext> unlocksByName, string keyAchi, Node nodeAchi, int achievementIdInt)
    {
        List<Categorization> possibleClassifications = [];
        var achievement = achievements!.SingleOrDefault(a => a.Id == achievementIdInt);
        if (achievementCriteriaByAchievementId!.TryGetValue(achievementIdInt, out var foundAchievementCategoryCriteria))
        {
            possibleClassifications.AddRange(foundAchievementCategoryCriteria.Select(c => c.Categorization).OfType<Categorization>());
        }
        else if (achievement != null && achievement.Bits != null)
        {
            var skinIds = achievement.Bits.OfType<AchievementSkinBit>().Select(b => b.Id).ToList();
            var skinNames = skinIds
                .Select(skinId =>
                    skinLookup!.TryGetValue(skinId, out var skinKey)
                        ? skinKey
                        : null)
                .Where(x => x is not null)
                .ToHashSet();

            possibleClassifications.AddRange(unlocksByName.Where(kvp => skinNames.Contains(kvp.Key)).Select(kvp => kvp.Value.Categorization).OfType<Categorization>());
        }

        var bestClassificationGroup = possibleClassifications
            .GroupBy(x => x)
            .Select(g => new
            {
                g.Key,
                Count = g.Count(),
                Items = g.ToList()
            })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();
        if (bestClassificationGroup == null)
        {
            return;
        }
        Categorization bestClassification = bestClassificationGroup.Key;
        
        List<Unlock> unlocksToClassify = [];
        if (achievement?.Rewards != null)
        {
            var itemIds = achievement.Rewards.OfType<ItemReward>().Where(ir => ir != null).Select(ir => ir.Id);
            var itemNames = itemIds
                .Select(itemId =>
                    itemLookup!.TryGetValue(itemId, out var itemKey)
                        ? itemKey
                        : null)
                .OfType<string>();

            unlocksToClassify = [.. itemNames
                .Select(itemName =>
                {
                    var edges = edgesByTo!.TryGetValue(itemName, out var e) ? e : [];
                    var skins = edges.Where(e => e.Type == EdgeType.SkinUnlock).Select(e => e.From);
                    
                    return skins.Select(s => {
                        var node = graph!.GetNode(s);
                        if(node != null){
                            return new Unlock(s, node);
                        }
                        return null;
                    })
                    .OfType<Unlock>()
                    .ToList();
                })
                .SelectMany(x => x)
                .ToList()];
        }

        unlocksToClassify.Add(new Unlock(keyAchi, nodeAchi));

        foreach (var unlock in unlocksToClassify.Distinct())
        {
            if (bestClassification.Group != null)
            {
                Categorize(bestClassification.Group.Name, null, unlock.Name, unlock.Node);
            }
            else if (bestClassification.GroupOfCategoryName != null && bestClassification.Category != null)
            {
                Categorize(bestClassification.GroupOfCategoryName, bestClassification.Category.Name, unlock.Name, unlock.Node);
            }
        }
    }

    private void ClassifyUnlock(string unlock)
    {
        var result = Classify(graph!, unlock);
        if (result != null)
        {
            var zone = result.Value;
            logger.LogInformation("{zone.Key} ({zone.Node.Type})", zone.Key, zone.Node?.Type);

            logger.LogInformation("Path:");
            foreach (var step in zone.Path)
            {
                logger.LogInformation("  -> {step}", step);
            }
        }
    }

    private async Task Init(CancellationToken cancellationToken)
    {
        graph ??= await wikiProcessingSource.GetAcquisitionGraph(cancellationToken);
        miniatures ??= await apiSource.GetMiniaturesAsync(cancellationToken);
        items ??= await apiSource.GetItemsAsync(cancellationToken);
        skins ??= await apiSource.GetSkinsAsync(cancellationToken);
        achievements ??= await apiSource.GetAchievementsAsync(cancellationToken);
        achievementCategories ??= await apiSource.GetAchievementCategoriesAsync(cancellationToken);
        titles ??= await apiSource.GetTitlesAsync(cancellationToken);
        novelties ??= await apiSource.GetNoveltiesAsync(cancellationToken);

        classifyConfig = CreateConfig();

        var zoneData = await wikiProcessingSource.GetZoneData(cancellationToken);

        foreach (var group in classifyConfig.UnlockGroups)
        {
            foreach (var category in group.UnlockCategories)
            {
                var foundCategory = zoneData.Zones.SingleOrDefault(z => category.Name == z.Name);
                if (foundCategory != null)
                {
                    foreach (var achievementCategory in foundCategory.AchievementCategories)
                    {
                        category.UnlockCriteria.Add(new AchievementCategoryCriteria(achievementCategory));
                    }
                }
            }
        }

        tokenCriteria = classifyConfig.GetUnlockCriteriaWithContext<TokenCriteria>();
        currencyCriteriaWithoutZoneSpecification = classifyConfig.GetUnlockCriteriaWithContext<CurrencyCriteria>()?.Where(c => !c.Criteria.UsedInZoneSpecification);
        tokenCriteriaWithoutZoneSpecification = classifyConfig.GetUnlockCriteriaWithContext<TokenCriteria>()?.Where(c => !c.Criteria.UsedInZoneSpecification);
        craftingMaterialCriteria = classifyConfig.GetUnlockCriteriaWithContext<CraftingMaterialCriteria>();
        var achievementCategoryCriteria = classifyConfig.GetUnlockCriteriaWithContext<AchievementCategoryCriteria>();

        achievementCriteriaByAchievementId = [];
        achievementCategoryByAchievementId = [];
        foreach (var category in achievementCategories)
        {
            var matchingCriteria = achievementCategoryCriteria
                .Where(c => c.Criteria.Matches(category.Name))
                .ToList();



            foreach (var achievement in category.Achievements)
            {
                achievementCategoryByAchievementId[achievement.Id] = category;

                if (matchingCriteria.Count == 0)
                    continue;

                if (!achievementCriteriaByAchievementId.TryGetValue(achievement.Id, out var list))
                {
                    list = [];
                    achievementCriteriaByAchievementId[achievement.Id] = list;
                }

                list.AddRange(matchingCriteria);
            }
        }

        skinLookup = [];
        itemLookup = [];
        foreach (var kvp in graph.Nodes)
        {
            var key = kvp.Key;
            var node = kvp.Value;

            var nodetypes = new List<NodeType> { NodeType.Item, NodeType.BackItem, NodeType.Weapon, NodeType.Armor, NodeType.Container };

            if (node.Type == NodeType.Skin &&
                node.Metadata.TryGetValue("id", out var skinId) &&
                !string.IsNullOrWhiteSpace(skinId) &&
                int.TryParse(skinId, out var skinIdInt))
            {
                skinLookup[skinIdInt] = key;
            }
            else if (nodetypes.Contains(node.Type) &&
                node.Metadata.TryGetValue("id", out var itemId) &&
                !string.IsNullOrWhiteSpace(itemId) &&
                int.TryParse(itemId, out var itemIdInt))
            {
                itemLookup[itemIdInt] = key;
            }
        }

        edgesByFrom = graph.Edges
            .GroupBy(x => x.From)
            .ToDictionary(g => g.Key, g => g.ToList());
        edgesByTo = graph.Edges
            .GroupBy(x => x.To)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static bool ShouldClassify(Node node) =>
    node switch
    {
        {
            Type: NodeType.Skin,
            Metadata: var metadata
        } when metadata.TryGetValue("type", out var type) &&
               (
                   !type.Equals("Fishing Rod", StringComparison.OrdinalIgnoreCase) &&
                   !type.Equals("Mining", StringComparison.OrdinalIgnoreCase) &&
                   !type.Equals("Logging", StringComparison.OrdinalIgnoreCase) &&
                   !type.Equals("Foraging", StringComparison.OrdinalIgnoreCase)
               )
            => true,
        {
            Type: NodeType.Item,
            Metadata: var metadata
        } when metadata.TryGetValue("type", out var type) &&
               (
                   type.Equals("miniature", StringComparison.OrdinalIgnoreCase) ||
                   type.Contains("Novelty", StringComparison.OrdinalIgnoreCase)
               )
            => true,

        // ❌ Everything else
        _ => false
    };

    private readonly List<string> debugtitles = [
        //"Plush Zhaia Backpack (skin)",
        //"Plush Zhaia Backpack",
        //"Halloween Vendor",
        //"Hooligan's Route",
        //"Lion's Arch",
        //"Tarir, the Forgotten City",
        //"Auric Basin",
        //"Axe of the Dragon's Deep",
        //"Arah Weapons Box",
        //"Dungeon Weapon Container",
        //"Dragon's Stand"
        //"Piles of Bloodstone Dust",
        //"Tome of the Rubicon",
        //"Mistborn Mote",
        "Funerary Axe (skin)"
    ];

    private sealed record SearchState(
        string Key,
        string? Cost,
        EdgeType? IncomingEdgeType);

    private void FillInApiData(Unlock unlock)
    {
        var node = unlock.Node;
        var startKey = unlock.Name;
        logger.LogInformation("getting API data for {key} ({type})", startKey, node.Type);
        if (miniatures == null || skins == null || achievements == null || achievementCategories == null
            || titles == null || novelties == null || items == null || achievementCategoryByAchievementId == null)
        {
            logger.LogWarning("API data not initialized when trying to get API data for {key} ({type})", startKey, node.Type);
            return;
        }

        object? result = null;
        if (node.Type == NodeType.Item && node.Metadata.TryGetValue("type", out var metadataTypeMini) && metadataTypeMini.Equals("miniature", StringComparison.OrdinalIgnoreCase))
        {
            if(node.Metadata.TryGetValue("miniature id", out var miniId) && !string.IsNullOrEmpty(miniId) && int.TryParse(miniId, out var miniIdInt))
            {
                result = miniatures.SingleOrDefault(m => m.Id == miniIdInt);
            }
            else
            {
                var matchingMiniature = miniatures.FirstOrDefault(m => m.Name.Equals(startKey, StringComparison.Ordinal));
                if (matchingMiniature != null)
                {
                    result = matchingMiniature;
                }
            }

        }

        if (node.Type == NodeType.Item && node.Metadata.TryGetValue("type", out var metadataTypeNovelty) && metadataTypeNovelty.Contains("novelty", StringComparison.OrdinalIgnoreCase)
            && node.Metadata.TryGetValue("novelty-id", out var noveltyId) && !string.IsNullOrEmpty(noveltyId) && int.TryParse(noveltyId, out var noveltyIdInt))
        {
            result = novelties.SingleOrDefault(m => m.Id == noveltyIdInt);
        }

        if (node.Type == NodeType.Skin && node.Metadata.TryGetValue("id", out var skinId) && !string.IsNullOrEmpty(skinId) && int.TryParse(skinId, out var skinIdInt))
        {
            result = skins.SingleOrDefault(i => i.Id == skinIdInt);
        }

        if (node.Type == NodeType.Achievement && node.Metadata.TryGetValue("achievementId", out var achievementId) && !string.IsNullOrEmpty(achievementId) && int.TryParse(achievementId, out var achievementIdInt))
        {
            var achievement = achievements.SingleOrDefault(i => i.Id == achievementIdInt);
            string? rewardName = null;
            string? rewardIcon = null;
            if (achievement?.Rewards != null) {
                var reward = (achievement.Rewards is { Count: > 0 }) ? achievement.Rewards[0] : null;
                if (reward != null)
                {
                    switch (reward)
                    {
                        case TitleReward titleReward:
                            rewardName = titles.FirstOrDefault(t => titleReward.Id == t.Id)?.Name;
                            rewardIcon = "/img/icon_title.png";
                            break;
                        case MasteryPointReward masteryReward:
                            rewardIcon = $"/img/mastery_{masteryReward.Region}.png";
                            break;
                        case ItemReward itemReward:
                            var item = items.SingleOrDefault(i => i.Id == itemReward.Id);
                            rewardName = item?.Name;
                            rewardIcon = item?.IconUrl?.ToString();
                            break;
                    }
                }
            }
            var category = achievementCategoryByAchievementId.TryGetValue(achievementIdInt, out var cat) ? cat : null;
            if(achievement != null)
                result = new AchievementWithReward(achievement, rewardIcon, rewardName, category?.IconUrl);
        }

        if (result == null)
        {
            logger.LogWarning("No API data found for {key} ({type})", startKey, node.Type);
            return;
        }

        unlock.ApiData = result;
    }

    private (string? Key, Node? Node, List<string> Path)? Classify(
        AcquisitionGraph graph,
        string startKey)
    {
        var visited = new Dictionary<string, HashSet<string?>>();
        var queue = new Queue<SearchState>();
        var parent = new Dictionary<string, string?>();
        var startNode = graph.GetNode(startKey);
        List<PossibleClassification> possibleClassifications = [];
        var craftingCandidates = new List<string>();

        if (startNode == null)
            return null;

        var startState = new SearchState(startKey, null, null);
        TryVisit(visited, startKey, null);
        queue.Enqueue(startState);
        parent[startKey] = null;

        while (queue.Count > 0)
        {
            var searchState = queue.Dequeue();
            var currentKey = searchState.Key;
            var current = graph.GetNode(currentKey);

            if (debugtitles.Contains(currentKey))
            {
                logger.LogInformation("Visiting {key} ({type}) with cost {cost}", currentKey, current?.Type, searchState.Cost);
            }

            if (current == null)
                continue;

            if (searchState.IncomingEdgeType == EdgeType.GatheredFrom
                && current.Type == NodeType.Gw2Object && current.Metadata.TryGetValue("type", out var objectType) && objectType != "chest")
            {
                continue;
            }

            if (current.Type == NodeType.Item && itemsToIgnore.Contains(currentKey, StringComparer.Ordinal))
            {
                continue;
            }

            if (current.Type == NodeType.Item && current.Metadata.TryGetValue("type", out var itemType))
            {
                var isCraftingMaterial = itemType.Equals("crafting material", StringComparison.OrdinalIgnoreCase);
                var isThrophy = itemType.Equals("trophy", StringComparison.OrdinalIgnoreCase);
                var isInMaterialStorage = current.Metadata.TryGetValue("material storage", out var materialStorage) && !string.IsNullOrWhiteSpace(materialStorage);
                var isMysticMaterial = current.Metadata.TryGetValue("material type", out var materialType) && materialType.Equals("mystic", StringComparison.OrdinalIgnoreCase);
                if ((isCraftingMaterial || isThrophy) && (isInMaterialStorage || isMysticMaterial ))
                {
                    var matchingCraftingMaterials = craftingMaterialCriteria!.Where(c => c.Criteria.Matches(currentKey)).ToList();
                    if (matchingCraftingMaterials.Count == 0)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (var criteria in matchingCraftingMaterials)
                        {
                            var groupName = criteria.Categorization!.Group?.Name;
                            var categoryName = criteria.Categorization!.Category?.Name ?? "";
                            var groupOfCategoryName = criteria.Categorization!.GroupOfCategoryName ?? "";
                            if (groupName != null)
                            {
                                possibleClassifications.Add(new(groupName, null, BuildPath(currentKey, parent), 100));
                            }
                            else
                            {
                                possibleClassifications.Add(new(groupOfCategoryName, categoryName, BuildPath(currentKey, parent), 100));
                            }
                        }
                        continue;
                    }
                }
                if (itemType.Equals("Container", StringComparison.OrdinalIgnoreCase))
                {
                    if (containersToIgnore.Any(c => currentKey.Contains(c, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                }
            }

            if(current.Type == NodeType.NPC && searchState.Cost != null)
            {
                var cost = searchState.Cost;
                var festivals = classifyConfig!.UnlockGroups.Single(g => g.Name == "Festivals");
                foreach (var category in festivals.UnlockCategories)
                {
                    var validTokens = category.UnlockCriteria
                        .OfType<TokenCriteria>().ToList();

                    if (validTokens.Any(t => t.MatchesCost(cost)))
                    {
                        possibleClassifications.Add(new(festivals.Name, category.Name, BuildPath(currentKey, parent), 100));
                    }
                }

            }

            // When we reach a zone/city → validate cost
            if (current.Type == NodeType.Location &&
                current.Metadata.TryGetValue("type", out var value) &&
                (value.Equals("Zone", StringComparison.OrdinalIgnoreCase) ||
                 value.Equals("City", StringComparison.OrdinalIgnoreCase)))
            {
                var zone = currentKey;
                foreach (var group in classifyConfig!.UnlockGroups)
                {
                    foreach (var category in group.UnlockCategories)
                    {
                        // Does the category have a ZoneCriteria that matches this zone?
                        var hasZone = category.UnlockCriteria
                            .OfType<ZoneCriteria>()
                            .Any(z => z.Matches(zone));

                        if (!hasZone)
                            continue;


                        // Does the category have a CurrencyCriteria that matches this currency?
                        var validCurrencies = category.UnlockCriteria
                            .OfType<CurrencyCriteria>().Concat(group.UnlockCriteria.OfType<CurrencyCriteria>()).Concat(commonCurrencies).ToList();
                        var validTokens = category.UnlockCriteria
                            .OfType<TokenCriteria>().Concat(group.UnlockCriteria.OfType<TokenCriteria>()).ToList();

                        var cost = searchState.Cost;
                        if (cost == null || (validCurrencies.Count == 0 && validTokens.Count == 0))
                        {
                            possibleClassifications.Add(new(group.Name, category.Name, BuildPath(currentKey, parent), 80));
                        }
                        else
                        {
                            var countOfValidCurrencies = validCurrencies.Count(c => c.Matches(cost));
                            var countofValidTokens = validTokens.Count(t => t.MatchesCost(cost));
                            possibleClassifications.Add(new(group.Name, category.Name, BuildPath(currentKey, parent), 80 + countOfValidCurrencies * 5 + countofValidTokens * 5));
                        }
                    }
                }
            }

            var edges = edgesByFrom!.TryGetValue(currentKey, out var e) ? e : [];
            var foundTokenCriteria = tokenCriteria!.Where(c => edges.Any(e => c.Criteria.Matches(e.To))).ToList();
            foreach (var criteria in foundTokenCriteria)
            {
                var groupName = criteria.Categorization!.Group?.Name;
                var categoryName = criteria.Categorization!.Category?.Name ?? "";
                var groupOfCategoryName = criteria.Categorization!.GroupOfCategoryName ?? "";
                if (groupName != null)
                {
                    possibleClassifications.Add(new(groupName, null, BuildPath(currentKey, parent), 100));
                }
                else
                {
                    possibleClassifications.Add(new(groupOfCategoryName, categoryName, BuildPath(currentKey, parent), 100));
                }
            }

            var isCurrentAToken = tokenCriteria!.Any(c => c.Criteria.Matches(currentKey));
            foreach (var edge in edges)
             {
                if (edge.Type == EdgeType.HasIngredient && edge.Metadata != null
                    &&
                    (edge.Metadata.TryGetValue("discipline", out var discipline) && !string.IsNullOrWhiteSpace(discipline)
                    || edge.Metadata.TryGetValue("disciplines", out var disciplines) && !string.IsNullOrWhiteSpace(disciplines))
                    )
                {
                    craftingCandidates.Add(edge.To);
                }

                if (edge.Type == EdgeType.HasIngredient && edge.Metadata != null
                    && edge.Metadata.TryGetValue("source", out var recipeSource)
                    && recipeSource.Equals("mystic forge", StringComparison.OrdinalIgnoreCase))
                {
                    edge.Metadata.TryGetValue("type", out var recipeType);
                    if (recipeType == null || !recipeType.Equals("promotion", StringComparison.OrdinalIgnoreCase))
                    {
                        possibleClassifications.Add(new("Other", "Mystic Forge", BuildPath(currentKey, parent), 70));
                    }
                }

                if((edge.Type == EdgeType.ContainedIn || edge.Type == EdgeType.SoldBy) && isCurrentAToken)
                {
                    continue;
                }


                var nextCost = searchState.Cost;
                // If SoldBy → capture cost
                if (edge.Type == EdgeType.SoldBy &&
                    edge.Metadata != null && edge.Metadata.TryGetValue("cost", out var cost))
                {
                    nextCost = cost; // overwrite or store

                    var foundCurrencyCriteriaWithoutZoneSpecification = currencyCriteriaWithoutZoneSpecification!.Where(c => c.Criteria.Matches(cost));
                    var foundTokenCriteriaWithoutZoneSpecification = tokenCriteriaWithoutZoneSpecification!.Where(c => c.Criteria.MatchesCost(cost));
                    var combinedCriteria = foundCurrencyCriteriaWithoutZoneSpecification.Select(x => new UnlockCriteriaContext<UnlockCriteria>(x.Criteria, x.Categorization))
                                            .Union(
                                                foundTokenCriteriaWithoutZoneSpecification.Select(x => new UnlockCriteriaContext<UnlockCriteria>(x.Criteria, x.Categorization))
                                            );
                
                    foreach (var criteria in combinedCriteria)
                    {
                        var groupName = criteria.Categorization!.Group?.Name;
                        var categoryName = criteria.Categorization!.Category?.Name ?? "";
                        var groupOfCategoryName = criteria.Categorization!.GroupOfCategoryName ?? "";
                        if (groupName != null)
                        {
                            possibleClassifications.Add(new(groupName, null, BuildPath(currentKey, parent), 100));
                            continue;
                        }
                        else
                        {
                            possibleClassifications.Add(new(groupOfCategoryName, categoryName, BuildPath(currentKey, parent), 100));
                            continue;
                        }
                    }
                }

                var nextState = new SearchState(
                    edge.To,
                    nextCost,
                    edge.Type);

                if (TryVisit(visited, edge.To, nextCost))
                {
                    parent[edge.To] = currentKey;

                    queue.Enqueue(nextState);
                }
            }

            if (current.Type == NodeType.Weapon && current.Metadata.TryGetValue("IsNamedExoticWeapon", out var rarity) && rarity.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                possibleClassifications.Add(new("Other", "General", BuildPath(currentKey, parent), 60));
            }
        }

        if (craftingCandidates.Count > 0)
        {
            possibleClassifications.Add(new("Other", "Crafting", [startKey], 50));
        }

        var orderedMatches = possibleClassifications
            .GroupBy(x => new { x.Group, x.Category })
            .Select(g => new
            {
                g.Key.Group,
                g.Key.Category,
                AverageCertainty = g.Average(x => x.CertaintyPercentage),
                Count = g.Count(),
                Items = g.ToList()
            })
            .OrderByDescending(x => x.AverageCertainty)
            .ThenByDescending(x => x.Count); // optional tie-breaker
        var bestMatch = orderedMatches.FirstOrDefault();

        if (bestMatch != null)
        {
            Categorize(bestMatch.Group, bestMatch.Category, startKey, startNode);
            return (startKey, startNode, bestMatch.Items.First().Path);
        }

        return null;
    }

    private static bool TryVisit(
        Dictionary<string, HashSet<string?>> visited,
        string key,
        string? cost)
    {
        if (!visited.TryGetValue(key, out var costs))
        {
            costs = [];
            visited[key] = costs;
        }

        return costs.Add(cost);
    }

    private void Categorize(string groupName, string? categoryName, string startKkey, Node startNode)
    {
        var group = classifyConfig!.UnlockGroups.Single(g => g.Name.Equals(groupName, StringComparison.Ordinal));
        if (categoryName == null)
        {
            if(!group.Unlocks.Any(u => u.Name.Equals(startKkey, StringComparison.Ordinal)))
                group.Unlocks.Add(new Unlock(startKkey, startNode));
        }
        else
        {
            var category = group.UnlockCategories.Single(c => c.Name.Equals(categoryName, StringComparison.Ordinal));
            if (!category.Unlocks.Any(u => u.Name.Equals(startKkey, StringComparison.Ordinal)))
                category.Unlocks.Add(new Unlock(startKkey, startNode));
        }
    }

    private static List<string> BuildPath(string endKey, Dictionary<string, string?> parent)
    {
        var path = new List<string>();
        var current = endKey;

        while (current != null)
        {
            path.Add(current);
            current = parent[current];
        }

        path.Reverse();
        return path;
    }
}

internal class PossibleClassification(string group, string? category, List<string> path, int certaintyPercentage)
{
    public string Group { get; internal set; } = group;
    public string? Category { get; internal set; } = category;
    public List<string> Path { get; internal set; } = path;
    public int CertaintyPercentage { get; internal set; } = certaintyPercentage;
}