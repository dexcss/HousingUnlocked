using System.Collections.Generic;

namespace HousingUnlocked
{
  /// <summary>
  /// Complete offline snap data for every housing item in the game, extracted directly
  /// from the live game files (sgb SphereCastRange blocks + pcb collision materials +
  /// sgb TargetMarkers). Coverage: every HousingFurniture/HousingYardObject model.
  /// 
  /// - ItemSnapRanges: SphereCastRange (0x4B) overrides — uniform sphere radius in yalms
  /// - ItemScatterRanges: anisotropic scatter blocks (Cherry Petal Pile, Hingan Rock Garden)
  /// - WallItemThresholds: wall snap distance for wall-mounted items (maxMarkerXZ * 0.5)
  /// - WallReceiverItemIds: collision carries WallCollisionMesh (d3==1) — false walls
  /// - TableReceiverItemIds: collision carries TableTop (d4==8)
  /// 
  /// Generated 2026-10-21 — 116 snap items, 371 wall thresholds, 39 wall receivers, 246 table receivers.
  /// Refresh per FFXIV patch.
  /// </summary>
  public static class SnapRangeData
  {
    /// <summary>Item RowId -> sphere-cast snap radius in yalms (uniform F1==F2==F3 blocks).</summary>
    public static readonly IReadOnlyDictionary<uint, float> ItemSnapRanges = new Dictionary<uint, float>
    {
      { 6476u, 0.3f }, // Shroud Broadleaf
      { 6477u, 0.5f }, // Island Palm
      { 6478u, 0.3f }, // Desert Ironwood
      { 6482u, 0.5f }, // Linen Parasol
      { 6483u, 0.7f }, // Lotus Parasol
      { 6486u, 0.45f }, // Brass Bench
      { 6487u, 0.45f }, // Oaken Bench
      { 6490u, 0.5f }, // Riviera Lamppost
      { 6495u, 0.6f }, // Sylphic Lamp Tree
      { 6503u, 0.5f }, // Blue Coral Formation
      { 6508u, 0.5f }, // Death's Fingers
      { 6599u, 2f }, // Llymlaen's Embrace
      { 7064u, 1.2f }, // Summoning Bell
      { 7989u, 2.2f }, // Chocobo Stable
      { 8812u, 0.5f }, // Adventurers' Guild Aduyses
      { 9719u, 2f }, // Oriental Bathtub
      { 9741u, 0.5f }, // Eastern Cherry Tree
      { 9744u, 0.4f }, // Wild Banana Tree
      { 12100u, 1.8f }, // Royal Plotting Table
      { 12103u, 1f }, // Bearskin Rug
      { 12671u, 2f }, // Sunflower Plot
      { 13061u, 1.2f }, // Crystal Bell
      { 13065u, 1.5f }, // Twin Star Ornament
      { 13082u, 1.8f }, // Paramour Bed
      { 13088u, 1f }, // Sephirot Root
      { 13089u, 1f }, // Sephirot Tree
      { 13090u, 1f }, // Oriental Grass Plot
      { 13091u, 0.3f }, // Dravanian Down Tree
      { 15152u, 0.8f }, // Nidhogg Miniature
      { 15953u, 2.2f }, // Riviera Chocobo Stable
      { 15954u, 2.2f }, // Glade Chocobo Stable
      { 15955u, 2.2f }, // Oasis Chocobo Stable
      { 15971u, 2f }, // Knightly Round Table
      { 15981u, 0.5f }, // Autumnal Maple Tree
      { 15982u, 0.5f }, // Autumnal Ginkgo Tree
      { 15983u, 0.5f }, // Maple Leaf Pile
      { 15984u, 0.5f }, // Ginkgo Leaf Pile
      { 15985u, 1f }, // Snow Drift
      { 15996u, 1.5f }, // Jumbo Twin Star Ornament
      { 16781u, 1f }, // Troupe Stage
      { 17019u, 1f }, // Plundered Treasure
      { 17029u, 1f }, // Eastern Cherry Petal Pile
      { 17039u, 1.8f }, // Authentic Paramour Bed
      { 17044u, 0.5f }, // Authentic Eastern Cherry Tree
      { 17137u, 2.2f }, // Oriental Chocobo Stable
      { 17982u, 0.5f }, // Eastern Pine
      { 20772u, 0.75f }, // Far Eastern Willow
      { 20817u, 1f }, // Handmade Snow Drift
      { 21839u, 0.1f }, // Stage Panel
      { 21864u, 0.5f }, // Corner Hedge Partition
      { 21865u, 1.7f }, // Bamboo Copse
      { 21866u, 2f }, // Hingan Hot Spring
      { 21873u, 1f }, // Authentic Eastern Cherry Petal Pile
      { 22521u, 1f }, // Crystal Tower Striker
      { 22577u, 1.5f }, // Monster Toss Machine
      { 22578u, 1f }, // Cuff-a-Cur Machine
      { 22581u, 1.5f }, // Miniature Heaven-on-High
      { 23864u, 1f }, // Doman Dogwood
      { 23870u, 0.5f }, // Silver Starlight Sentinel
      { 23883u, 0.6f }, // Bread Rack
      { 23892u, 1f }, // Verdant Partition
      { 23893u, 1f }, // Bathroom Floor Tiles
      { 23898u, 1f }, // Garden Gravel
      { 24535u, 2f }, // Simple Treehouse
      { 24537u, 1.2f }, // Big One Festival Float
      { 27278u, 0.5f }, // Hunting Supplies
      { 28161u, 0.4f }, // Cedar Tree
      { 28911u, 0.5f }, // Authentic Silver Starlight Sentinel
      { 28996u, 1f }, // Eastern Stage
      { 28999u, 0.4f }, // Lakeland Elf Tree
      { 29681u, 0.5265f }, // Crystallized Coral
      { 29682u, 0.6f }, // White Screen
      { 30382u, 0.4f }, // Sapphire Weapon Bust
      { 30408u, 1.7333f }, // Wooden Footbridge
      { 30410u, 0.56f }, // Marble Alcove Bed
      { 30422u, 0.001f }, // Slanted Wall
      { 30423u, 0.001f }, // Slanted Skylight
      { 30424u, 1f }, // The Moogle's Paw Machine
      { 30425u, 1f }, // Out on a Limb Machine
      { 30426u, 1f }, // The Finer Miner Machine
      { 32206u, 0.5f }, // Emerald Weapon Bust
      { 32247u, 0.4f }, // Snow-dusted Tree
      { 32248u, 0.4f }, // Parkside Tree
      { 32250u, 0.5255f }, // La Noscean Grape Pergola
      { 32251u, 0.4f }, // Fruitful Fountain
      { 32966u, 1f }, // Authentic Eastern Stage
      { 33297u, 0.4f }, // Eastern Wisteria
      { 35583u, 0.4f }, // Hanging Bench
      { 35584u, 0.4f }, // Coldbare Tree
      { 35617u, 2.2f }, // Highland Chocobo Stable
      { 35807u, 1f }, // Thavnairian Dhow
      { 36380u, 0.4f }, // Deluxe Snow-dusted Tree
      { 37397u, 2f }, // Alpine Open-air Bath
      { 37400u, 2.7f }, // Raised Wooden Deck
      { 38624u, 0.4f }, // Illuminated Tree
      { 39421u, 2f }, // Smaller Water Otter Fountain
      { 40651u, 0.6252f }, // Verdant Pillar
      { 41144u, 1f }, // Imitation Magitek Repair Station (Wall Unit)
      { 41146u, 2.4583f }, // Hingan Rock Garden
      { 41147u, 2.5404f }, // Sharlayan Reflecting Pool
      { 41150u, 0.4f }, // Authentic Illuminated Tree
      { 41429u, 2.55f }, // Imitation Magitek Repair Station (Floor Unit)
      { 41435u, 0.3919f }, // Aloalo Island Palm
      { 44271u, 1f }, // Flying Pumpkin
      { 44892u, 2f }, // Traumerei Floor Stencil
      { 44927u, 1f }, // Garden Palm Tree
      { 44934u, 0.5f }, // Ja Ja Gural Tree
      { 46106u, 1.8f }, // Training Bench
      { 46120u, 0.4f }, // La Noscean Orange Tree
      { 46168u, 0.1f }, // Lyhe Ghiah Stained Crystal Ornament
      { 46170u, 1f }, // Traverse Tree
      { 47228u, 1f }, // Authentic Flying Pumpkin
      { 47352u, 0.4625f }, // Weeping Cherry Tree
      { 48100u, 0.5f }, // East End Tree
      { 48106u, 0.05f }, // No-entry Door Partition
      { 48115u, 0.2f }, // Vault Rib
    };

    /// <summary>Item RowId -> wall snap threshold in yalms (TargetMarker-derived, category-15 items).</summary>
    public static readonly IReadOnlyDictionary<uint, float> WallItemThresholds = new Dictionary<uint, float>
    {
      { 6656u, 0.5f }, // Maelstrom Banner
      { 6657u, 0.5f }, // Twin Adder Banner
      { 6658u, 0.5f }, // Immortal Flames Banner
      { 6659u, 0.5f }, // Lominsan Cityscape
      { 6660u, 0.5f }, // Gridanian Cityscape
      { 6661u, 0.5f }, // Ul'dahn Cityscape
      { 6662u, 0.5f }, // Eorzean Map
      { 6663u, 0.5f }, // Galleass Wheel
      { 6664u, 0.5f }, // Demon Wall Sculpture
      { 6665u, 0.5f }, // Inferno Wall Lamp
      { 6666u, 0.5f }, // Blazing Inferno Wall Lamp
      { 6667u, 0.5f }, // Sylphic Wall Lantern
      { 6668u, 0.5f }, // Nymian Wall Lantern
      { 6669u, 0.5f }, // Amdapori Wall Lantern
      { 6670u, 0.5f }, // Belah'dian Crystal Lantern
      { 6671u, 0.5f }, // Wall Chronometer
      { 6672u, 0.5f }, // Behemoth Wall Trophy
      { 7074u, 0.5f }, // Glade Wall Chronometer
      { 7082u, 0.5f }, // Tonberry Wall Lantern
      { 7093u, 0.5f }, // Carbuncle Chronometer
      { 7095u, 0.5f }, // Admiral Portrait
      { 7096u, 0.5f }, // Sultana and Flame General Portrait
      { 7097u, 0.5f }, // Elder Seedseer Portrait
      { 7152u, 0.5f }, // Sultana Portrait
      { 7958u, 0.5f }, // Riviera Wall Shelf
      { 7968u, 0.5f }, // Onus of an Engineer
      { 7969u, 0.5f }, // Enquire Within
      { 7970u, 0.5f }, // Wall Planter
      { 8730u, 0.5f }, // A Northerly Wind
      { 8731u, 0.5f }, // The Antecedent
      { 8805u, 0.5f }, // Wall Lantern
      { 8807u, 0.5f }, // Stuffed Mazlaya Marlin
      { 8808u, 0.5f }, // Megalodon Jaws
      { 8809u, 0.5f }, // Stuffed Takitaro
      { 8810u, 0.5f }, // Starlight Wreath
      { 8811u, 0.5f }, // Towel Hanger
      { 8812u, 0.5f }, // Adventurers' Guild Aduyses
      { 9730u, 0.5f }, // Oriental Altar
      { 9731u, 0.5f }, // Oriental Wall Scroll
      { 12093u, 0.5f }, // Oriental Wind Chime
      { 13064u, 0.5f }, // Starlight Ornament
      { 13065u, 0.5f }, // Twin Star Ornament
      { 13289u, 0.5f }, // A Knight to Remember
      { 13338u, 0.5f }, // 
      { 13339u, 0.5f }, // Starlight Celebration MMXIV Advertisement
      { 13340u, 0.5f }, // 
      { 15156u, 0.5f }, // Rising Banner
      { 15970u, 0.5f }, // Mounted Box Shelf
      { 15994u, 0.5f }, // Jumbo Starlight Wreath
      { 15995u, 0.5f }, // Jumbo Starlight Ornament
      { 15996u, 0.5f }, // Jumbo Twin Star Ornament
      { 16147u, 0.5f }, // Deluxe Oriental Wind Chime
      { 16779u, 0.5f }, // Indirect Wall Lighting
      { 16935u, 0.5f }, // Grade 1 Picture Frame
      { 16936u, 0.5f }, // Grade 2 Picture Frame
      { 16937u, 0.5f }, // Grade 3 Picture Frame
      { 17018u, 0.5f }, // Forever Friends
      { 17022u, 0.5f }, // Little Ladies' Day MMXVI Advertisement
      { 17955u, 0.5f }, // Mounted Flower Vase
      { 17975u, 0.5f }, // Moonfire Faire MMXIV Advertisement
      { 20306u, 0.5f }, // Yugiri and Gosetsu Sketch
      { 20310u, 0.5f }, // Imitation Square Window
      { 20623u, 0.5f }, // Limited Edition Rising Banner
      { 20639u, 0.5f }, // Mol Banner
      { 20742u, 0.5f }, // Hingan Chochin Lantern
      { 20743u, 0.5f }, // Hingan Wall Scroll
      { 20744u, 0.5f }, // Decorative Wall Fan
      { 20752u, 0.5f }, // Fool's Portal
      { 20753u, 0.5f }, // Imitation Stained Crystal Ornament
      { 20754u, 0.5f }, // Shinryu's Wing
      { 20764u, 0.5f }, // All Saints' Wake MMXIII Advertisement
      { 20768u, 0.5f }, // Starlight Celebration MMXIII Advertisement
      { 20816u, 0.5f }, // Authentic Starlight Celebration MMXIV Advertisement
      { 21311u, 0.5f }, // Authentic MMXIII All Saints' Wake Advertisement
      { 21315u, 0.5f }, // Authentic Starlight Celebration MMXIII Advertisement
      { 21812u, 0.5f }, // Hatching-tide Banner
      { 21815u, 0.5f }, // Moogle Wall Chronometer
      { 21820u, 0.5f }, // Valentione's Day MMXIV Advertisement
      { 21826u, 0.5f }, // Brick Interior Wall
      { 21827u, 0.5f }, // Mounted Cupboard
      { 21835u, 0.5f }, // Kitchen Hanger
      { 21836u, 0.5f }, // Hanya Mask
      { 21872u, 0.5f }, // Authentic Little Ladies' Day MMXVI Advertisement
      { 22405u, 0.5f }, // Authentic Hatching-tide Banner
      { 22407u, 0.5f }, // Authentic Valentione's Day MMXIV Advertisement
      { 22541u, 0.5f }, // Odder Otter Wall Lantern
      { 22542u, 0.5f }, // Crossed Halberds
      { 22546u, 0.5f }, // Falcons Banner
      { 22547u, 0.5f }, // Ravens Banner
      { 22560u, 0.5f }, // Trouser Hanger
      { 22572u, 0.5f }, // Fool's Threshold
      { 23339u, 0.5f }, // Authentic Moonfire Faire MMXIV Advertisement
      { 23863u, 0.5f }, // Suzaku Wall Hanging
      { 23865u, 0.5f }, // Enclave Map
      { 23867u, 0.5f }, // Pumpkin Party Pennants
      { 23869u, 0.5f }, // Familiar Pawprints
      { 23877u, 0.5f }, // Imitation Shuttered Window
      { 23891u, 0.5f }, // Mounted Plate Rack
      { 23894u, 0.5f }, // Bathroom Wall Tiles
      { 24501u, 0.5f }, // Small Imitation Window
      { 24508u, 0.5f }, // Botanist's Dried Herbs
      { 24510u, 0.5f }, // Four Lords
      { 24511u, 10f }, // Wooden Loft
      { 24512u, 0.5f }, // Wooden Steps
      { 24513u, 10f }, // Wooden Beam
      { 24526u, 0.5f }, // Hanging Planter Shelf
      { 27272u, 0.5f }, // Lily Wall Lamp
      { 27274u, 0.5f }, // Simple Curtain
      { 27276u, 0.5f }, // Butterfly Specimen
      { 27280u, 0.5f }, // Imitation Stained Crystal Pane
      { 27285u, 0.5f }, // Crystarium Kitchen Hanger
      { 27288u, 0.5f }, // Crystarium Wall Chronometer
      { 27289u, 0.5f }, // Moonfire Faire MMXVIII Advertisement
      { 27290u, 0.5f }, // All Saints' Wake MMXV Advertisement
      { 27299u, 0.5f }, // Blades of Innocence
      { 27300u, 0.5f }, // Titania Shadow Box
      { 27306u, 0.5f }, // Liberation
      { 27307u, 0.5f }, // The Legend Returns
      { 27308u, 0.5f }, // Rise of a New Sun
      { 27309u, 0.5f }, // Under the Moonlight
      { 27310u, 0.5f }, // Prelude in Violet
      { 27311u, 0.5f }, // Requiem for Heroes
      { 27981u, 0.5f }, // Authentic Pumpkin Party Pennants
      { 27983u, 0.5f }, // Authentic Familiar Pawprints
      { 28129u, 0.5f }, // Starlight Goobbue Wreath
      { 28133u, 0.5f }, // Oasis Wall-mounted Fountain
      { 28139u, 0.5f }, // Imitation Long Window
      { 28152u, 0.5f }, // Starlight Celebration MMXVIII Advertisement
      { 28501u, 0.5f }, // Dragonsong Reproduction
      { 28502u, 0.5f }, // Liberation Reproduction
      { 28503u, 0.5f }, // Nightfall Reproduction
      { 28636u, 0.5f }, // Shirt Display Rack
      { 28915u, 0.5f }, // Scions of the Seventh Dawn Reproduction
      { 28931u, 0.5f }, // Small Angler's Canvas
      { 28932u, 0.5f }, // Medium Angler's Canvas
      { 28933u, 0.5f }, // Large Angler's Canvas
      { 28934u, 0.5f }, // Extra Large Angler's Canvas
      { 28966u, 0.5f }, // Hatching-tide MMXVIII Advertisement
      { 28972u, 0.5f }, // Amaurot Wall Lamp
      { 28975u, 0.5f }, // Oldrose Wall Planter
      { 28979u, 0.5f }, // Lord Commander Portrait
      { 28989u, 0.5f }, // Azure Dragoon Portrait
      { 28990u, 0.5f }, // Cosmos Decoration
      { 28995u, 0.5f }, // Apron Rack
      { 28997u, 0.5f }, // Little Ladies' Day Banner
      { 29684u, 0.5f }, // Fat Cat Wall Chronometer
      { 30381u, 0.5f }, // Trophy of Light
      { 30387u, 0.5f }, // Bluebird Mobile
      { 30389u, 0.75f }, // Stage Curtain
      { 30397u, 0.5f }, // Cracked Arch Window
      { 30398u, 0.5f }, // Industrial Wall Pipes
      { 30400u, 10f }, // Factory Grate
      { 30401u, 0.5f }, // Factory Staircase
      { 30403u, 0.5f }, // Factory Beam
      { 30406u, 0.5f }, // Factory Wall Lamp
      { 30411u, 0.5f }, // Watts's Sketches
      { 30429u, 0.5f }, // Authentic Moonfire Faire MMXVIII Advertisement
      { 30883u, 0.5f }, // The Sultana's Seven
      { 31706u, 0.5f }, // Authentic Copy of Liberation
      { 31707u, 0.5f }, // Authentic Copy of The Legend Returns
      { 31708u, 0.5f }, // Authentic Copy of Rise of a New Sun
      { 31709u, 0.5f }, // Authentic Copy of Under the Moonlight
      { 31710u, 0.5f }, // Authentic Copy of Prelude in Violet
      { 31711u, 0.5f }, // Authentic Copy of Requiem for Heroes
      { 31713u, 0.5f }, // Authentic All Saints' Wake MMXV Advertisement
      { 32209u, 10f }, // Unmelting Ice Loft
      { 32219u, 0.5f }, // Iron Torch
      { 32224u, 0.5f }, // Swag Valance
      { 32232u, 0.5f }, // Elemental Wheel
      { 32961u, 0.5f }, // Authentic Starlight Celebration MMXVIII Advertisement
      { 32963u, 0.5f }, // Authentic Starlight Goobbue Wreath
      { 32967u, 0.5f }, // Authentic Little Ladies' Day Banner
      { 32969u, 0.5f }, // Authentic Hatching-tide MMXVIII Advertisement
      { 33265u, 0.5f }, // Aetherolectric Guitar
      { 33286u, 0.5f }, // Bar Rack
      { 33287u, 0.5f }, // Portrait of Gestahl
      { 33291u, 0.5f }, // Goobbue Wall Chronometer
      { 33293u, 0.5f }, // Mounted Dinosaur Skull
      { 33304u, 0.5f }, // Authentic Sultana's Seven
      { 35557u, 0.5f }, // Ivy Mural
      { 35562u, 0.75f }, // Round Wooden Loft
      { 35563u, 0.5f }, // Heavenly Ornamental Array
      { 35571u, 0.5f }, // Wooden Blinds
      { 35573u, 0.5f }, // Antique Wall Shelf
      { 35575u, 1.5f }, // Imitation Wooden Skylight
      { 35577u, 0.5f }, // Pendant Wall Light
      { 35799u, 0.5f }, // Labyrinthos Warning Sign
      { 35803u, 0.5f }, // Starlight Mobile
      { 36382u, 10f }, // Deluxe Unmelting Ice Loft
      { 36631u, 0.5f }, // Valentione Mobile
      { 37351u, 0.5f }, // Ornamental Crystal Array
      { 37353u, 0.5f }, // Kweh-kweh Clock
      { 37354u, 10f }, // Small Blackboard
      { 37359u, 0.5f }, // Ivy Curtain
      { 37360u, 10f }, // Luminous Wooden Loft
      { 37362u, 0.5f }, // Ironworks Tool Set
      { 37364u, 0.5f }, // Simple Arched Window
      { 37371u, 0.5f }, // Alpine Wall Lantern
      { 37373u, 0.05f }, // Imitation Highland Window
      { 37388u, 0.5f }, // Hatching-tide Mobile
      { 37389u, 0.5f }, // Emperor Portrait
      { 37390u, 0.5f }, // Heavensturn Crane Banner
      { 37391u, 0.5f }, // Azure Banner of Conflict
      { 37392u, 0.5f }, // Crimson Banner of Conflict
      { 38576u, 0.5f }, // Imitation Stained Crystal Roundel
      { 38577u, 0.75f }, // Hanging Planter Branch
      { 38580u, 0.5f }, // Wall-climbing Ivy
      { 38582u, 0.5f }, // Hydaelyn, as We Know Her
      { 38583u, 0.5f }, // Wondrous Windoor
      { 38587u, 0.5f }, // Wall-mounted Wings
      { 38591u, 0.5f }, // Side-tied Curtain
      { 38592u, 10f }, // Tatami Loft
      { 38603u, 0.5f }, // Sil'dihn Banner
      { 38605u, 0.5f }, // Korpokkur Chronometer
      { 38606u, 10f }, // Natural Wooden Beam
      { 38607u, 0.5f }, // Flagstone Steps
      { 38608u, 10f }, // Flagstone Loft
      { 38609u, 0.5f }, // Exciting Neon Wall Light
      { 38613u, 0.5f }, // Bill of Contrition (S-1)
      { 38614u, 0.5f }, // Bill of Deep Contrition (M-1)
      { 38615u, 0.5f }, // Bill of Deepest Contrition (L-1)
      { 38670u, 0.5f }, // Authentic Starlight Mobile
      { 38676u, 0.5f }, // Beyond Gloam's Veil
      { 38677u, 0.5f }, // Love and Loss
      { 38678u, 0.5f }, // Wherewith We Learn
      { 38679u, 0.5f }, // Business's Pleasure
      { 38680u, 0.5f }, // Scion of Wisdom
      { 38681u, 0.5f }, // Scion of Valor
      { 38682u, 0.5f }, // Discernment
      { 38683u, 0.5f }, // Adventure Calls
      { 38697u, 0.5f }, // Bill of Contrition (S-2)
      { 38698u, 0.5f }, // Bill of Contrition (S-3)
      { 38699u, 0.5f }, // Bill of Contrition (S-4)
      { 38700u, 0.5f }, // Bill of Contrition (S-5)
      { 38701u, 0.5f }, // Bill of Deep Contrition (M-2)
      { 38702u, 0.5f }, // Bill of Deep Contrition (M-3)
      { 38703u, 0.5f }, // Bill of Deep Contrition (M-4)
      { 38704u, 0.5f }, // Bill of Deep Contrition (M-5)
      { 38705u, 0.5f }, // Bill of Deepest Contrition (L-2)
      { 38706u, 0.5f }, // Bill of Deepest Contrition (L-3)
      { 38707u, 0.5f }, // Bill of Deepest Contrition (L-4)
      { 38708u, 0.5f }, // Bill of Deepest Contrition (L-5)
      { 39380u, 0.5f }, // Faerie Door
      { 39384u, 0.5f }, // Far Eastern Stained Crystal Roundel
      { 39388u, 0.5f }, // Allagan Terminal Alpha
      { 39389u, 0.5f }, // Allagan Terminal Beta
      { 39395u, 0.5f }, // Enigma Wall Chronometer
      { 39401u, 0.5f }, // Hannish Pendant Wall Light
      { 39410u, 0.5f }, // Infernal Archfiend Cloak
      { 39417u, 0.5f }, // Hatching-tide MMXXII Advertisement
      { 39418u, 0.5f }, // Little Ladies' Day MMXXI Advertisement
      { 39433u, 0.5f }, // Authentic Valentione Mobile
      { 39435u, 0.5f }, // Authentic Hatching-tide Mobile
      { 40624u, 1.5f }, // Imitation Curtained Window
      { 40630u, 0.5f }, // Imitation Moonlit Window
      { 40631u, 0.5f }, // Metal Pipe Wall Shelf
      { 40634u, 0.5f }, // Floating Kitchen Cabinet
      { 40635u, 10f }, // Azure Sky Phasmascape
      { 40636u, 10f }, // Starry Sky Phasmascape
      { 40645u, 0.5f }, // Imitation Vault
      { 40646u, 0.55f }, // Ivy Pendant Wall Light
      { 40647u, 0.75f }, // Crystarium Pendant Wall Light
      { 40655u, 0.5f }, // Imitation Closed Door
      { 40656u, 0.5f }, // Wall-mounted Pot Rack
      { 40660u, 0.5f }, // Forever at Your Side
      { 40664u, 0.5f }, // The Rising MMXXIII Advertisement
      { 40665u, 0.5f }, // Wall-mounted Vampire Bats
      { 40666u, 0.5f }, // Springtide Fan Stand
      { 40759u, 0.5f }, // Authentic Copy of Newfound Adventure
      { 40760u, 0.5f }, // Authentic Copy of Buried Memory
      { 40761u, 0.5f }, // Authentic Copy of Gods Revel, Lands Tremble
      { 40762u, 0.5f }, // Authentic Copy of the Dark Throne
      { 40763u, 0.5f }, // Loss, Fire, Faith Reproduction
      { 41085u, 0.5f }, // Hanging Lure Display
      { 41087u, 0.5f }, // Crystarium Wall Canopy
      { 41097u, 0.05f }, // Lapis Manalis Banner
      { 41103u, 0.85f }, // Wood Slice Loft
      { 41104u, 0.5f }, // Faerie Pendant Wall Light
      { 41113u, 0.5f }, // Colossal Chronometer Window
      { 41115u, 0.5f }, // Authentic Copy of A Realm Awoken
      { 41116u, 0.5f }, // Authentic Copy of Through the Maelstrom
      { 41117u, 0.5f }, // Authentic Copy of Defenders of Eorzea
      { 41118u, 0.5f }, // Authentic Copy of Dreams of Ice
      { 41119u, 0.5f }, // Authentic Copy of Before the Fall
      { 41120u, 0.5f }, // Authentic Copy of As Goes Light, So Goes Darkness
      { 41121u, 0.5f }, // Authentic Copy of The Gears of Change
      { 41122u, 0.5f }, // Authentic Copy of Revenge of the Horde
      { 41123u, 0.5f }, // Authentic Copy of Soul Surrender
      { 41124u, 0.5f }, // Authentic Copy of The Far Edge of Fate
      { 41125u, 0.5f }, // Authentic Copy of Vows of Virtue, Deeds of Cruelty
      { 41126u, 0.5f }, // Authentic Copy of Echoes of a Fallen Star
      { 41127u, 0.5f }, // Authentic Copy of Reflections in Crystal
      { 41128u, 0.5f }, // Authentic Copy of Futures Rewritten
      { 41129u, 0.5f }, // Authentic Copy of Death Unto Dawn
      { 41130u, 0.5f }, // Authentic Copy of Growing Light
      { 41131u, 0.5f }, // The Builder
      { 41132u, 0.5f }, // The Destroyer
      { 41133u, 0.5f }, // The Warden
      { 41134u, 0.5f }, // The Traders
      { 41135u, 0.5f }, // The Matron
      { 41136u, 0.5f }, // The Keeper
      { 41151u, 0.5f }, // Authentic Heavensturn Crane Banner
      { 41153u, 0.5f }, // Authentic Little Ladies' Day MMXXI Advertisement
      { 41154u, 0.5f }, // Authentic Hatching-tide MMXXII Advertisement
      { 41430u, 0.5f }, // Aloalo Pendant Wall Lamp
      { 41822u, 0.5f }, // Decorative Hingan Door
      { 41823u, 10f }, // Sunset Phasmascape
      { 43558u, 0.5f }, // The Spinner
      { 43559u, 0.5f }, // The Fury
      { 43560u, 0.5f }, // The Lover
      { 43561u, 0.5f }, // The Scholar
      { 43562u, 0.5f }, // The Navigator
      { 43563u, 0.5f }, // The Wanderer
      { 43941u, 0.5f }, // Legacies Reproduction
      { 44119u, 0.5f }, // Horizontal Neon Wall Light
      { 44120u, 0.5f }, // Vertical Neon Wall Light
      { 44332u, 0.5f }, // Authentic Rising MMXXIII Advertisement
      { 44333u, 0.5f }, // Authentic Wall-mounted Vampire Bats
      { 44874u, 0.5f }, // Holodisplay
      { 44877u, 0.5f }, // Frilly Curtains
      { 44878u, 0.5f }, // Botanist's Wall Frames
      { 44879u, 10f }, // Botanical Loft
      { 44883u, 0.5f }, // Investigation Map
      { 44891u, 0.5f }, // Strayborough Wall Lamp
      { 44897u, 0.5f }, // Imitation Closet
      { 44899u, 0.5f }, // Dartboard Cabinet
      { 44904u, 0.5f }, // Classroom Blackboard
      { 44906u, 0.5f }, // Starlight Celebration MMXXIII Advertisement
      { 44907u, 0.5f }, // Valentione's Day MMXXIV Advertisement
      { 44908u, 0.5f }, // Authentic Copy of Crossroads
      { 44911u, 1.5f }, // Weathered Porthole
      { 44914u, 0.5f }, // Indoor Awning
      { 46104u, 0.5f }, // Overhead Studio Light
      { 46117u, 0.5f }, // Authentic Copy of The Promise of Tomorrow
      { 46119u, 0.5f }, // All Saints' Wake MMXXIV Advertisement
      { 46163u, 0.5f }, // Wilds Banner
      { 46168u, 0.5f }, // Lyhe Ghiah Stained Crystal Ornament
      { 46172u, 0.5f }, // Legends of Ivalice
      { 47350u, 0.5f }, // Authentic Valentione's Day MMXXIV Advertisement
      { 48109u, 0.5f }, // Decorative Hingan Window
      { 48111u, 0.5f }, // Hingan Wall Shelf
      { 48112u, 0.5f }, // Hingan Wall Cabinet
      { 48133u, 0.5f }, // Authentic Copy of Seekers of Eternity
      { 48144u, 0.5f }, // Occult Crescent Map
      { 49845u, 0.5f }, // Housing Design Award MMXXV Plaque 
      { 49847u, 0.5f }, // Unfriendly Yan Neon Wall Light
      { 49849u, 0.5f }, // Imitation Cosmoportal
      { 49853u, 0.5f }, // Peeling Wallpaper Sticker
      { 49860u, 0.5f }, // Authentic Copy of Into the Mist
      { 49861u, 0.5f }, // Valentione's Day MMXXV Advertisement
      { 49862u, 0.5f }, // Little Ladies' Day MMXXV Advertisement
      { 49864u, 0.5f }, // Little Ladies' Day MMXVIII Advertisement
      { 49865u, 0.5f }, // Hatching-tide MMXXV Advertisement
      { 49867u, 0.5f }, // Hatching-tide MMXVI Advertisement
      { 49868u, 0.5f }, // Authentic Starlight Celebration MMXXIII Advertisement
      { 49869u, 0.5f }, // Authentic All Saints' Wake MMXXIV Advertisement
      { 50083u, 0.5f }, // Moonfire Faire MMXXV Advertisement
      { 50085u, 0.5f }, // Moonfire Faire MMXVII Advertisement
      { 50086u, 0.5f }, // Make It Rain Campaign MMXXV Advertisement
      { 50088u, 0.5f }, // Make It Rain Campaign MMXXIV Advertisement
      { 50360u, 0.5f }, // Authentic Springtide Fan Stand
      { 51264u, 0.5f }, // Everkeep Wall Decoration
      { 51282u, 0.5f }, // Winged Scalekin Fossil
      { 51284u, 0.5f }, // Crescent Wall Rack
      { 51298u, 0.5f }, // Authentic Copy of Trail to the Heavens
      { 51299u, 0.5f }, // The Rising MMXXV Advertisement
      { 51301u, 0.5f }, // The Rising MMXXIV Advertisement
      { 51302u, 0.5f }, // All Saints' Wake MMXXV Advertisement
      { 51304u, 0.5f }, // All Saints' Wake MMXVI Advertisement
      { 51305u, 0.5f }, // Starlight Celebration MMXXV Advertisement
      { 51307u, 0.5f }, // Starlight Celebration MMXXII Advertisement
    };

    /// <summary>Items whose collision carries WallCollisionMesh (d3==1) — walls wall items snap onto.</summary>
    public static readonly IReadOnlySet<uint> WallReceiverItemIds = new HashSet<uint>
    {
      6576u, // Riviera Partition (indoor:m0029)
      6577u, // Glade Partition (indoor:m0057)
      6578u, // Oasis Partition (indoor:m0055)
      17028u, // White Partition (indoor:m0502)
      21106u, // Blank Riviera Partition (indoor:m0527)
      21107u, // Blank Glade Partition (indoor:m0648)
      21108u, // Blank Oasis Partition (indoor:m0649)
      21109u, // Blank Hingan Partition (indoor:m0650)
      21207u, // Hingan Partition (indoor:m0656)
      21829u, // Open Partition (indoor:m0620)
      21839u, // Stage Panel (indoor:m0633)
      24513u, // Wooden Beam (indoor:m0765)
      24514u, // White Rectangular Partition (indoor:m0766)
      27273u, // Marble Arch Partition (indoor:m0799)
      27281u, // Marble Partition (indoor:m0807)
      28154u, // Log Partition (indoor:m0867)
      29682u, // White Screen (indoor:m0945)
      30383u, // Verdant Shelf Partition (indoor:m0932)
      30405u, // Factory Partition (indoor:m0975)
      30410u, // Marble Alcove Bed (indoor:m0956)
      30422u, // Slanted Wall (indoor:m0951)
      30423u, // Slanted Skylight (indoor:m0952)
      32208u, // Unmelting Ice Partition (indoor:m0989)
      32226u, // Wood Slat Partition (indoor:m1008)
      33267u, // Windowed Partition (indoor:m1041)
      33268u, // Hingan Windowed Partition (indoor:m1042)
      35576u, // Square Niche Partition (indoor:m1093)
      36381u, // Deluxe Unmelting Ice Partition (indoor:m0989)
      39392u, // Highland Partition (indoor:m1223)
      39407u, // Hannish Bookshelf Partition (indoor:m1234)
      40652u, // Rattan Partition (indoor:m1255)
      41110u, // Wall Cabinet (indoor:m1324)
      41816u, // Kitchen Cabinet (indoor:m1380)
      44111u, // Stone Partition (indoor:m1202)
      44895u, // Climbing Wall Partition (indoor:m1425)
      44912u, // Rail-and-stile Partition (indoor:m1442)
      46174u, // Cosmokitchen Partition (indoor:m1546)
      48134u, // Blank Minimalist Partition (indoor:m1495)
      51253u, // Minimalist Poplar Partition (indoor:m1595)
    };

    /// <summary>Items whose collision carries TableTop (d4==8) — surfaces tabletop items snap onto.</summary>
    public static readonly IReadOnlySet<uint> TableReceiverItemIds = new HashSet<uint>
    {
      6538u, // Storm Desk (indoor:m0119)
      6539u, // Serpent Desk (indoor:m0131)
      6540u, // Flame Desk (indoor:m0125)
      6548u, // Ash Cabinet (indoor:m0007)
      6556u, // Storm Strongbox (indoor:m0120)
      6557u, // Serpent Strongbox (indoor:m0132)
      6558u, // Flame Strongbox (indoor:m0126)
      6564u, // Ahriman Cupboard (indoor:m0090)
      6601u, // Riviera Round Table (indoor:m0001)
      6602u, // Oasis Round Table (indoor:m0002)
      6603u, // Glade Round Table (indoor:m0003)
      6604u, // Ahriman Round Table (indoor:m0086)
      6605u, // Diagnostics Module (indoor:m0140)
      6606u, // Riviera Table (indoor:m0169)
      6607u, // Glade Table (indoor:m0171)
      6608u, // Oasis Table (indoor:m0170)
      6609u, // Sylphic Table (indoor:m0094)
      6610u, // Riviera Dining Table (indoor:m0067)
      6611u, // Glade Dining Table (indoor:m0173)
      6612u, // Oasis Dining Table (indoor:m0172)
      6613u, // Walnut Dining Table (indoor:m0008)
      6614u, // Sylphic Dining Table (indoor:m0095)
      6615u, // Manor Table (indoor:m0078)
      6616u, // Riviera Desk (indoor:m0174)
      6617u, // Oasis Desk (indoor:m0175)
      6618u, // Glade Desk (indoor:m0176)
      6619u, // Manor Desk (indoor:m0076)
      6620u, // Knight Captain's Desk (indoor:m0103)
      6621u, // Walnut Cartonnier (indoor:m0009)
      6622u, // Glade Cartonnier (indoor:m0033)
      6623u, // Glade Sideboard (indoor:m0034)
      6624u, // Glade Drawer Table (indoor:m0035)
      6625u, // Manor Flower Stand (indoor:m0069)
      6626u, // Ahriman Sideboard (indoor:m0087)
      6627u, // Riviera Stall (indoor:m0164)
      6628u, // Glade Stall (indoor:m0165)
      6629u, // Oasis Stall (indoor:m0036)
      6630u, // Riviera Counter (indoor:m0166)
      6631u, // Glade Counter (indoor:m0168)
      6632u, // Oasis Counter (indoor:m0167)
      6633u, // Sylphic Counter (indoor:m0096)
      6634u, // Manor Fireplace (indoor:m0073)
      7075u, // Tonberry Round Table (indoor:m0128)
      7076u, // Tonberry Dining Table (indoor:m0134)
      7084u, // Lalafellin Step Stool (indoor:m0193)
      7087u, // Carbuncle Round Table (indoor:m0196)
      7089u, // Carbuncle Desk (indoor:m0198)
      7102u, // Eorzean Map Table (indoor:m0230)
      7108u, // Manor Harpsichord (indoor:m0236)
      7958u, // Riviera Wall Shelf (indoor:m0248)
      7973u, // Riviera Half Partition (indoor:m0263)
      7978u, // Corner Counter (indoor:m0268)
      8793u, // Riviera Open-shelf Bookcase (indoor:m0286)
      8794u, // Glade Open-shelf Bookcase (indoor:m0287)
      8795u, // Oasis Open-shelf Bookcase (indoor:m0288)
      8796u, // Shipping Crate (indoor:m0308)
      8797u, // Wine Barrel (indoor:m0309)
      9722u, // Kotatsu Table (indoor:m0328)
      9724u, // Guildleve Counter (indoor:m0342)
      9737u, // Order of Nald'thal Lectern (indoor:m0357)
      9748u, // Retainer Cartonnier (outdoor:m0106)
      9749u, // Retainer Counter (outdoor:m0107)
      12085u, // Alpine Round Table (indoor:m0330)
      12095u, // Glade Half Partition (indoor:m0373)
      12096u, // Oasis Half Partition (indoor:m0372)
      12097u, // Cask Rack (indoor:m0374)
      12098u, // Display Stand (indoor:m0375)
      12100u, // Royal Plotting Table (indoor:m0377)
      12101u, // Round Banquet Table (indoor:m0378)
      12102u, // Gold Saucer Corner Counter (indoor:m0379)
      12104u, // Gordian Bureau (indoor:m0381)
      12105u, // Savage Gordian Bureau (indoor:m0382)
      13063u, // Pumpkin Desk (indoor:m0388)
      13071u, // Manor Oven (indoor:m0396)
      13072u, // Manor Counter (indoor:m0397)
      13075u, // Oriental Round Table (indoor:m0402)
      14051u, // Pudding Desk (indoor:m0421)
      15143u, // Staggered Shelf (indoor:m0447)
      15778u, // Tier 4 Aquarium (indoor:m0456)
      15970u, // Mounted Box Shelf (indoor:m0463)
      15971u, // Knightly Round Table (indoor:m0464)
      15976u, // Combed Wool Rug (indoor:m0470)
      15990u, // Deluxe Pumpkin Desk (indoor:m0388)
      16780u, // Palace Pedestal (indoor:m0495)
      16781u, // Troupe Stage (indoor:m0498)
      16782u, // Round Stage (indoor:m0499)
      17024u, // Merchant's Shelf (indoor:m0493)
      17973u, // Zabuton Cushion (indoor:m0525)
      17974u, // White Half Partition (indoor:m0526)
      20726u, // Hingan Desk (indoor:m0551)
      20727u, // Hingan Sitting Table (indoor:m0561)
      20732u, // Hingan Sideboard (indoor:m0555)
      20733u, // Hingan Wardrobe (indoor:m0556)
      20734u, // Hingan Bookshelf (indoor:m0557)
      20735u, // Hingan Cupboard (indoor:m0558)
      20736u, // Hingan Open-shelf Bookcase (indoor:m0562)
      20738u, // Hingan Fire Pit (indoor:m0553)
      21816u, // Byakko Sideboard (indoor:m0607)
      21827u, // Mounted Cupboard (indoor:m0618)
      21829u, // Open Partition (indoor:m0620)
      21830u, // Counter Cart (indoor:m0621)
      21831u, // Sharlayan Cabinet (indoor:m0622)
      21835u, // Kitchen Hanger (indoor:m0626)
      21844u, // Bomb Stove (indoor:m0653)
      21849u, // Mummer's Wardrobe (indoor:m0661)
      21850u, // Far Eastern Doll Display (indoor:m0663)
      22409u, // Authentic Far Eastern Doll Display (indoor:m0663)
      22538u, // Odder Otter Dining Table (indoor:m0628)
      22554u, // Portable Stepladder (indoor:m0677)
      22558u, // Bar Counter (indoor:m0681)
      22569u, // Southern Kitchen (indoor:m0699)
      23856u, // Chocobo Round Table (indoor:m0541)
      23859u, // Chocobo Dining Table (indoor:m0641)
      23868u, // Pumpkin Pastry Platter (indoor:m0718)
      23879u, // Wooden Showcase (indoor:m0729)
      23891u, // Mounted Plate Rack (indoor:m0741)
      24491u, // Mirific Mogshelf (indoor:m0643)
      24493u, // Tier 4 Metal Aquarium (indoor:m0655)
      24507u, // Tree Slice Tower (indoor:m0759)
      24509u, // Steppe Kitchen (indoor:m0761)
      24511u, // Wooden Loft (indoor:m0763)
      24516u, // Fruit Stall (indoor:m0769)
      24518u, // Pastry Cupboard (indoor:m0771)
      24521u, // Potion Rack (indoor:m0774)
      24526u, // Hanging Planter Shelf (indoor:m0779)
      27284u, // Crystarium Stove (indoor:m0814)
      27294u, // Decadent Fruit Platter (indoor:m0827)
      27298u, // Carpet of Flowers (indoor:m0831)
      27982u, // Authentic Pumpkin Pastry Platter (indoor:m0718)
      28140u, // Imposing Ishgardian Shelf (indoor:m0852)
      28638u, // Lakeland Table (indoor:m0898)
      28963u, // Moogle Round Table (indoor:m0539)
      28973u, // Manor Dressing Table (indoor:m0901)
      28974u, // Cheese Collection (indoor:m0903)
      28982u, // Wooden Table (indoor:m0915)
      28991u, // Used Banquet Table (indoor:m0934)
      28996u, // Eastern Stage (indoor:m0938)
      30383u, // Verdant Shelf Partition (indoor:m0932)
      30385u, // Wooden Staircase Bookshelf (indoor:m0892)
      30393u, // Barrel Table (indoor:m0964)
      30400u, // Factory Grate (indoor:m0970)
      30416u, // Ishgardian Display Stand (indoor:m0961)
      32209u, // Unmelting Ice Loft (indoor:m0990)
      32214u, // Antique Sink (indoor:m0995)
      32225u, // Rustic Chocobo Counter (indoor:m1007)
      32230u, // Showcase (indoor:m1013)
      32231u, // Shelving Showcase (indoor:m1014)
      32233u, // Toy Showcase (indoor:m1016)
      32234u, // Apparel Showcase (indoor:m1017)
      32235u, // Armament Showcase (indoor:m1018)
      32236u, // Art Showcase (indoor:m1019)
      32237u, // Baked Goods Showcase (indoor:m1020)
      32966u, // Authentic Eastern Stage (indoor:m0938)
      33252u, // Cactuar Round Table (indoor:m0640)
      33263u, // Wooden Reel Table (indoor:m1037)
      33264u, // Violin Showcase (indoor:m1038)
      33273u, // Country Dining Set (indoor:m1048)
      33275u, // Icebox (indoor:m1052)
      33276u, // Red Brick Counter (indoor:m1053)
      33279u, // Back Bar (indoor:m1057)
      33281u, // Philosopher's Stone Table (indoor:m1060)
      33283u, // Low Bookshelf (indoor:m1062)
      33284u, // Qiqirn Shelf (indoor:m1063)
      33285u, // Mogbureau (indoor:m1064)
      35556u, // Shoe Rack (indoor:m1073)
      35562u, // Round Wooden Loft (indoor:m1079)
      35565u, // Apothecary's Workbench (indoor:m1082)
      35573u, // Antique Wall Shelf (indoor:m1090)
      35576u, // Square Niche Partition (indoor:m1093)
      35578u, // Loft Bed (indoor:m1055)
      35579u, // Restaurant Showcase (indoor:m1050)
      35801u, // Nekropolis Table (indoor:m1104)
      36382u, // Deluxe Unmelting Ice Loft (indoor:m0990)
      37347u, // Classic Dresser (indoor:m1098)
      37360u, // Luminous Wooden Loft (indoor:m1118)
      37361u, // Swinging-door Counter (indoor:m1119)
      37368u, // Indoor Climbing Frame (indoor:m1141)
      37369u, // Steel Locker (indoor:m1142)
      37370u, // Alpine Cabinet (indoor:m1139)
      37374u, // House Fortemps Fireplace (indoor:m1157)
      37376u, // Cathedral Lectern (indoor:m1159)
      37377u, // Scholasticate Table (indoor:m1160)
      38584u, // Mummer's Shelf (indoor:m1174)
      38592u, // Tatami Loft (indoor:m1184)
      38595u, // Sharlayan Sink (indoor:m1187)
      38597u, // Sharlayan Desk (indoor:m1189)
      38602u, // Sil'dihn Side Table (indoor:m1194)
      38608u, // Flagstone Loft (indoor:m1200)
      38611u, // Valentione's Heart Desk (indoor:m1204)
      39378u, // Simple Low Table (indoor:m1209)
      39396u, // Rainbow Hopping Rug (indoor:m1227)
      39405u, // Hannish Round Table (indoor:m1232)
      39407u, // Hannish Bookshelf Partition (indoor:m1234)
      39411u, // Faerie Round Table (indoor:m1236)
      40623u, // Botanist's Work Table (indoor:m1252)
      40624u, // Imitation Curtained Window (indoor:m1258)
      40631u, // Metal Pipe Wall Shelf (indoor:m1262)
      40634u, // Floating Kitchen Cabinet (indoor:m1265)
      40635u, // Azure Sky Phasmascape (indoor:m1267)
      40636u, // Starry Sky Phasmascape (indoor:m1268)
      40640u, // Rustic Log Table (indoor:m1272)
      40643u, // Vintage Side Table (indoor:m1275)
      40644u, // Short Pillar Bookshelf (indoor:m1276)
      40648u, // Fishing Rod Rack (indoor:m1108)
      40658u, // Classic Secretaire (indoor:m1280)
      41089u, // Ronkan Fireplace (indoor:m1304)
      41100u, // Aitiascope Desk (indoor:m1314)
      41103u, // Wood Slice Loft (indoor:m1317)
      41111u, // Field of Hope Rug (indoor:m1325)
      41152u, // Authentic Valentione's Heart Desk (indoor:m1204)
      41426u, // Cactuar Dining Table (indoor:m0644)
      41816u, // Kitchen Cabinet (indoor:m1380)
      41823u, // Sunset Phasmascape (indoor:m1372)
      41829u, // Twintop Table (indoor:m1378)
      44873u, // Wood-fired Range (indoor:m1396)
      44879u, // Botanical Loft (indoor:m1409)
      44889u, // Origenics Monitor Desk (indoor:m1419)
      44898u, // Fighting Ring (indoor:m1428)
      44900u, // Laboratory Counter (indoor:m1430)
      44902u, // Classroom Desk (indoor:m1432)
      44905u, // Contemporary Iron Shelf (indoor:m1435)
      44913u, // Tea Caddy Cabinet (indoor:m1443)
      46092u, // Straight Tuliyollan Counter (indoor:m1485)
      46093u, // Curved Tuliyollan Counter (indoor:m1486)
      46094u, // Tuliyollan Bench (indoor:m1487)
      46096u, // Tuliyollan Table (indoor:m1489)
      46103u, // Cabin Sideboard (indoor:m1508)
      47348u, // Valentione Round Table (indoor:m1553)
      48006u, // Authentic Cactuar Dining Table (indoor:m0644)
      48107u, // Hingan Dresser (indoor:m1457)
      48111u, // Hingan Wall Shelf (indoor:m1461)
      48113u, // Hingan Floor Cabinet (indoor:m1463)
      48116u, // Lattice-windowed Cabinet (indoor:m1466)
      48129u, // Mantelshelf (indoor:m1480)
      48131u, // Round Stepping Star (indoor:m1483)
      48140u, // Occult Table (indoor:m1500)
      48733u, // Cosmotable (indoor:m1515)
      51258u, // Everkeep Dining Table (indoor:m1600)
      51259u, // Everkeep Round Table (indoor:m1601)
      51260u, // Everkeep Sideboard (indoor:m1602)
      51261u, // Everkeep Chest (indoor:m1603)
      51266u, // Everkeep Kitchen Counter (indoor:m1608)
      51267u, // Everkeep Partition (indoor:m1609)
      51277u, // Cosmic Metal Shelf (indoor:m1628)
      51284u, // Crescent Wall Rack (indoor:m1633)
      51285u, // Crescent Display Case (indoor:m1634)
    };

    /// <summary>Item RowId -> display name (shared model variants use the shortest name).</summary>
    public static readonly IReadOnlyDictionary<uint, string> ItemNames = new Dictionary<uint, string>
    {
      { 6475u, "Moogle Letter Box" },
      { 6476u, "Shroud Broadleaf" },
      { 6477u, "Island Palm" },
      { 6478u, "Desert Ironwood" },
      { 6479u, "Unbreakable Rock" },
      { 6480u, "Mossy Rock" },
      { 6481u, "Campfire" },
      { 6482u, "Linen Parasol" },
      { 6483u, "Lotus Parasol" },
      { 6484u, "Walnut Table" },
      { 6485u, "Mythril Table" },
      { 6486u, "Brass Bench" },
      { 6487u, "Oaken Bench" },
      { 6488u, "Half Barrel Planter" },
      { 6489u, "Low Barrel Planter" },
      { 6490u, "Riviera Lamppost" },
      { 6491u, "Crofter's Wain" },
      { 6492u, "Stump Stool" },
      { 6494u, "Firewood" },
      { 6495u, "Sylphic Lamp Tree" },
      { 6496u, "Sylphic Lamppost" },
      { 6497u, "Cragsoul Lamp" },
      { 6498u, "Titanic Cragsoul Lamp" },
      { 6499u, "Wavesoul Fount" },
      { 6500u, "Tidal Wavesoul Fount" },
      { 6501u, "Dark Divinity Falleth" },
      { 6502u, "Savage Arc of Triumph" },
      { 6503u, "Blue Coral Formation" },
      { 6504u, "Bertha Cannon" },
      { 6505u, "Cannonballs" },
      { 6506u, "Stone Vigil Beacon" },
      { 6507u, "Bloodblossoms" },
      { 6508u, "Death's Fingers" },
      { 6509u, "Turnkey's Cups" },
      { 6510u, "Pyrepods" },
      { 6511u, "Ghostmaws" },
      { 6512u, "Amdapori Beacon" },
      { 6513u, "Riviera Chair" },
      { 6514u, "Glade Chair" },
      { 6515u, "Oasis Chair" },
      { 6516u, "Morbol Chair" },
      { 6517u, "Ahriman Chair" },
      { 6518u, "Sylphic Chair" },
      { 6519u, "Manor Stool" },
      { 6520u, "Manor Highback Chair" },
      { 6521u, "Knight Captain's Chair" },
      { 6522u, "Seating Module" },
      { 6523u, "Riviera Bench" },
      { 6524u, "Glade Bench" },
      { 6525u, "Oasis Bench" },
      { 6526u, "Storm Storage Bench" },
      { 6527u, "Flame Storage Bench" },
      { 6528u, "Serpent Storage Bench" },
      { 6529u, "Riviera Armchair" },
      { 6530u, "Glade Armchair" },
      { 6531u, "Oasis Armchair" },
      { 6532u, "Riviera Couch" },
      { 6533u, "Glade Couch" },
      { 6534u, "Oasis Couch" },
      { 6535u, "Manor Couch" },
      { 6536u, "Vortex Couch" },
      { 6537u, "Raging Vortex Couch" },
      { 6538u, "Storm Desk" },
      { 6539u, "Serpent Desk" },
      { 6540u, "Flame Desk" },
      { 6541u, "Message Book Stand" },
      { 6542u, "Riviera Bed" },
      { 6543u, "Glade Bed" },
      { 6544u, "Oasis Bed" },
      { 6545u, "Storm Utility Cot" },
      { 6546u, "Serpent Utility Cot" },
      { 6547u, "Flame Utility Cot" },
      { 6548u, "Ash Cabinet" },
      { 6549u, "Riviera Wardrobe" },
      { 6550u, "Glade Wardrobe" },
      { 6551u, "Oasis Wardrobe" },
      { 6552u, "Manor Wardrobe" },
      { 6553u, "Storm Armoire" },
      { 6554u, "Serpent Armoire" },
      { 6555u, "Flame Armoire" },
      { 6556u, "Storm Strongbox" },
      { 6557u, "Serpent Strongbox" },
      { 6558u, "Flame Strongbox" },
      { 6559u, "Company Chest" },
      { 6560u, "Riviera Cupboard" },
      { 6561u, "Glade Cupboard" },
      { 6562u, "Oasis Cupboard" },
      { 6563u, "Manor Cupboard" },
      { 6564u, "Ahriman Cupboard" },
      { 6565u, "Sylphic Cupboard" },
      { 6566u, "Riviera Bookshelf" },
      { 6567u, "Glade Bookshelf" },
      { 6568u, "Oasis Bookshelf" },
      { 6569u, "Manor Bookshelf" },
      { 6570u, "Ahriman Bookshelf" },
      { 6571u, "Sylphic Bookshelf" },
      { 6572u, "Morbol Bookshelf" },
      { 6573u, "Riviera Pillar" },
      { 6574u, "Glade Pillar" },
      { 6575u, "Oasis Pillar" },
      { 6576u, "Riviera Partition" },
      { 6577u, "Glade Partition" },
      { 6578u, "Oasis Partition" },
      { 6579u, "Mahogany Screen" },
      { 6580u, "Ahriman Screen" },
      { 6581u, "Sylphic Screen" },
      { 6582u, "Morbol Screen" },
      { 6583u, "Riviera Floor Lamp" },
      { 6584u, "Glade Floor Lamp" },
      { 6585u, "Oasis Floor Lamp" },
      { 6586u, "Manor Candelabra" },
      { 6587u, "Tonberry Floor Lamp" },
      { 6588u, "Mandragora Floor Lamp" },
      { 6589u, "Amigo Cactus Floor Lamp" },
      { 6590u, "Deluxe Manor Fireplace" },
      { 6591u, "Riviera Bathtub" },
      { 6592u, "Glade Bathtub" },
      { 6593u, "Oasis Bathtub" },
      { 6594u, "Terracotta Pot" },
      { 6595u, "Potted Maguey" },
      { 6596u, "Astroscope" },
      { 6597u, "Vodoriga Sculpture" },
      { 6598u, "Gargoyle Sculpture" },
      { 6599u, "Llymlaen's Embrace" },
      { 6600u, "Miniature Aetheryte" },
      { 6601u, "Riviera Round Table" },
      { 6602u, "Oasis Round Table" },
      { 6603u, "Glade Round Table" },
      { 6604u, "Ahriman Round Table" },
      { 6605u, "Diagnostics Module" },
      { 6606u, "Riviera Table" },
      { 6607u, "Glade Table" },
      { 6608u, "Oasis Table" },
      { 6609u, "Sylphic Table" },
      { 6610u, "Riviera Dining Table" },
      { 6611u, "Glade Dining Table" },
      { 6612u, "Oasis Dining Table" },
      { 6613u, "Walnut Dining Table" },
      { 6614u, "Sylphic Dining Table" },
      { 6615u, "Manor Table" },
      { 6616u, "Riviera Desk" },
      { 6617u, "Oasis Desk" },
      { 6618u, "Glade Desk" },
      { 6619u, "Manor Desk" },
      { 6620u, "Knight Captain's Desk" },
      { 6621u, "Walnut Cartonnier" },
      { 6622u, "Glade Cartonnier" },
      { 6623u, "Glade Sideboard" },
      { 6624u, "Glade Drawer Table" },
      { 6625u, "Manor Flower Stand" },
      { 6626u, "Ahriman Sideboard" },
      { 6627u, "Riviera Stall" },
      { 6628u, "Glade Stall" },
      { 6629u, "Oasis Stall" },
      { 6630u, "Riviera Counter" },
      { 6631u, "Glade Counter" },
      { 6632u, "Oasis Counter" },
      { 6633u, "Sylphic Counter" },
      { 6634u, "Manor Fireplace" },
      { 6635u, "Brass Pricket" },
      { 6636u, "Riviera Lantern" },
      { 6637u, "Glade Lantern" },
      { 6638u, "Oasis Lantern" },
      { 6639u, "Riviera Flower Vase" },
      { 6640u, "Glade Flower Vase" },
      { 6641u, "Oasis Flower Vase" },
      { 6642u, "Sylphic Flower Vase" },
      { 6643u, "Ahriman Flower Vase" },
      { 6644u, "Potted Axilflower" },
      { 6645u, "Potted Azalea" },
      { 6646u, "Potted Spider Plant" },
      { 6647u, "Table Chronometer" },
      { 6648u, "Ahriman Chronometer" },
      { 6649u, "Ink & Quill" },
      { 6650u, "Weighing Scale" },
      { 6651u, "Gemscope" },
      { 6652u, "Magitek Reaper Miniature" },
      { 6653u, "Stuffed Moogle" },
      { 6654u, "Stuffed Chocobo" },
      { 6655u, "Drinking Apkallu" },
      { 6656u, "Maelstrom Banner" },
      { 6657u, "Twin Adder Banner" },
      { 6658u, "Immortal Flames Banner" },
      { 6659u, "Lominsan Cityscape" },
      { 6660u, "Gridanian Cityscape" },
      { 6661u, "Ul'dahn Cityscape" },
      { 6662u, "Eorzean Map" },
      { 6663u, "Galleass Wheel" },
      { 6664u, "Demon Wall Sculpture" },
      { 6665u, "Inferno Wall Lamp" },
      { 6666u, "Blazing Inferno Wall Lamp" },
      { 6667u, "Sylphic Wall Lantern" },
      { 6668u, "Nymian Wall Lantern" },
      { 6669u, "Amdapori Wall Lantern" },
      { 6670u, "Belah'dian Crystal Lantern" },
      { 6671u, "Wall Chronometer" },
      { 6672u, "Behemoth Wall Trophy" },
      { 6673u, "Riviera Rug" },
      { 6674u, "Riviera Fringed Rug" },
      { 6675u, "Riviera Oval Rug" },
      { 6676u, "Glade Rug" },
      { 6677u, "Glade Fringed Rug" },
      { 6678u, "Glade Blossom Rug" },
      { 6679u, "Oasis Rug" },
      { 6680u, "Oasis Argyle Rug" },
      { 6681u, "Oasis Leaf Rug" },
      { 6682u, "Sheep Rug" },
      { 6683u, "Goobbue Rug" },
      { 6684u, "Moogle Rug" },
      { 6685u, "Morbol Rug" },
      { 6686u, "Amalj'aa Supply Carriage" },
      { 6687u, "Amalj'aa Pavis Shield" },
      { 7064u, "Summoning Bell" },
      { 7065u, "Woodworking Bench" },
      { 7066u, "Smithing Bench" },
      { 7067u, "Armorcraft Forge" },
      { 7068u, "Goldsmithing Bench" },
      { 7069u, "Leatherworking Bench" },
      { 7070u, "Clothcraft Loom" },
      { 7071u, "Alchemy Furnace" },
      { 7072u, "Cooking Stove" },
      { 7073u, "Riviera Table Chronometer" },
      { 7074u, "Glade Wall Chronometer" },
      { 7075u, "Tonberry Round Table" },
      { 7076u, "Tonberry Dining Table" },
      { 7077u, "Tonberry Armchair" },
      { 7078u, "Tonberry Couch" },
      { 7079u, "Tonberry Bookshelf" },
      { 7080u, "Tonberry Bed" },
      { 7081u, "Tonberry Screen" },
      { 7082u, "Tonberry Wall Lantern" },
      { 7083u, "Tonberry Oval Rug" },
      { 7084u, "Lalafellin Step Stool" },
      { 7085u, "Stuffed Ahriman" },
      { 7086u, "Ahriman Vase" },
      { 7087u, "Carbuncle Round Table" },
      { 7088u, "Carbuncle Armchair" },
      { 7089u, "Carbuncle Desk" },
      { 7090u, "Carbuncle Chair" },
      { 7091u, "Carbuncle Wardrobe" },
      { 7092u, "Carbuncle Rug" },
      { 7093u, "Carbuncle Chronometer" },
      { 7094u, "Carbuncle Lantern" },
      { 7095u, "Admiral Portrait" },
      { 7096u, "Sultana and Flame General Portrait" },
      { 7097u, "Elder Seedseer Portrait" },
      { 7098u, "Looming Tonberry Screen" },
      { 7099u, "Tonberry Square Rug" },
      { 7100u, "Projection Module" },
      { 7101u, "Purification Module" },
      { 7102u, "Eorzean Map Table" },
      { 7103u, "Dragon Piece" },
      { 7104u, "Wyvern Piece" },
      { 7105u, "Vigil Piece" },
      { 7106u, "Temple Knight Piece" },
      { 7107u, "Dragoon Piece" },
      { 7108u, "Manor Harpsichord" },
      { 7109u, "Manor Music Stool" },
      { 7110u, "Manor Timpani" },
      { 7111u, "Manor Marching Horn" },
      { 7112u, "Manor Cello" },
      { 7113u, "Manor Harp" },
      { 7114u, "Manor Music Stand" },
      { 7115u, "Regal Letter Box" },
      { 7116u, "Carbuncle Garden Table" },
      { 7117u, "Carbuncle Garden Bench" },
      { 7118u, "Riviera Striking Dummy" },
      { 7119u, "Oasis Striking Dummy" },
      { 7120u, "Glade Striking Dummy" },
      { 7121u, "Alps Striking Dummy" },
      { 7122u, "Sahagin Living Lamp" },
      { 7123u, "Sahagin Hanging Larder" },
      { 7124u, "Ixali Shelter" },
      { 7125u, "Ixali Banner" },
      { 7126u, "Automaton Digger" },
      { 7127u, "Kobold Furnace" },
      { 7128u, "Round Garden Patch" },
      { 7129u, "Oblong Garden Patch" },
      { 7130u, "Deluxe Garden Patch" },
      { 7152u, "Sultana Portrait" },
      { 7153u, "Material Supplier Permit" },
      { 7154u, "Junkmonger Permit" },
      { 7156u, "Mender Permit" },
      { 7958u, "Riviera Wall Shelf" },
      { 7959u, "Gourmet Lunch" },
      { 7960u, "Gourmet Supper" },
      { 7961u, "Afternoon Tea Set" },
      { 7962u, "Assorted Fruit" },
      { 7963u, "Chilled Red" },
      { 7964u, "The Five Ages" },
      { 7965u, "Classified Documents" },
      { 7966u, "Stuffed Goblin" },
      { 7967u, "Ultima Weapon Miniature" },
      { 7968u, "Onus of an Engineer" },
      { 7969u, "Enquire Within" },
      { 7970u, "Wall Planter" },
      { 7971u, "Encyclopedia Eorzea" },
      { 7972u, "Open Hearth" },
      { 7973u, "Riviera Half Partition" },
      { 7974u, "Potted Umbrella Fig" },
      { 7975u, "Potted Dragon Tree" },
      { 7976u, "Model Star Globe" },
      { 7977u, "Tatami Mat" },
      { 7978u, "Corner Counter" },
      { 7979u, "Planter Partition" },
      { 7980u, "Plasma Lamp" },
      { 7981u, "Lord of Levin Lamp" },
      { 7982u, "Emperor's Throne" },
      { 7983u, "Riviera Doormat" },
      { 7984u, "Double Feather Bed" },
      { 7985u, "Chocobo Pillar" },
      { 7986u, "Riviera Cushion" },
      { 7987u, "Glade Cushion" },
      { 7988u, "Oasis Cushion" },
      { 7989u, "Chocobo Stable" },
      { 7990u, "Moonfire Lantern" },
      { 7991u, "Ornamental Bamboo" },
      { 7992u, "Rounded Shrub" },
      { 7993u, "Lattice Planter" },
      { 7994u, "Water Lilies" },
      { 7995u, "Garden Pond" },
      { 7996u, "Cast-iron Cookpot" },
      { 7997u, "Stepladder" },
      { 7998u, "Straight Stepping Stones" },
      { 7999u, "Curved Stepping Stones" },
      { 8000u, "Straight Stepping Stumps" },
      { 8001u, "Curved Stepping Stumps" },
      { 8002u, "Planter Box" },
      { 8003u, "Wooden Bucket" },
      { 8727u, "Odin Miniature" },
      { 8728u, "Bahamut Miniature" },
      { 8729u, "Stuffed Tonberry" },
      { 8730u, "A Northerly Wind" },
      { 8731u, "The Antecedent" },
      { 8777u, "South Seas Armchair" },
      { 8778u, "South Seas Couch" },
      { 8779u, "Riviera Canopy Bed" },
      { 8780u, "Glade Canopy Bed" },
      { 8781u, "Oasis Canopy Bed" },
      { 8782u, "Potted Oliphant's Ear" },
      { 8783u, "Shower Stand" },
      { 8784u, "Head of the Dreadwyrm" },
      { 8785u, "Syrcus Tower" },
      { 8786u, "Riviera Stool" },
      { 8787u, "Glade Stool" },
      { 8788u, "Oasis Stool" },
      { 8789u, "Armoire" },
      { 8790u, "Masonwork Stove" },
      { 8791u, "Eternity Cake" },
      { 8792u, "Empty Twinkleboxes" },
      { 8793u, "Riviera Open-shelf Bookcase" },
      { 8794u, "Glade Open-shelf Bookcase" },
      { 8795u, "Oasis Open-shelf Bookcase" },
      { 8796u, "Shipping Crate" },
      { 8797u, "Wine Barrel" },
      { 8798u, "Incensory" },
      { 8799u, "Sheep Dolls" },
      { 8800u, "Hard Rice Cakes" },
      { 8801u, "Stuffed Qiqirn" },
      { 8802u, "Starlight Dodo" },
      { 8803u, "Pumpkin Basket" },
      { 8804u, "Starlight Cake" },
      { 8805u, "Wall Lantern" },
      { 8806u, "Manor Sofa" },
      { 8807u, "Stuffed Mazlaya Marlin" },
      { 8808u, "Megalodon Jaws" },
      { 8809u, "Stuffed Takitaro" },
      { 8810u, "Starlight Wreath" },
      { 8811u, "Towel Hanger" },
      { 8812u, "Adventurers' Guild Aduyses" },
      { 8813u, "Glade Doormat" },
      { 8814u, "Oasis Doormat" },
      { 8815u, "Large Woven Rug" },
      { 8816u, "Large Planter Box" },
      { 8817u, "Riviera Path Light" },
      { 8818u, "Glade Path Light" },
      { 8819u, "Oasis Path Light" },
      { 8820u, "Glade Lamppost" },
      { 8821u, "Oasis Lamppost" },
      { 8822u, "Pumpkin Candlestand" },
      { 8823u, "Star-topped Starlight Sentinel" },
      { 8824u, "Moon-topped Starlight Sentinel" },
      { 8825u, "Sun-topped Starlight Sentinel" },
      { 8826u, "Snowman" },
      { 8827u, "Empty Gift Boxes" },
      { 8828u, "Kadomatsu" },
      { 8837u, "Expansion of Duties Permit" },
      { 8838u, "Level 1 Aetherial Wheel Stand" },
      { 9462u, "Level 2 Aetherial Wheel Stand" },
      { 9463u, "Level 3 Aetherial Wheel Stand" },
      { 9464u, "Level 4 Aetherial Wheel Stand" },
      { 9465u, "Level 5 Aetherial Wheel Stand" },
      { 9466u, "Level 6 Aetherial Wheel Stand" },
      { 9717u, "Pine Bonsai" },
      { 9718u, "Longcase Chronometer" },
      { 9719u, "Oriental Bathtub" },
      { 9720u, "Frolicking Beasts" },
      { 9722u, "Kotatsu Table" },
      { 9723u, "Wax Vegetables" },
      { 9724u, "Guildleve Counter" },
      { 9725u, "Egg Floor Lamp" },
      { 9726u, "Glade Tea Set" },
      { 9727u, "Riviera Supper" },
      { 9728u, "Oasis Breakfast" },
      { 9729u, "Scattered Documents" },
      { 9730u, "Oriental Altar" },
      { 9731u, "Oriental Wall Scroll" },
      { 9732u, "Oriental Partition" },
      { 9733u, "Dead Man's Chest" },
      { 9734u, "Oak Low Barrel Planter" },
      { 9735u, "Valentione's Cake" },
      { 9736u, "Royal Partition" },
      { 9737u, "Order of Nald'thal Lectern" },
      { 9738u, "Green Tea Set" },
      { 9739u, "Dried Well" },
      { 9740u, "Oriental Striking Dummy" },
      { 9741u, "Eastern Cherry Tree" },
      { 9742u, "Crimson Felt Mat" },
      { 9743u, "Flame of Passion" },
      { 9744u, "Wild Banana Tree" },
      { 9745u, "Personal Astroscope" },
      { 9746u, "Riviera Garden Table Set" },
      { 9747u, "Dodo's Nest" },
      { 9748u, "Retainer Cartonnier" },
      { 9749u, "Retainer Counter" },
      { 12084u, "Alpine Chair" },
      { 12085u, "Alpine Round Table" },
      { 12086u, "Alpine Tea Set" },
      { 12087u, "Stuffed Carbuncle" },
      { 12088u, "Oriental Breakfast" },
      { 12089u, "Nameday Cake" },
      { 12090u, "Cake Tray" },
      { 12091u, "Oriental Supper" },
      { 12092u, "Riviera Lunch" },
      { 12093u, "Oriental Wind Chime" },
      { 12094u, "Toy Box" },
      { 12095u, "Glade Half Partition" },
      { 12096u, "Oasis Half Partition" },
      { 12097u, "Cask Rack" },
      { 12098u, "Display Stand" },
      { 12099u, "Miniature Gold Saucer" },
      { 12100u, "Royal Plotting Table" },
      { 12101u, "Round Banquet Table" },
      { 12102u, "Gold Saucer Corner Counter" },
      { 12103u, "Bearskin Rug" },
      { 12104u, "Gordian Bureau" },
      { 12105u, "Savage Gordian Bureau" },
      { 12106u, "Gordian Chair" },
      { 12107u, "Savage Gordian Chair" },
      { 12108u, "Star Sapphire Music Box" },
      { 12109u, "Star Ruby Music Box" },
      { 12110u, "Triple Triad Board" },
      { 12111u, "Expanse Fountain" },
      { 12112u, "Boundless Expanse Fountain" },
      { 12113u, "Topiary Moogle" },
      { 12114u, "Standing Signboard" },
      { 12115u, "Laundry Pole" },
      { 12234u, "Lupin Plot" },
      { 12235u, "Explorer's Campfire" },
      { 12670u, "Old World Striking Dummy" },
      { 12671u, "Sunflower Plot" },
      { 13055u, "Steward Permit" },
      { 13061u, "Crystal Bell" },
      { 13062u, "Stuffed Succubus" },
      { 13063u, "Pumpkin Desk" },
      { 13064u, "Starlight Ornament" },
      { 13065u, "Twin Star Ornament" },
      { 13066u, "Starlight Sentinel" },
      { 13067u, "Oriental Orange Basket" },
      { 13068u, "Paissa Doll" },
      { 13069u, "The Unending Journey" },
      { 13070u, "Oriental Tea Set" },
      { 13071u, "Manor Oven" },
      { 13072u, "Manor Counter" },
      { 13073u, "Stack of Tomes" },
      { 13074u, "Pile of Tomes" },
      { 13075u, "Oriental Round Table" },
      { 13076u, "Mhachi Coffin" },
      { 13077u, "Funguar Chair" },
      { 13078u, "Mahogany Partition Door" },
      { 13079u, "Pumpkin Chair" },
      { 13080u, "Broken Heart Chair (Right)" },
      { 13081u, "Broken Heart Chair (Left)" },
      { 13082u, "Paramour Bed" },
      { 13083u, "Hatchling Lamp" },
      { 13084u, "Topiary Chocobo" },
      { 13085u, "Ultimate Kettle" },
      { 13086u, "Ultimate Kettle Zenith" },
      { 13087u, "Ultimate Kettle Nexus" },
      { 13088u, "Sephirot Root" },
      { 13089u, "Sephirot Tree" },
      { 13090u, "Oriental Grass Plot" },
      { 13091u, "Dravanian Down Tree" },
      { 13285u, "Thorne Dynasty Mantelshelf" },
      { 13289u, "A Knight to Remember" },
      { 13335u, "Topaz Carbuncle Lamp" },
      { 13336u, "Premium Stuffed Moogle" },
      { 13337u, "Dreadwyrm Miniature" },
      { 13339u, "Starlight Celebration MMXIV Advertisement" },
      { 13341u, "Ruby Carbuncle Lamp" },
      { 13342u, "Emerald Carbuncle Lamp" },
      { 13354u, "Cold Knight's Cookfire" },
      { 13355u, "Oriental Snowcave" },
      { 14008u, "Riviera Vase" },
      { 14009u, "Glade Vase" },
      { 14010u, "Oasis Vase" },
      { 14045u, "Orchestrion" },
      { 14047u, "Stuffed Onion Prince" },
      { 14048u, "Pudding Floor Lamp" },
      { 14049u, "Pudding Throw Rug" },
      { 14050u, "Pudding Settee" },
      { 14051u, "Pudding Desk" },
      { 14052u, "Alpine Supper Set" },
      { 14053u, "High House Supper Set" },
      { 14054u, "Senor Sabotender Trophy" },
      { 14055u, "Riviera Flowerpot" },
      { 14056u, "Glade Flowerpot" },
      { 14057u, "Oasis Flowerpot" },
      { 14058u, "Dinosaur Skull" },
      { 14059u, "Dinosaur Skeleton" },
      { 14060u, "Dinosaur Tailbone" },
      { 14061u, "Topiary Sabotender" },
      { 14062u, "Archon Egg Tower" },
      { 14063u, "Dandelion Plot" },
      { 14064u, "Living Arch" },
      { 14065u, "Shishi-odoshi" },
      { 14066u, "Faust Striking Dummy" },
      { 14067u, "Gnathic Lamp Tree" },
      { 14900u, "Stuffed Eggplant Knight" },
      { 14901u, "Stuffed Garlic Jester" },
      { 14902u, "Stuffed Tomato King" },
      { 14903u, "Stuffed Mandragora Queen" },
      { 15134u, "Storm Trophy (Left)" },
      { 15135u, "Storm Trophy (Center)" },
      { 15136u, "Storm Trophy (Right)" },
      { 15137u, "Serpent Trophy (Left)" },
      { 15138u, "Serpent Trophy (Center)" },
      { 15139u, "Serpent Trophy (Right)" },
      { 15140u, "Flame Trophy (Left)" },
      { 15141u, "Flame Trophy (Center)" },
      { 15142u, "Flame Trophy (Right)" },
      { 15143u, "Staggered Shelf" },
      { 15144u, "Washbasin" },
      { 15145u, "Stuffed Bear" },
      { 15146u, "Alpine Breakfast" },
      { 15147u, "Hat Stand" },
      { 15148u, "Ashtray" },
      { 15149u, "Indirect Lighting" },
      { 15150u, "Indirect Stand Lighting" },
      { 15151u, "Chirurgeon's Curtain" },
      { 15152u, "Nidhogg Miniature" },
      { 15153u, "Markworthy Mogsofa" },
      { 15154u, "Dragon Floor Lamp" },
      { 15155u, "Season One Lone Wolf Trophy" },
      { 15156u, "Rising Banner" },
      { 15157u, "Beach Chair" },
      { 15158u, "Morning Glory Plot" },
      { 15159u, "Large Garden Pond" },
      { 15160u, "Oriental Wood Bridge" },
      { 15161u, "Oriental Deck" },
      { 15162u, "Pumpkin Carriage" },
      { 15163u, "Firefly Lamp" },
      { 15265u, "Season One Pack Wolf Trophy" },
      { 15266u, "Rising Pillar" },
      { 15775u, "Tier 1 Aquarium" },
      { 15776u, "Tier 2 Aquarium" },
      { 15777u, "Tier 3 Aquarium" },
      { 15778u, "Tier 4 Aquarium" },
      { 15953u, "Riviera Chocobo Stable" },
      { 15954u, "Glade Chocobo Stable" },
      { 15955u, "Oasis Chocobo Stable" },
      { 15956u, "Compact Level 6 Aetherial Wheel Stand" },
      { 15957u, "Tiny Bronco Miniature" },
      { 15958u, "Invincible Miniature" },
      { 15959u, "Enterprise Miniature" },
      { 15960u, "Invincible II Miniature" },
      { 15961u, "Odyssey Miniature" },
      { 15962u, "Tatanora Miniature" },
      { 15963u, "Viltgance Miniature" },
      { 15964u, "Sophia Miniature" },
      { 15965u, "Alexander Miniature" },
      { 15966u, "Happy New Chocobo" },
      { 15967u, "Stuffed Calca" },
      { 15968u, "Stuffed Brina" },
      { 15969u, "Mammet Lantern" },
      { 15970u, "Mounted Box Shelf" },
      { 15971u, "Knightly Round Table" },
      { 15973u, "Pumpkin Stew" },
      { 15974u, "Mounted Bookshelf" },
      { 15975u, "Great Gubal Floor Lamp" },
      { 15976u, "Combed Wool Rug" },
      { 15977u, "Spirits Collection" },
      { 15978u, "Starlight Pillar" },
      { 15979u, "Season Two Lone Wolf Trophy" },
      { 15980u, "Season Two Pack Wolf Trophy" },
      { 15981u, "Autumnal Maple Tree" },
      { 15982u, "Autumnal Ginkgo Tree" },
      { 15983u, "Maple Leaf Pile" },
      { 15984u, "Ginkgo Leaf Pile" },
      { 15985u, "Snow Drift" },
      { 15986u, "Firecrest Trophy" },
      { 15987u, "Glass Pumpkin Trophy" },
      { 15988u, "Deluxe Pumpkin Basket" },
      { 15989u, "Deluxe Stuffed Succubus" },
      { 15990u, "Deluxe Pumpkin Desk" },
      { 15991u, "Deluxe Pumpkin Chair" },
      { 15992u, "Empty Sparkleboxes" },
      { 15993u, "Jumbo Stuffed Qiqirn" },
      { 15994u, "Jumbo Starlight Wreath" },
      { 15995u, "Jumbo Starlight Ornament" },
      { 15996u, "Jumbo Twin Star Ornament" },
      { 15997u, "Jumbo Starlight Sentinel" },
      { 15998u, "Jumbo Sheep Dolls" },
      { 15999u, "Jumbo Hard Rice Cakes" },
      { 16000u, "Jumbo Oriental Orange Basket" },
      { 16001u, "Jumbo Paissa Doll" },
      { 16002u, "Deluxe Pumpkin Candlestand" },
      { 16003u, "Jumbo Star-topped Starlight Sentinel" },
      { 16004u, "Jumbo Moon-topped Starlight Sentinel" },
      { 16005u, "Jumbo Sun-topped Starlight Sentinel" },
      { 16006u, "Jumbo Snowman" },
      { 16007u, "Empty Present Boxes" },
      { 16008u, "Jumbo Kadomatsu" },
      { 16147u, "Deluxe Oriental Wind Chime" },
      { 16149u, "Deluxe Moonfire Lantern" },
      { 16150u, "Deluxe Ornamental Bamboo" },
      { 16151u, "Deluxe Wooden Bucket" },
      { 16704u, "Materia Melder Permit" },
      { 16779u, "Indirect Wall Lighting" },
      { 16780u, "Palace Pedestal" },
      { 16781u, "Troupe Stage" },
      { 16782u, "Round Stage" },
      { 16935u, "Grade 1 Picture Frame" },
      { 16936u, "Grade 2 Picture Frame" },
      { 16937u, "Grade 3 Picture Frame" },
      { 16969u, "Material Supplier Permit A-2" },
      { 16970u, "Junkmonger Permit A-2" },
      { 16971u, "Mender Permit A-2" },
      { 17003u, "Stuffed Baby Behemoth" },
      { 17013u, "Zurvan Miniature" },
      { 17014u, "Diabolos Miniature" },
      { 17015u, "Scathach Miniature" },
      { 17016u, "Paramour Vase" },
      { 17018u, "Forever Friends" },
      { 17019u, "Plundered Treasure" },
      { 17020u, "Yarn Basket" },
      { 17021u, "Riviera Breakfast" },
      { 17022u, "Little Ladies' Day MMXVI Advertisement" },
      { 17023u, "Paissa Floor Lamp" },
      { 17024u, "Merchant's Shelf" },
      { 17025u, "Ivy Pillar" },
      { 17026u, "Season Three Lone Wolf Trophy" },
      { 17027u, "Season Three Pack Wolf Trophy" },
      { 17028u, "White Partition" },
      { 17029u, "Eastern Cherry Petal Pile" },
      { 17030u, "Topiary Paissa" },
      { 17031u, "Moongrass Plot" },
      { 17037u, "Authentic Broken Heart Chair (Right)" },
      { 17038u, "Authentic Broken Heart Chair (Left)" },
      { 17039u, "Authentic Paramour Bed" },
      { 17040u, "Authentic Thorne Dynasty Mantelshelf" },
      { 17041u, "Authentic Egg Floor Lamp" },
      { 17042u, "Authentic Senor Sabotender Trophy" },
      { 17043u, "Authentic Flame of Passion" },
      { 17044u, "Authentic Eastern Cherry Tree" },
      { 17045u, "Authentic Felt Mat" },
      { 17046u, "Authentic Archon Egg Tower" },
      { 17137u, "Oriental Chocobo Stable" },
      { 17575u, "Stuffed Sabotender" },
      { 17677u, "Season Four Lone Wolf Trophy" },
      { 17678u, "Season Four Pack Wolf Trophy" },
      { 17953u, "Carbuncle Bed" },
      { 17954u, "Table Orchestrion" },
      { 17955u, "Mounted Flower Vase" },
      { 17956u, "Stuffed Dhalmel" },
      { 17957u, "Stuffed Gagana" },
      { 17958u, "Oriental Pipe Box" },
      { 17959u, "Oriental Dressing Case" },
      { 17960u, "Doman Partition" },
      { 17961u, "Lakshmi Miniature" },
      { 17962u, "Susano Miniature" },
      { 17963u, "Season Five Lone Wolf Trophy" },
      { 17965u, "Stuffed Edda" },
      { 17966u, "Rising Balloon" },
      { 17967u, "Oriental Sushi Lunch" },
      { 17968u, "Oriental Soba Lunch" },
      { 17969u, "Oriental Udon Lunch" },
      { 17970u, "Oriental Rice Ball Lunch" },
      { 17971u, "Easel" },
      { 17972u, "Elixir Bottle" },
      { 17973u, "Zabuton Cushion" },
      { 17974u, "White Half Partition" },
      { 17975u, "Moonfire Faire MMXIV Advertisement" },
      { 17976u, "Evercold Shaved Ice" },
      { 17977u, "Unmoving Maneki Moogle" },
      { 17978u, "Matanga Lamp" },
      { 17979u, "Stuffed Khloe" },
      { 17980u, "Stuffed Fox" },
      { 17981u, "Ixion Trophy" },
      { 17982u, "Eastern Pine" },
      { 17983u, "Eastern Teahouse Bench" },
      { 17984u, "Topiary Carbuncle" },
      { 17985u, "Moai Statue" },
      { 17986u, "Dataqi Standard" },
      { 17987u, "Gyr Abanian Striking Dummy" },
      { 20305u, "Stuffed Dog" },
      { 20306u, "Yugiri and Gosetsu Sketch" },
      { 20307u, "Morin Khuur" },
      { 20310u, "Imitation Square Window" },
      { 20623u, "Limited Edition Rising Banner" },
      { 20624u, "Limited Edition Firefly Lamp" },
      { 20625u, "Limited Edition Rising Pillar" },
      { 20639u, "Mol Banner" },
      { 20726u, "Hingan Desk" },
      { 20727u, "Hingan Sitting Table" },
      { 20728u, "Hingan Bench" },
      { 20729u, "Hingan Chair" },
      { 20730u, "Hingan Sofa" },
      { 20731u, "Hingan Bed" },
      { 20732u, "Hingan Sideboard" },
      { 20733u, "Hingan Wardrobe" },
      { 20734u, "Hingan Bookshelf" },
      { 20735u, "Hingan Cupboard" },
      { 20736u, "Hingan Open-shelf Bookcase" },
      { 20737u, "Hingan Oven" },
      { 20738u, "Hingan Fire Pit" },
      { 20739u, "Paper Partition" },
      { 20740u, "Bamboo Planter" },
      { 20741u, "Hingan Andon Lamp" },
      { 20742u, "Hingan Chochin Lantern" },
      { 20743u, "Hingan Wall Scroll" },
      { 20744u, "Decorative Wall Fan" },
      { 20745u, "Kimono Hanger" },
      { 20746u, "Crimson Coral Object" },
      { 20747u, "Kotetsu Replica" },
      { 20748u, "Hingan Armor Display" },
      { 20749u, "Hingan Brushes" },
      { 20750u, "Hingan Scroll" },
      { 20751u, "Porcelain Vase" },
      { 20752u, "Fool's Portal" },
      { 20753u, "Imitation Stained Crystal Ornament" },
      { 20754u, "Shinryu's Wing" },
      { 20755u, "Stuffed Kojin" },
      { 20756u, "Eggplant Knight Flower Vase" },
      { 20757u, "Garlic Jester Flower Vase" },
      { 20758u, "Mandragora Queen Flower Vase" },
      { 20759u, "Onion Prince Flower Vase" },
      { 20760u, "Tomato King Flower Vase" },
      { 20761u, "Pumpkin Pie Set" },
      { 20762u, "Pumpkin Pudding Set" },
      { 20763u, "Ghost Candlestand" },
      { 20764u, "All Saints' Wake MMXIII Advertisement" },
      { 20765u, "Pumpkin Tower" },
      { 20766u, "Starlight Stew Set" },
      { 20767u, "Starlight Log Set" },
      { 20768u, "Starlight Celebration MMXIII Advertisement" },
      { 20769u, "Starlight Sapling" },
      { 20770u, "Hatchling Lantern" },
      { 20771u, "Fat Chocobo Table Chronometer" },
      { 20772u, "Far Eastern Willow" },
      { 20773u, "Toro Lantern" },
      { 20814u, "Piping Hot Pumpkin Stew" },
      { 20815u, "Genuine Starlight Pillar" },
      { 20816u, "Authentic Starlight Celebration MMXIV Advertisement" },
      { 20817u, "Handmade Snow Drift" },
      { 20818u, "Handmade Happy New Chocobo" },
      { 21106u, "Blank Riviera Partition" },
      { 21107u, "Blank Glade Partition" },
      { 21108u, "Blank Oasis Partition" },
      { 21109u, "Blank Hingan Partition" },
      { 21199u, "Material Supplier Permit A-3" },
      { 21200u, "Junkmonger Permit A-3" },
      { 21201u, "Mender Permit A-3" },
      { 21207u, "Hingan Partition" },
      { 21308u, "Authentic Pumpkin Pie Set" },
      { 21309u, "Authentic Pumpkin Pudding Set" },
      { 21310u, "Authentic Ghost Candlestand" },
      { 21311u, "Authentic MMXIII All Saints' Wake Advertisement" },
      { 21312u, "Authentic Pumpkin Tower" },
      { 21313u, "Authentic Starlight Stew Set" },
      { 21314u, "Authentic Starlight Log Set" },
      { 21315u, "Authentic Starlight Celebration MMXIII Advertisement" },
      { 21316u, "Authentic Starlight Sapling" },
      { 21811u, "Futon Mattress" },
      { 21812u, "Hatching-tide Banner" },
      { 21813u, "Odder Otter Lantern" },
      { 21814u, "Boar's Head Kadomatsu" },
      { 21815u, "Moogle Wall Chronometer" },
      { 21816u, "Byakko Sideboard" },
      { 21817u, "Byakko Throne" },
      { 21818u, "Stuffed Spriggan" },
      { 21819u, "Season Six Lone Wolf Trophy" },
      { 21820u, "Valentione's Day MMXIV Advertisement" },
      { 21821u, "Valentione's Day Balloons" },
      { 21822u, "Bread Basket" },
      { 21823u, "Stuffed Alpha" },
      { 21824u, "House Borel Tea Set" },
      { 21825u, "Royal Dowager Tea Set" },
      { 21826u, "Brick Interior Wall" },
      { 21827u, "Mounted Cupboard" },
      { 21828u, "Vegetable Basket" },
      { 21829u, "Open Partition" },
      { 21830u, "Counter Cart" },
      { 21831u, "Sharlayan Cabinet" },
      { 21832u, "Sharlayan Wardrobe" },
      { 21833u, "Carbuncle Bathtub" },
      { 21834u, "Chirurgeon's Essentials" },
      { 21835u, "Kitchen Hanger" },
      { 21836u, "Hanya Mask" },
      { 21838u, "Sake Set" },
      { 21839u, "Stage Panel" },
      { 21840u, "Stuffed Ananta" },
      { 21841u, "Tier 1 Metal Aquarium" },
      { 21842u, "Tier 2 Metal Aquarium" },
      { 21843u, "Eorzean Lantern" },
      { 21844u, "Bomb Stove" },
      { 21845u, "Hingan Pillar" },
      { 21846u, "Stack of Documents" },
      { 21847u, "Mummer's Maquillage" },
      { 21848u, "Dress Hanger" },
      { 21849u, "Mummer's Wardrobe" },
      { 21850u, "Far Eastern Doll Display" },
      { 21851u, "Coat Hanger" },
      { 21852u, "Altar to Pazuzu" },
      { 21854u, "Hokonin Permit" },
      { 21855u, "Allagan Resupply Node Permit" },
      { 21856u, "Allagan Commerce Node Permit" },
      { 21857u, "Allagan Repair Node Permit" },
      { 21858u, "Allagan Housekeeping Node Permit" },
      { 21859u, "Ananta Material Supplier Permit" },
      { 21860u, "Ananta Junkmonger Permit" },
      { 21861u, "Ananta Mender Permit" },
      { 21862u, "Ananta Maidservant Permit" },
      { 21863u, "Rose Wagon" },
      { 21864u, "Corner Hedge Partition" },
      { 21865u, "Bamboo Copse" },
      { 21866u, "Hingan Hot Spring" },
      { 21870u, "Melodious Mogchestrion" },
      { 21871u, "Authentic Paramour Vase" },
      { 21872u, "Authentic Little Ladies' Day MMXVI Advertisement" },
      { 21873u, "Authentic Eastern Cherry Petal Pile" },
      { 22405u, "Authentic Hatching-tide Banner" },
      { 22406u, "Authentic Boar's Head Kadomatsu" },
      { 22407u, "Authentic Valentione's Day MMXIV Advertisement" },
      { 22408u, "Authentic Valentione's Day Balloons" },
      { 22409u, "Authentic Far Eastern Doll Display" },
      { 22410u, "Authentic Rose Wagon" },
      { 22440u, "Hedge Partition" },
      { 22520u, "Dodo Message Book Stand" },
      { 22521u, "Crystal Tower Striker" },
      { 22536u, "Merry Mog Bed" },
      { 22537u, "Mellow Mog Lamp" },
      { 22538u, "Odder Otter Dining Table" },
      { 22539u, "Odder Otter Screen" },
      { 22540u, "Odder Otter Chair" },
      { 22541u, "Odder Otter Wall Lantern" },
      { 22542u, "Crossed Halberds" },
      { 22543u, "Sewing Baskets" },
      { 22544u, "Season Seven Lone Wolf Trophy" },
      { 22545u, "Stuffed Tsukuyomi" },
      { 22546u, "Falcons Banner" },
      { 22547u, "Ravens Banner" },
      { 22548u, "Disordered Wardrobe" },
      { 22549u, "Laundry Basket" },
      { 22550u, "Leather Bags" },
      { 22551u, "Cozy Leather Shoes" },
      { 22552u, "Assorted Candles" },
      { 22553u, "Glade Bachelor's Chest" },
      { 22554u, "Portable Stepladder" },
      { 22555u, "Glade Partition Door" },
      { 22556u, "Oasis Partition Door" },
      { 22557u, "Wine Glass" },
      { 22558u, "Bar Counter" },
      { 22559u, "Bar Stool" },
      { 22560u, "Trouser Hanger" },
      { 22561u, "Chainmail Hanger" },
      { 22562u, "Sumptuous Dimension Castle Vase" },
      { 22563u, "Imposing Dimension Castle Vase" },
      { 22564u, "Stuffed Namazu" },
      { 22565u, "Season Eight Lone Wolf Trophy" },
      { 22566u, "Empyrean Shrine" },
      { 22567u, "Basket of Steamed Buns" },
      { 22568u, "Miniature Iloh" },
      { 22569u, "Southern Kitchen" },
      { 22570u, "Hingan Cleaning Supplies" },
      { 22571u, "Indoor BBQ Spit" },
      { 22572u, "Fool's Threshold" },
      { 22573u, "Ame-zaiku Set" },
      { 22574u, "Jellyfish Lamp" },
      { 22575u, "Curry Plate" },
      { 22576u, "Mosquito Moogle" },
      { 22577u, "Monster Toss Machine" },
      { 22578u, "Cuff-a-Cur Machine" },
      { 22579u, "Odder Otter Bench" },
      { 22580u, "Picnic Set" },
      { 22581u, "Miniature Heaven-on-High" },
      { 22582u, "Odder Otter Andon Lamp" },
      { 22583u, "BBQ Spit" },
      { 22584u, "Namazu Effigy" },
      { 22585u, "Namazu Material Supplier Permit" },
      { 22586u, "Namazu Junkmonger Permit" },
      { 22587u, "Namazu Mender Permit" },
      { 22588u, "Namazu Manservant Permit" },
      { 23097u, "Grand Chair" },
      { 23229u, "Stuffed Happy Bunny" },
      { 23230u, "Journeyman Salvager Permit" },
      { 23339u, "Authentic Moonfire Faire MMXIV Advertisement" },
      { 23340u, "Authentic Evercold Shaved Ice" },
      { 23341u, "Authentic Rising Balloon" },
      { 23846u, "Mannequin" },
      { 23856u, "Chocobo Round Table" },
      { 23857u, "Chocobo Chair" },
      { 23858u, "Odder Otter Round Andon Lamp" },
      { 23859u, "Chocobo Dining Table" },
      { 23860u, "Season Nine Lone Wolf Trophy" },
      { 23861u, "Season Ten Lone Wolf Trophy" },
      { 23862u, "Pot of Cream Stew" },
      { 23863u, "Suzaku Wall Hanging" },
      { 23864u, "Doman Dogwood" },
      { 23865u, "Enclave Map" },
      { 23866u, "Heavensturn Bounty" },
      { 23867u, "Pumpkin Party Pennants" },
      { 23868u, "Pumpkin Pastry Platter" },
      { 23869u, "Familiar Pawprints" },
      { 23870u, "Silver Starlight Sentinel" },
      { 23871u, "Starlight Donuts" },
      { 23872u, "Evercold Starlight Snowman" },
      { 23873u, "Opened Twinkleboxes" },
      { 23874u, "Redoubtable Rolanberry Tart" },
      { 23875u, "Crystal Boule" },
      { 23876u, "Gridanian Bouquet" },
      { 23877u, "Imitation Shuttered Window" },
      { 23878u, "Little Chef's Playset" },
      { 23879u, "Wooden Showcase" },
      { 23880u, "Unfinished Shawl" },
      { 23881u, "Adventuring Pack" },
      { 23882u, "Spice Rack" },
      { 23883u, "Bread Rack" },
      { 23884u, "Mahogany Bunk Bed" },
      { 23885u, "Handbasket" },
      { 23886u, "Strewn Smallclothes" },
      { 23887u, "Strewn Bottoms" },
      { 23888u, "Glass Jars" },
      { 23889u, "Wooden Plates" },
      { 23890u, "Wooden Cups" },
      { 23891u, "Mounted Plate Rack" },
      { 23892u, "Verdant Partition" },
      { 23893u, "Bathroom Floor Tiles" },
      { 23894u, "Bathroom Wall Tiles" },
      { 23895u, "Towels" },
      { 23896u, "Sofa Cushions" },
      { 23897u, "Birdbath" },
      { 23898u, "Garden Gravel" },
      { 23899u, "Brick Garden Circle" },
      { 24283u, "Exclusive Stuffed Dog" },
      { 24488u, "Paissa Chronometer" },
      { 24489u, "Paint Can" },
      { 24490u, "Emerald Crystal Boule" },
      { 24491u, "Mirific Mogshelf" },
      { 24492u, "Tier 3 Metal Aquarium" },
      { 24493u, "Tier 4 Metal Aquarium" },
      { 24494u, "Travel Paint Set" },
      { 24495u, "Seiryu Statuette" },
      { 24496u, "Valentione Lobster Platter" },
      { 24497u, "Valentione Cake Pairing" },
      { 24498u, "Eggcentric Crown Roast" },
      { 24499u, "Eggcentric Chocolate Cake" },
      { 24500u, "Archon Egg Pile" },
      { 24501u, "Small Imitation Window" },
      { 24502u, "Bag of Booty" },
      { 24503u, "Undersea Spoils" },
      { 24504u, "Level Checker Replica" },
      { 24505u, "Cerulean Crystal Boule" },
      { 24506u, "Fat Cat Bank" },
      { 24507u, "Tree Slice Tower" },
      { 24508u, "Botanist's Dried Herbs" },
      { 24509u, "Steppe Kitchen" },
      { 24510u, "Four Lords" },
      { 24511u, "Wooden Loft" },
      { 24512u, "Wooden Steps" },
      { 24513u, "Wooden Beam" },
      { 24514u, "White Rectangular Partition" },
      { 24515u, "Bowl of Oden" },
      { 24516u, "Fruit Stall" },
      { 24517u, "Vegetable Stall" },
      { 24518u, "Pastry Cupboard" },
      { 24519u, "Mineral Display" },
      { 24520u, "Fabric Rack" },
      { 24521u, "Potion Rack" },
      { 24522u, "Prism Pouch" },
      { 24523u, "Marimo Lamp" },
      { 24524u, "Honey Pot" },
      { 24525u, "Red Carpet" },
      { 24526u, "Hanging Planter Shelf" },
      { 24527u, "Plush Pile" },
      { 24528u, "Fat Cat Sofa" },
      { 24529u, "Enormous Stuffed Alpha" },
      { 24530u, "Stuffed Mammet" },
      { 24531u, "Festive Chirashi-zushi" },
      { 24532u, "Festive Sushi Balls" },
      { 24533u, "Season Eleven Lone Wolf Trophy" },
      { 24534u, "Season Twelve Lone Wolf Trophy" },
      { 24535u, "Simple Treehouse" },
      { 24536u, "Botanist's Garden" },
      { 24537u, "Big One Festival Float" },
      { 24617u, "Ananta Metalworks" },
      { 24618u, "Kojin Material Supplier Permit" },
      { 24619u, "Kojin Junkmonger Permit" },
      { 24620u, "Kojin Mender Permit" },
      { 24621u, "Kojin Manservant Permit" },
      { 27271u, "Lily Floor Lamp" },
      { 27272u, "Lily Wall Lamp" },
      { 27273u, "Marble Arch Partition" },
      { 27274u, "Simple Curtain" },
      { 27275u, "Messy Dishes" },
      { 27276u, "Butterfly Specimen" },
      { 27277u, "Grocery Bag" },
      { 27278u, "Hunting Supplies" },
      { 27279u, "War Chest" },
      { 27280u, "Imitation Stained Crystal Pane" },
      { 27281u, "Marble Partition" },
      { 27282u, "Crystarium Bench" },
      { 27283u, "Potted Orange Tree" },
      { 27284u, "Crystarium Stove" },
      { 27285u, "Crystarium Kitchen Hanger" },
      { 27286u, "Crystarium Teapot" },
      { 27287u, "Ruby Crystal Boule" },
      { 27288u, "Crystarium Wall Chronometer" },
      { 27289u, "Moonfire Faire MMXVIII Advertisement" },
      { 27290u, "All Saints' Wake MMXV Advertisement" },
      { 27291u, "Crystarium Mechanical Till" },
      { 27292u, "Stuffed Fuath" },
      { 27293u, "Antique Vessels" },
      { 27294u, "Decadent Fruit Platter" },
      { 27295u, "Sandwich Basket" },
      { 27296u, "Wooden Handrail" },
      { 27297u, "Misplaced Eyeglasses" },
      { 27298u, "Carpet of Flowers" },
      { 27299u, "Blades of Innocence" },
      { 27300u, "Titania Shadow Box" },
      { 27301u, "Season Thirteen Lone Wolf Trophy" },
      { 27302u, "Team Season Pack Wolf Trophy" },
      { 27303u, "Metal Work Lantern" },
      { 27304u, "Wooden Work Lantern" },
      { 27305u, "Alchemical Lantern" },
      { 27306u, "Liberation" },
      { 27307u, "The Legend Returns" },
      { 27308u, "Rise of a New Sun" },
      { 27309u, "Under the Moonlight" },
      { 27310u, "Prelude in Violet" },
      { 27311u, "Requiem for Heroes" },
      { 27312u, "Crystarium Umbrella Stand" },
      { 27313u, "Stuffed Clionid" },
      { 27314u, "Fallen Tree" },
      { 27315u, "Crystarium Striking Dummy" },
      { 27316u, "Firebloom" },
      { 27317u, "Seastone Brazier" },
      { 27318u, "Portable Pool" },
      { 27319u, "Wind Chime Stand" },
      { 27979u, "Authentic Mosquito Moogle" },
      { 27980u, "Authentic Curry Plate" },
      { 27981u, "Authentic Pumpkin Party Pennants" },
      { 27982u, "Authentic Pumpkin Pastry Platter" },
      { 27983u, "Authentic Familiar Pawprints" },
      { 28079u, "Season Fourteen Lone Wolf Trophy" },
      { 28080u, "Season Fifteen Lone Wolf Trophy" },
      { 28129u, "Starlight Goobbue Wreath" },
      { 28131u, "Indoor Oriental Garden" },
      { 28132u, "Oriental Chawan-mushi Lunch" },
      { 28133u, "Oasis Wall-mounted Fountain" },
      { 28134u, "Flooring Mat" },
      { 28136u, "Golden Ewer" },
      { 28137u, "Dishware" },
      { 28139u, "Imitation Long Window" },
      { 28140u, "Imposing Ishgardian Shelf" },
      { 28141u, "Stuffed Hraesvelgr" },
      { 28142u, "Misplaced Mog Slippers" },
      { 28143u, "Basket of Flowers" },
      { 28144u, "Hades Trophy" },
      { 28145u, "Ale Tap" },
      { 28146u, "Allagan Tomestone of Frivolity" },
      { 28147u, "Tabletop Hat Stand" },
      { 28148u, "Vanity Mirror" },
      { 28149u, "Necklace Display Stand" },
      { 28150u, "Empty Boxes" },
      { 28151u, "Set of Liquor Bottles" },
      { 28152u, "Starlight Celebration MMXVIII Advertisement" },
      { 28153u, "Starlight Roll Cake" },
      { 28154u, "Log Partition" },
      { 28155u, "Stuffed Giant Beaver" },
      { 28156u, "Stuffed Porxie" },
      { 28157u, "Handmade Akabeko" },
      { 28158u, "Chocolate Fountain" },
      { 28160u, "Vegetable Garden" },
      { 28161u, "Cedar Tree" },
      { 28162u, "Armorer's Stall" },
      { 28163u, "Weaponsmith's Stall" },
      { 28164u, "Jeweler's Stall" },
      { 28165u, "Florist's Stall" },
      { 28166u, "Baker's Stall" },
      { 28167u, "Fruiterer's Stall" },
      { 28168u, "Greengrocer's Stall" },
      { 28169u, "Bookseller's Stall" },
      { 28170u, "Enchanter's Stall" },
      { 28171u, "Butcher's Stall" },
      { 28501u, "Dragonsong Reproduction" },
      { 28502u, "Liberation Reproduction" },
      { 28503u, "Nightfall Reproduction" },
      { 28635u, "Greatwood Planter" },
      { 28636u, "Shirt Display Rack" },
      { 28637u, "Lakeland Chair" },
      { 28638u, "Lakeland Table" },
      { 28639u, "Leather Sofa" },
      { 28640u, "Il Mheg Flower Lamp" },
      { 28641u, "Macaron Cushion" },
      { 28642u, "Wooden Deck" },
      { 28911u, "Authentic Silver Starlight Sentinel" },
      { 28912u, "Authentic Starlight Donuts" },
      { 28913u, "Authentic Evercold Starlight Snowman" },
      { 28914u, "Authentic Opened Twinkleboxes" },
      { 28915u, "Scions of the Seventh Dawn Reproduction" },
      { 28931u, "Small Angler's Canvas" },
      { 28932u, "Medium Angler's Canvas" },
      { 28933u, "Large Angler's Canvas" },
      { 28934u, "Extra Large Angler's Canvas" },
      { 28963u, "Moogle Round Table" },
      { 28964u, "Modern Mogseat" },
      { 28965u, "Cosmetics Box" },
      { 28966u, "Hatching-tide MMXVIII Advertisement" },
      { 28967u, "Wine Rack" },
      { 28968u, "Rope Stanchion" },
      { 28969u, "Cardpaper Box" },
      { 28970u, "Dish Rack" },
      { 28971u, "Garment Rack" },
      { 28972u, "Amaurot Wall Lamp" },
      { 28973u, "Manor Dressing Table" },
      { 28974u, "Cheese Collection" },
      { 28975u, "Oldrose Wall Planter" },
      { 28976u, "Folded Futon" },
      { 28977u, "The Bismarck" },
      { 28978u, "Devil's Salad Bowl" },
      { 28979u, "Lord Commander Portrait" },
      { 28980u, "Golden Crystal Boule" },
      { 28981u, "Cosmos Partition" },
      { 28982u, "Wooden Table" },
      { 28983u, "Picnic Basket" },
      { 28984u, "First Chair's Trumpet" },
      { 28985u, "Sunflower Bouquet" },
      { 28986u, "Cutting Board" },
      { 28987u, "Open Book" },
      { 28988u, "Milk Carton" },
      { 28989u, "Azure Dragoon Portrait" },
      { 28990u, "Cosmos Decoration" },
      { 28991u, "Used Banquet Table" },
      { 28992u, "Steppe Bed" },
      { 28993u, "Stuffed Rabbit" },
      { 28994u, "Ruby Weapon Bust" },
      { 28995u, "Apron Rack" },
      { 28996u, "Eastern Stage" },
      { 28997u, "Little Ladies' Day Banner" },
      { 28998u, "Dohn Trellis" },
      { 28999u, "Lakeland Elf Tree" },
      { 29000u, "Lakeland Flower Garden" },
      { 29680u, "Mahogany Aqueduct" },
      { 29681u, "Crystallized Coral" },
      { 29682u, "White Screen" },
      { 29683u, "Siphon Coffee Brewer" },
      { 29684u, "Fat Cat Wall Chronometer" },
      { 29685u, "Bomb Cauldron" },
      { 29686u, "Ronkan Rocking Chair" },
      { 29687u, "Brick Garden Wall" },
      { 29688u, "Authentic Festive Chirashi-zushi" },
      { 29689u, "Authentic Festive Sushi Balls" },
      { 29690u, "Authentic Archon Egg Pile" },
      { 29691u, "Authentic Eggcentric Crown Roast" },
      { 29692u, "Authentic Eggcentric Chocolate Cake" },
      { 29980u, "Authentic Valentione Lobster Platter" },
      { 29981u, "Authentic Valentione Cake Pairing" },
      { 29982u, "Authentic Stuffed Mammet" },
      { 30381u, "Trophy of Light" },
      { 30382u, "Sapphire Weapon Bust" },
      { 30383u, "Verdant Shelf Partition" },
      { 30384u, "Ironfrog Keeper" },
      { 30385u, "Wooden Staircase Bookshelf" },
      { 30386u, "Gemseeker's Pack" },
      { 30387u, "Bluebird Mobile" },
      { 30388u, "Nomad's Tent" },
      { 30389u, "Stage Curtain" },
      { 30390u, "Floor Sofa" },
      { 30391u, "Tako-yaki Boat" },
      { 30393u, "Barrel Table" },
      { 30394u, "Indoor Pond" },
      { 30395u, "Giant Beaver Burger Set" },
      { 30396u, "Red Brick Kitchen" },
      { 30397u, "Cracked Arch Window" },
      { 30398u, "Industrial Wall Pipes" },
      { 30399u, "Season Sixteen Lone Wolf Trophy" },
      { 30400u, "Factory Grate" },
      { 30401u, "Factory Staircase" },
      { 30402u, "Factory Railing" },
      { 30403u, "Factory Beam" },
      { 30404u, "Factory Automatic Door" },
      { 30405u, "Factory Partition" },
      { 30406u, "Factory Wall Lamp" },
      { 30407u, "Factory Bench" },
      { 30408u, "Wooden Footbridge" },
      { 30409u, "Gulg Planter" },
      { 30410u, "Marble Alcove Bed" },
      { 30411u, "Watts's Sketches" },
      { 30412u, "Dwarven Floor Lamp" },
      { 30413u, "Mandragora Table Chronometer" },
      { 30414u, "Paissa Rug" },
      { 30415u, "Huggable Gaelicat" },
      { 30416u, "Ishgardian Display Stand" },
      { 30417u, "Fortemps Sofa" },
      { 30418u, "Stuffed Estinien" },
      { 30419u, "Helldiver SD" },
      { 30420u, "Dainsleif SD" },
      { 30421u, "Oriental Wooden Deck" },
      { 30422u, "Slanted Wall" },
      { 30423u, "Slanted Skylight" },
      { 30424u, "The Moogle's Paw Machine" },
      { 30425u, "Out on a Limb Machine" },
      { 30426u, "The Finer Miner Machine" },
      { 30427u, "Sliced Watermelon" },
      { 30428u, "Water Bomb Stand" },
      { 30429u, "Authentic Moonfire Faire MMXVIII Advertisement" },
      { 30430u, "Authentic Portable Pool" },
      { 30431u, "Authentic Wind Chime Stand" },
      { 30594u, "Stuffed Paissa Patissier" },
      { 30595u, "Stuffed Behelmeted Serpent" },
      { 30883u, "The Sultana's Seven" },
      { 31706u, "Authentic Copy of Liberation" },
      { 31707u, "Authentic Copy of The Legend Returns" },
      { 31708u, "Authentic Copy of Rise of a New Sun" },
      { 31709u, "Authentic Copy of Under the Moonlight" },
      { 31710u, "Authentic Copy of Prelude in Violet" },
      { 31711u, "Authentic Copy of Requiem for Heroes" },
      { 31712u, "Authentic Pumpkin Carriage" },
      { 31713u, "Authentic All Saints' Wake MMXV Advertisement" },
      { 32202u, "Grilled Eel Set" },
      { 32203u, "Goobbue Lamp" },
      { 32204u, "Dress Form" },
      { 32205u, "Tankard" },
      { 32206u, "Emerald Weapon Bust" },
      { 32207u, "Stuffed Kupka Kupp" },
      { 32208u, "Unmelting Ice Partition" },
      { 32209u, "Unmelting Ice Loft" },
      { 32210u, "Queen's Gunner Piece" },
      { 32211u, "Queen's Soldier Piece" },
      { 32212u, "Shakshouka Pot" },
      { 32213u, "Stuffed Cait Sith" },
      { 32214u, "Antique Sink" },
      { 32215u, "Spring Meadow Partition" },
      { 32216u, "Ishgardian Stove" },
      { 32217u, "Extra-stuffed Carbuncle" },
      { 32218u, "Carbuncle House Slippers" },
      { 32219u, "Iron Torch" },
      { 32220u, "Major-General Cushion" },
      { 32221u, "Rose Trellis" },
      { 32222u, "Dragon Icon" },
      { 32223u, "Royal Lion" },
      { 32224u, "Swag Valance" },
      { 32225u, "Rustic Chocobo Counter" },
      { 32226u, "Wood Slat Partition" },
      { 32227u, "Homemade Cookie Set" },
      { 32228u, "Florist's Counter" },
      { 32229u, "Paissa Swing Chair" },
      { 32230u, "Showcase" },
      { 32231u, "Shelving Showcase" },
      { 32232u, "Elemental Wheel" },
      { 32233u, "Toy Showcase" },
      { 32234u, "Apparel Showcase" },
      { 32235u, "Armament Showcase" },
      { 32236u, "Art Showcase" },
      { 32237u, "Baked Goods Showcase" },
      { 32238u, "The White Tiger" },
      { 32239u, "Queen's Warrior Piece" },
      { 32240u, "Queen's Knight Piece" },
      { 32241u, "Season Seventeen Lone Wolf Trophy" },
      { 32242u, "Toymaker's Show Window" },
      { 32243u, "Designer's Show Window" },
      { 32244u, "Smith's Show Window" },
      { 32245u, "Artist's Show Window" },
      { 32246u, "Baker's Show Window" },
      { 32247u, "Snow-dusted Tree" },
      { 32248u, "Parkside Tree" },
      { 32249u, "Brick Paving" },
      { 32250u, "La Noscean Grape Pergola" },
      { 32251u, "Fruitful Fountain" },
      { 32961u, "Authentic Starlight Celebration MMXVIII Advertisement" },
      { 32962u, "Authentic Starlight Roll Cake" },
      { 32963u, "Authentic Starlight Goobbue Wreath" },
      { 32964u, "Authentic Heavensturn Bounty" },
      { 32965u, "Authentic Chocolate Fountain" },
      { 32966u, "Authentic Eastern Stage" },
      { 32967u, "Authentic Little Ladies' Day Banner" },
      { 32968u, "Authentic Stuffed Rabbit" },
      { 32969u, "Authentic Hatching-tide MMXVIII Advertisement" },
      { 33251u, "Cactuar Stool" },
      { 33252u, "Cactuar Round Table" },
      { 33253u, "Diamond Weapon Bust" },
      { 33254u, "Queen's Rest" },
      { 33255u, "Hatching-tide Confections" },
      { 33256u, "Grilled Corn" },
      { 33257u, "Stuffed Peerifool" },
      { 33258u, "Eastern Indoor Pond" },
      { 33259u, "Grocery Counter Cart" },
      { 33260u, "Azim Steppe Phasmascape" },
      { 33261u, "Blue Moon Phasmascape" },
      { 33262u, "Stuffed Android Units" },
      { 33263u, "Wooden Reel Table" },
      { 33264u, "Violin Showcase" },
      { 33265u, "Aetherolectric Guitar" },
      { 33266u, "Pub Signboard" },
      { 33267u, "Windowed Partition" },
      { 33268u, "Hingan Windowed Partition" },
      { 33269u, "Mushroom Lamp" },
      { 33270u, "Sealed Letter" },
      { 33271u, "Wooden Spiral Staircase" },
      { 33272u, "Approved Ishgardian Lunch" },
      { 33273u, "Country Dining Set" },
      { 33274u, "Akadaemia Prospectuses" },
      { 33275u, "Icebox" },
      { 33276u, "Red Brick Counter" },
      { 33277u, "Necrologos Transcription" },
      { 33278u, "Hand-poured Coffee" },
      { 33279u, "Back Bar" },
      { 33280u, "Pixie Apple Basket" },
      { 33281u, "Philosopher's Stone Table" },
      { 33282u, "Crystarium Wardrobe" },
      { 33283u, "Low Bookshelf" },
      { 33284u, "Qiqirn Shelf" },
      { 33285u, "Mogbureau" },
      { 33286u, "Bar Rack" },
      { 33287u, "Portrait of Gestahl" },
      { 33288u, "Pepperoni Pizza Set" },
      { 33289u, "Archon Egg Pouch" },
      { 33290u, "Costa del Hielo" },
      { 33291u, "Goobbue Wall Chronometer" },
      { 33292u, "Armor Hanger" },
      { 33293u, "Mounted Dinosaur Skull" },
      { 33294u, "Plot of Hydrangeas" },
      { 33295u, "Glade Gazebo" },
      { 33296u, "Garden Tool Shelf" },
      { 33297u, "Eastern Wisteria" },
      { 33298u, "Moonfire Mask Stall" },
      { 33299u, "Forging Bench" },
      { 33300u, "Garden Anvil" },
      { 33301u, "Materiel Table" },
      { 33302u, "Fête Party Balloons" },
      { 33303u, "Fête Skyrise Balloon" },
      { 33304u, "Authentic Sultana's Seven" },
      { 33305u, "Authentic Water Bomb Stand" },
      { 33327u, "G-Warrior MG" },
      { 33451u, "Season Eighteen Lone Wolf Trophy" },
      { 33461u, "Season Nineteen Lone Wolf Trophy" },
      { 35554u, "Season Twenty Lone Wolf Trophy" },
      { 35555u, "Orchestrion Phonograph" },
      { 35556u, "Shoe Rack" },
      { 35557u, "Ivy Mural" },
      { 35558u, "Table Mat" },
      { 35559u, "Matching Cushions" },
      { 35560u, "Marimba" },
      { 35561u, "Indoor Oriental Waterfall" },
      { 35562u, "Round Wooden Loft" },
      { 35563u, "Heavenly Ornamental Array" },
      { 35564u, "Woodland Chair" },
      { 35565u, "Apothecary's Workbench" },
      { 35566u, "Candlelit Sundries" },
      { 35567u, "Fat Cat Rug" },
      { 35568u, "Walnut Flower Vases" },
      { 35569u, "Fluffy Pancakes" },
      { 35570u, "Stuffed Bom Boko" },
      { 35571u, "Wooden Blinds" },
      { 35572u, "Dance Pole" },
      { 35573u, "Antique Wall Shelf" },
      { 35574u, "Wooden Bin" },
      { 35575u, "Imitation Wooden Skylight" },
      { 35576u, "Square Niche Partition" },
      { 35577u, "Pendant Wall Light" },
      { 35578u, "Loft Bed" },
      { 35579u, "Restaurant Showcase" },
      { 35580u, "Ruby Sea Phasmascape" },
      { 35581u, "Aurora Phasmascape" },
      { 35582u, "Il Mheg Garden Fountain" },
      { 35583u, "Hanging Bench" },
      { 35584u, "Coldbare Tree" },
      { 35585u, "Poroggo Lawn Ornament" },
      { 35586u, "Miniature Antique Clock Tower" },
      { 35587u, "Shirogane Pinwheel" },
      { 35617u, "Highland Chocobo Stable" },
      { 35793u, "Trophy of Eternal Darkness" },
      { 35794u, "Trophy of Divine Light" },
      { 35796u, "New World Burrito Lunch" },
      { 35797u, "Hannish Silk Rack" },
      { 35798u, "Ancient Lamp" },
      { 35799u, "Labyrinthos Warning Sign" },
      { 35800u, "Etheirys Globe" },
      { 35801u, "Nekropolis Table" },
      { 35802u, "Pumpkin Flower Vase" },
      { 35803u, "Starlight Mobile" },
      { 35804u, "Thavnairian Striking Dummy" },
      { 35805u, "Garlean Containers" },
      { 35806u, "Sharlayan Lamppost" },
      { 35807u, "Thavnairian Dhow" },
      { 35808u, "Haunted Pumpkin Set" },
      { 36380u, "Deluxe Snow-dusted Tree" },
      { 36381u, "Deluxe Unmelting Ice Partition" },
      { 36382u, "Deluxe Unmelting Ice Loft" },
      { 36383u, "Deluxe Handmade Akabeko" },
      { 36384u, "Deluxe Stuffed Kupka Kupp" },
      { 36631u, "Valentione Mobile" },
      { 37312u, "Authentic White Tiger" },
      { 37347u, "Classic Dresser" },
      { 37348u, "Log Rack" },
      { 37349u, "Indoor Marble Fountain" },
      { 37351u, "Ornamental Crystal Array" },
      { 37352u, "Log Pillar" },
      { 37353u, "Kweh-kweh Clock" },
      { 37354u, "Small Blackboard" },
      { 37355u, "Eastern Canopy Bed" },
      { 37356u, "Wondrous Parfait" },
      { 37357u, "Eulmoran Divan" },
      { 37358u, "Sweetfish Skewers" },
      { 37359u, "Ivy Curtain" },
      { 37360u, "Luminous Wooden Loft" },
      { 37361u, "Swinging-door Counter" },
      { 37362u, "Ironworks Tool Set" },
      { 37363u, "Trick Bookshelf Partition" },
      { 37364u, "Simple Arched Window" },
      { 37365u, "Factory Pillar" },
      { 37366u, "Curved Marble Partition" },
      { 37367u, "Iron Chair" },
      { 37368u, "Indoor Climbing Frame" },
      { 37369u, "Steel Locker" },
      { 37370u, "Alpine Cabinet" },
      { 37371u, "Alpine Wall Lantern" },
      { 37372u, "Astrologicum Star Map" },
      { 37373u, "Imitation Highland Window" },
      { 37374u, "House Fortemps Fireplace" },
      { 37375u, "Alpine Doormat" },
      { 37376u, "Cathedral Lectern" },
      { 37377u, "Scholasticate Table" },
      { 37378u, "Scholasticate Chair" },
      { 37379u, "Scholasticate Bookshelf" },
      { 37380u, "Bluefeather Astrolabos" },
      { 37381u, "Royal Menagerie Phasmascape" },
      { 37382u, "Sea of Clouds Phasmascape" },
      { 37383u, "Stuffed Sanduruva" },
      { 37384u, "Stuffed Minduruva" },
      { 37385u, "Stuffed Cinduruva" },
      { 37386u, "Moogle Balloon" },
      { 37387u, "Eggsemplary Basket" },
      { 37388u, "Hatching-tide Mobile" },
      { 37389u, "Emperor Portrait" },
      { 37390u, "Heavensturn Crane Banner" },
      { 37391u, "Azure Banner of Conflict" },
      { 37392u, "Crimson Banner of Conflict" },
      { 37393u, "Authentic Hatching-tide Confections" },
      { 37394u, "Authentic Archon Egg Pouch" },
      { 37395u, "Authentic Costa del Hielo" },
      { 37396u, "Authentic Grilled Corn" },
      { 37397u, "Alpine Open-air Bath" },
      { 37398u, "Ivy-covered Bridge" },
      { 37399u, "Snowy Red Brick Bench" },
      { 37400u, "Raised Wooden Deck" },
      { 37401u, "Garden Gate" },
      { 37402u, "Planter Set" },
      { 37403u, "Sweet Lawn Ornaments" },
      { 37404u, "Damaged Ishgardian Wall" },
      { 37405u, "Ishgardian Lamppost" },
      { 37406u, "Summer Bonfire" },
      { 37407u, "Authentic Moonfire Mask Stall" },
      { 38576u, "Imitation Stained Crystal Roundel" },
      { 38577u, "Hanging Planter Branch" },
      { 38578u, "Simple Basket" },
      { 38579u, "Gaol Partition Door" },
      { 38580u, "Wall-climbing Ivy" },
      { 38581u, "Gaol Partition" },
      { 38582u, "Hydaelyn, as We Know Her" },
      { 38583u, "Wondrous Windoor" },
      { 38584u, "Mummer's Shelf" },
      { 38585u, "Cheese Fondue Set" },
      { 38586u, "Enigma Partition" },
      { 38587u, "Wall-mounted Wings" },
      { 38588u, "Dragonstar aux Marrons" },
      { 38589u, "Coconut Water" },
      { 38590u, "Oriental New Year's Meal" },
      { 38591u, "Side-tied Curtain" },
      { 38592u, "Tatami Loft" },
      { 38593u, "Kugane Phasmascape" },
      { 38594u, "Costa del Sol Phasmascape" },
      { 38595u, "Sharlayan Sink" },
      { 38596u, "Sharlayan Chair" },
      { 38597u, "Sharlayan Desk" },
      { 38598u, "Sharlayan Rug" },
      { 38599u, "Garlean Stove" },
      { 38600u, "Sil'dihn Chair" },
      { 38601u, "Sil'dihn Kitchen Shelf" },
      { 38602u, "Sil'dihn Side Table" },
      { 38603u, "Sil'dihn Banner" },
      { 38604u, "Caged Wisp" },
      { 38605u, "Korpokkur Chronometer" },
      { 38606u, "Natural Wooden Beam" },
      { 38607u, "Flagstone Steps" },
      { 38608u, "Flagstone Loft" },
      { 38609u, "Exciting Neon Wall Light" },
      { 38610u, "Farmer's Straw Bed" },
      { 38611u, "Valentione's Heart Desk" },
      { 38612u, "Storm's Crown Sword Stand" },
      { 38613u, "Bill of Contrition (S-1)" },
      { 38614u, "Bill of Deep Contrition (M-1)" },
      { 38615u, "Bill of Deepest Contrition (L-1)" },
      { 38616u, "Rousing Chronometer" },
      { 38617u, "Countertop Coffee Service" },
      { 38618u, "Camping Tent" },
      { 38619u, "Damaged Highland Turret" },
      { 38620u, "Shoveled Snow" },
      { 38621u, "Blooming Garden Table" },
      { 38622u, "Bamboo Fence" },
      { 38623u, "Camping Furniture" },
      { 38624u, "Illuminated Tree" },
      { 38625u, "Far Eastern Well" },
      { 38626u, "Far Eastern Lamppost" },
      { 38627u, "Elpis Stone Pillar" },
      { 38628u, "Chocobo Stable Essentials" },
      { 38669u, "Authentic Pumpkin Flower Vase" },
      { 38670u, "Authentic Starlight Mobile" },
      { 38671u, "Authentic Haunted Pumpkin Set" },
      { 38676u, "Beyond Gloam's Veil" },
      { 38677u, "Love and Loss" },
      { 38678u, "Wherewith We Learn" },
      { 38679u, "Business's Pleasure" },
      { 38680u, "Scion of Wisdom" },
      { 38681u, "Scion of Valor" },
      { 38682u, "Discernment" },
      { 38683u, "Adventure Calls" },
      { 38697u, "Bill of Contrition (S-2)" },
      { 38698u, "Bill of Contrition (S-3)" },
      { 38699u, "Bill of Contrition (S-4)" },
      { 38700u, "Bill of Contrition (S-5)" },
      { 38701u, "Bill of Deep Contrition (M-2)" },
      { 38702u, "Bill of Deep Contrition (M-3)" },
      { 38703u, "Bill of Deep Contrition (M-4)" },
      { 38704u, "Bill of Deep Contrition (M-5)" },
      { 38705u, "Bill of Deepest Contrition (L-2)" },
      { 38706u, "Bill of Deepest Contrition (L-3)" },
      { 38707u, "Bill of Deepest Contrition (L-4)" },
      { 38708u, "Bill of Deepest Contrition (L-5)" },
      { 39378u, "Simple Low Table" },
      { 39379u, "Flower Boucage" },
      { 39380u, "Faerie Door" },
      { 39381u, "Rattan Sofa" },
      { 39382u, "Charcoal Iron" },
      { 39383u, "Grounded Cactuar" },
      { 39384u, "Far Eastern Stained Crystal Roundel" },
      { 39385u, "Timeworn Thaumaturgic Array" },
      { 39386u, "Darkest Hourglass" },
      { 39387u, "Classical Water Jug" },
      { 39388u, "Allagan Terminal Alpha" },
      { 39389u, "Allagan Terminal Beta" },
      { 39390u, "Toy Cooking Set" },
      { 39391u, "Lalafell Lifter" },
      { 39392u, "Highland Partition" },
      { 39393u, "Highland Partition Door" },
      { 39394u, "Alpine Pillar" },
      { 39395u, "Enigma Wall Chronometer" },
      { 39396u, "Rainbow Hopping Rug" },
      { 39397u, "Loporrit Material Supplier Permit" },
      { 39398u, "Loporrit Junkmonger Permit" },
      { 39399u, "Loporrit Mender Permit" },
      { 39400u, "Loporrit Servingway Permit" },
      { 39401u, "Hannish Pendant Wall Light" },
      { 39402u, "Hannish Screen" },
      { 39403u, "Hannish Bed" },
      { 39404u, "Hannish Chair" },
      { 39405u, "Hannish Round Table" },
      { 39406u, "Hannish Table Lamp" },
      { 39407u, "Hannish Bookshelf Partition" },
      { 39408u, "Hannish Rug" },
      { 39409u, "Waterfall Partition" },
      { 39410u, "Infernal Archfiend Cloak" },
      { 39411u, "Faerie Round Table" },
      { 39412u, "Pudding Pudding" },
      { 39413u, "Exciting Balloon" },
      { 39414u, "Simple Sink" },
      { 39415u, "Crystal Tower Phasmascape" },
      { 39416u, "Urth's Fount Phasmascape" },
      { 39417u, "Hatching-tide MMXXII Advertisement" },
      { 39418u, "Little Ladies' Day MMXXI Advertisement" },
      { 39419u, "Low Garden Hedge" },
      { 39420u, "Orthos Display Stand" },
      { 39421u, "Smaller Water Otter Fountain" },
      { 39422u, "Hannish Lamppost" },
      { 39423u, "Simple Fountain" },
      { 39424u, "Jinrikisha" },
      { 39425u, "Lemonade Stand" },
      { 39426u, "Dorpokkur Kiln" },
      { 39427u, "Stone Garden Wall" },
      { 39428u, "Colorful Flower Patch" },
      { 39432u, "Stuffed Loporrit" },
      { 39433u, "Authentic Valentione Mobile" },
      { 39434u, "Authentic Eggsemplary Basket" },
      { 39435u, "Authentic Hatching-tide Mobile" },
      { 40623u, "Botanist's Work Table" },
      { 40624u, "Imitation Curtained Window" },
      { 40625u, "Assorted Cookies" },
      { 40626u, "Botanical Candle Display" },
      { 40627u, "Cieldalaes Planter" },
      { 40628u, "Chair Hena" },
      { 40629u, "Classic Table Lamp" },
      { 40630u, "Imitation Moonlit Window" },
      { 40631u, "Metal Pipe Wall Shelf" },
      { 40632u, "Steamed Shumai" },
      { 40633u, "Fried Gyoza" },
      { 40634u, "Floating Kitchen Cabinet" },
      { 40635u, "Azure Sky Phasmascape" },
      { 40636u, "Starry Sky Phasmascape" },
      { 40637u, "Freestanding Rattan Hammock" },
      { 40638u, "Classic Tableware" },
      { 40639u, "Classic Umbrella Stand" },
      { 40640u, "Rustic Log Table" },
      { 40641u, "Rustic Log Bench" },
      { 40642u, "Scattered Tomes" },
      { 40643u, "Vintage Side Table" },
      { 40644u, "Short Pillar Bookshelf" },
      { 40645u, "Imitation Vault" },
      { 40646u, "Ivy Pendant Wall Light" },
      { 40647u, "Crystarium Pendant Wall Light" },
      { 40648u, "Fishing Rod Rack" },
      { 40649u, "Far Eastern Indoor Bamboo Fountain" },
      { 40650u, "Cactuar Colony Planter" },
      { 40651u, "Verdant Pillar" },
      { 40652u, "Rattan Partition" },
      { 40653u, "Oyoroi Display" },
      { 40654u, "Far Eastern Brazier" },
      { 40655u, "Imitation Closed Door" },
      { 40656u, "Wall-mounted Pot Rack" },
      { 40657u, "Serving Barrel" },
      { 40658u, "Classic Secretaire" },
      { 40659u, "Raw Log Half Partition" },
      { 40660u, "Forever at Your Side" },
      { 40661u, "Blighted Earth Sword Stand" },
      { 40662u, "Drowned King Sculpture" },
      { 40663u, "Eventide Sword Stand" },
      { 40664u, "The Rising MMXXIII Advertisement" },
      { 40665u, "Wall-mounted Vampire Bats" },
      { 40666u, "Springtide Fan Stand" },
      { 40667u, "Valentione's Heart Chair" },
      { 40671u, "Island Garden Fence" },
      { 40672u, "Island Parasol Table" },
      { 40673u, "Island Wooden Chair" },
      { 40674u, "Railway Sleepers" },
      { 40675u, "Garden Creek" },
      { 40676u, "Mushroom Medley Lamp" },
      { 40677u, "Illuminated Topiary Chocobo" },
      { 40678u, "Country Swing Seat" },
      { 40679u, "Ornate Fountain" },
      { 40680u, "Smashed Flower Pot" },
      { 40681u, "Well Pump" },
      { 40682u, "Ivy-canopied Deck" },
      { 40683u, "Komainu Statue" },
      { 40684u, "Stone Toro Lantern" },
      { 40685u, "Planted Toro Lantern" },
      { 40757u, "Seiryu Sculpture" },
      { 40758u, "Suzaku Sculpture" },
      { 40759u, "Authentic Copy of Newfound Adventure" },
      { 40760u, "Authentic Copy of Buried Memory" },
      { 40761u, "Authentic Copy of Gods Revel, Lands Tremble" },
      { 40762u, "Authentic Copy of the Dark Throne" },
      { 40763u, "Loss, Fire, Faith Reproduction" },
      { 40764u, "Authentic Summer Bonfire" },
      { 41085u, "Hanging Lure Display" },
      { 41086u, "Abyssal Wings Display" },
      { 41087u, "Crystarium Wall Canopy" },
      { 41088u, "Nursery Canopy" },
      { 41089u, "Ronkan Fireplace" },
      { 41090u, "Tomestool" },
      { 41091u, "Magazine Display Rack" },
      { 41092u, "Celestial Screen" },
      { 41094u, "Imitation Trapdoor" },
      { 41095u, "Manderville's Adventurer Flakes" },
      { 41096u, "Cluster Lamp" },
      { 41097u, "Lapis Manalis Banner" },
      { 41099u, "Hyperboreia Bench" },
      { 41100u, "Aitiascope Desk" },
      { 41101u, "Aitiascope Lamp" },
      { 41102u, "Throne of the Foremost" },
      { 41103u, "Wood Slice Loft" },
      { 41104u, "Faerie Pendant Wall Light" },
      { 41105u, "Portaled Partition" },
      { 41106u, "Korpokkur Moss Ball" },
      { 41107u, "Sealing Wax & Letter Set" },
      { 41108u, "Bubbly Mogfloat" },
      { 41109u, "Royal Leather Sofa" },
      { 41110u, "Wall Cabinet" },
      { 41111u, "Field of Hope Rug" },
      { 41112u, "Sake Barrel" },
      { 41113u, "Colossal Chronometer Window" },
      { 41114u, "Hyper Hustle Trophy" },
      { 41115u, "Authentic Copy of A Realm Awoken" },
      { 41116u, "Authentic Copy of Through the Maelstrom" },
      { 41117u, "Authentic Copy of Defenders of Eorzea" },
      { 41118u, "Authentic Copy of Dreams of Ice" },
      { 41119u, "Authentic Copy of Before the Fall" },
      { 41120u, "Authentic Copy of As Goes Light, So Goes Darkness" },
      { 41121u, "Authentic Copy of The Gears of Change" },
      { 41122u, "Authentic Copy of Revenge of the Horde" },
      { 41123u, "Authentic Copy of Soul Surrender" },
      { 41124u, "Authentic Copy of The Far Edge of Fate" },
      { 41125u, "Authentic Copy of Vows of Virtue, Deeds of Cruelty" },
      { 41126u, "Authentic Copy of Echoes of a Fallen Star" },
      { 41127u, "Authentic Copy of Reflections in Crystal" },
      { 41128u, "Authentic Copy of Futures Rewritten" },
      { 41129u, "Authentic Copy of Death Unto Dawn" },
      { 41130u, "Authentic Copy of Growing Light" },
      { 41131u, "The Builder" },
      { 41132u, "The Destroyer" },
      { 41133u, "The Warden" },
      { 41134u, "The Traders" },
      { 41135u, "The Matron" },
      { 41136u, "The Keeper" },
      { 41137u, "Il Mheg Arbor" },
      { 41138u, "Mobile Garden Stairs" },
      { 41139u, "Flower-wreathed Gazebo" },
      { 41140u, "Alzadaal's Garden Lamp" },
      { 41141u, "Elpis Garden Fountain" },
      { 41142u, "Wildflower Bed" },
      { 41143u, "Bluecap Mushroom Lamp" },
      { 41144u, "Imitation Magitek Repair Station (Wall Unit)" },
      { 41145u, "Labyrinthos Grape Lamppost" },
      { 41146u, "Hingan Rock Garden" },
      { 41147u, "Sharlayan Reflecting Pool" },
      { 41149u, "Authentic Caged Wisp" },
      { 41150u, "Authentic Illuminated Tree" },
      { 41151u, "Authentic Heavensturn Crane Banner" },
      { 41152u, "Authentic Valentione's Heart Desk" },
      { 41153u, "Authentic Little Ladies' Day MMXXI Advertisement" },
      { 41154u, "Authentic Hatching-tide MMXXII Advertisement" },
      { 41425u, "Goobbue Table Chronometer" },
      { 41426u, "Cactuar Dining Table" },
      { 41427u, "Minute Mindflayer Sprinkler" },
      { 41428u, "Senor Sabotender Chronometer" },
      { 41429u, "Imitation Magitek Repair Station (Floor Unit)" },
      { 41430u, "Aloalo Pendant Wall Lamp" },
      { 41431u, "Forgotten Figure" },
      { 41432u, "Aloalo Faerie Cushion" },
      { 41433u, "South Seas Coelacanth Display" },
      { 41434u, "Sagely Sparrow Statue" },
      { 41435u, "Aloalo Island Palm" },
      { 41436u, "Aloalo Roselle Hedge" },
      { 41437u, "Blunderville Crown" },
      { 41438u, "Blunderville Flag" },
      { 41439u, "Blunderville Donut Bumper" },
      { 41440u, "Blunderville Padded Wall" },
      { 41441u, "Blunderville Rainbow" },
      { 41442u, "Blunderville Hammer" },
      { 41651u, "Alzadaal's Treasure Rug" },
      { 41813u, "Wine Coupe Tower" },
      { 41814u, "Skyruin Trophy" },
      { 41815u, "Resilient Sword Stand" },
      { 41816u, "Kitchen Cabinet" },
      { 41817u, "Slovenly Chair" },
      { 41818u, "Stuffed Axolotl Eft" },
      { 41819u, "Stuffed Flag" },
      { 41820u, "Fluffy-wuffy Stuffed Drippy" },
      { 41821u, "Stuffed Punutiy" },
      { 41822u, "Decorative Hingan Door" },
      { 41823u, "Sunset Phasmascape" },
      { 41824u, "Goblet Phasmascape" },
      { 41825u, "Lavender Beds Phasmascape" },
      { 41826u, "Mist Phasmascape" },
      { 41827u, "Shirogane Phasmascape" },
      { 41828u, "Empyreum Phasmascape" },
      { 41829u, "Twintop Table" },
      { 41830u, "Mug of Cocoa" },
      { 41831u, "Everkeep Striking Dummy" },
      { 41832u, "Stargazing Deck" },
      { 41833u, "Wooden Garden Gate" },
      { 43558u, "The Spinner" },
      { 43559u, "The Fury" },
      { 43560u, "The Lover" },
      { 43561u, "The Scholar" },
      { 43562u, "The Navigator" },
      { 43563u, "The Wanderer" },
      { 43941u, "Legacies Reproduction" },
      { 44111u, "Stone Partition" },
      { 44112u, "Xibruq Pibil" },
      { 44113u, "Tuliyollan Table Lamp" },
      { 44114u, "Pelupelu Partition" },
      { 44115u, "Ihuykatumu Bed" },
      { 44116u, "Vending Machine Nine" },
      { 44117u, "Assortment of Strewn Bottles" },
      { 44118u, "Heritage Found Crates" },
      { 44119u, "Horizontal Neon Wall Light" },
      { 44120u, "Vertical Neon Wall Light" },
      { 44121u, "Yak T'el Scale Tree" },
      { 44122u, "Neo Kingdom Lamppost" },
      { 44269u, "Pile of Gold" },
      { 44270u, "Rising Table Lamp" },
      { 44271u, "Flying Pumpkin" },
      { 44272u, "Gilded Screen" },
      { 44273u, "Bouncing Bomb Balloons" },
      { 44332u, "Authentic Rising MMXXIII Advertisement" },
      { 44333u, "Authentic Wall-mounted Vampire Bats" },
      { 44873u, "Wood-fired Range" },
      { 44874u, "Holodisplay" },
      { 44875u, "Storybook Bench" },
      { 44876u, "Sweet Dreamer's Stall" },
      { 44877u, "Frilly Curtains" },
      { 44878u, "Botanist's Wall Frames" },
      { 44879u, "Botanical Loft" },
      { 44880u, "Patchwork Sofa" },
      { 44881u, "Gleaner's Knapsack" },
      { 44882u, "Popoto Chips" },
      { 44883u, "Investigation Map" },
      { 44884u, "Apothecary Mortar" },
      { 44885u, "Grassy Rug" },
      { 44886u, "Royal Bed" },
      { 44887u, "Eternal Cenotaph" },
      { 44888u, "Skydeep Lamp" },
      { 44889u, "Origenics Monitor Desk" },
      { 44890u, "Origenics Chair" },
      { 44891u, "Strayborough Wall Lamp" },
      { 44892u, "Traumerei Floor Stencil" },
      { 44895u, "Climbing Wall Partition" },
      { 44896u, "Billiard Table" },
      { 44897u, "Imitation Closet" },
      { 44898u, "Fighting Ring" },
      { 44899u, "Dartboard Cabinet" },
      { 44900u, "Laboratory Counter" },
      { 44901u, "Kugane-yaki" },
      { 44902u, "Classroom Desk" },
      { 44903u, "Classroom Chair" },
      { 44904u, "Classroom Blackboard" },
      { 44905u, "Contemporary Iron Shelf" },
      { 44906u, "Starlight Celebration MMXXIII Advertisement" },
      { 44907u, "Valentione's Day MMXXIV Advertisement" },
      { 44908u, "Authentic Copy of Crossroads" },
      { 44909u, "Starlight Kinderpunsch" },
      { 44910u, "Starlight Mug Tower" },
      { 44911u, "Weathered Porthole" },
      { 44912u, "Rail-and-stile Partition" },
      { 44913u, "Tea Caddy Cabinet" },
      { 44914u, "Indoor Awning" },
      { 44915u, "Dragoon Display Crystal" },
      { 44916u, "Paladin Display Crystal" },
      { 44917u, "Warrior Display Crystal" },
      { 44918u, "Monk Display Crystal" },
      { 44919u, "Ninja Display Crystal" },
      { 44920u, "Bard Display Crystal" },
      { 44921u, "Black Mage Display Crystal" },
      { 44922u, "Summoner Display Crystal" },
      { 44923u, "White Mage Display Crystal" },
      { 44924u, "Scholar Display Crystal" },
      { 44925u, "Authentic Valentione's Heart Chair" },
      { 44926u, "Cactus Patch" },
      { 44927u, "Garden Palm Tree" },
      { 44928u, "Far Eastern Gazebo" },
      { 44929u, "Square Stepping Stones" },
      { 44930u, "Electrope Turret" },
      { 44931u, "Lesser Pillar of Tural" },
      { 44932u, "Ihuykatumu Flower Plot" },
      { 44933u, "Zormor Stone Lantern" },
      { 44934u, "Ja Ja Gural Tree" },
      { 44935u, "Authentic Illuminated Topiary Chocobo" },
      { 44970u, "Cushioned Bench" },
      { 44971u, "Tropical Canopy Bed" },
      { 44972u, "Wachumeqimeqi Balloon" },
      { 46090u, "Log Partition Door" },
      { 46091u, "Tuliyollan Beverage Rack" },
      { 46092u, "Straight Tuliyollan Counter" },
      { 46093u, "Curved Tuliyollan Counter" },
      { 46094u, "Tuliyollan Bench" },
      { 46095u, "Tuliyollan Screen" },
      { 46096u, "Tuliyollan Table" },
      { 46097u, "Tuliyollan Stool" },
      { 46098u, "Tuliyollan Censer" },
      { 46099u, "Tuliyollan Rug" },
      { 46100u, "Cabin Half-moon Rug" },
      { 46101u, "Cabin Bed" },
      { 46102u, "Cabin Sofa" },
      { 46103u, "Cabin Sideboard" },
      { 46104u, "Overhead Studio Light" },
      { 46105u, "Dumbbell Rack" },
      { 46106u, "Training Bench" },
      { 46107u, "Training Mat" },
      { 46108u, "Old Fishwife's Paella" },
      { 46109u, "Lattice Planter Partition" },
      { 46110u, "Scroll Cabinet" },
      { 46111u, "Minimalist Glade Partition Door" },
      { 46112u, "Minimalist Oasis Partition Door" },
      { 46113u, "Minimalist Riviera Partition Door" },
      { 46114u, "Minimalist Highland Partition Door" },
      { 46115u, "Minimalist Hingan Partition Door" },
      { 46116u, "Death's Halo" },
      { 46117u, "Authentic Copy of The Promise of Tomorrow" },
      { 46118u, "Far Eastern Odder Otter Seater" },
      { 46119u, "All Saints' Wake MMXXIV Advertisement" },
      { 46120u, "La Noscean Orange Tree" },
      { 46121u, "Imitation Battlefield" },
      { 46122u, "Frosted Topiary" },
      { 46123u, "Frozen Garden Pond" },
      { 46124u, "Guardian Arkveld's Windward Plains Phasmascape" },
      { 46125u, "Rey Dau's Windward Plains Phasmascape" },
      { 46126u, "Uth Duna's Scarlet Forest Phasmascape" },
      { 46127u, "Omega's Iceshard Cliffs Phasmascape" },
      { 46128u, "Jin Dahaad's Iceshard Cliffs Phasmascape" },
      { 46129u, "Nu Udra's Oilwell Basin Phasmascape" },
      { 46130u, "Lominsan Kinderpunsch" },
      { 46131u, "Lominsan Starlight Mugs" },
      { 46132u, "Ul'dahn Kinderpunsch" },
      { 46133u, "Ul'dahn Starlight Mugs" },
      { 46134u, "Yok Huy Lantern" },
      { 46161u, "Outdoor Hunter's Grill" },
      { 46162u, "Wilds Great Sword Stand" },
      { 46163u, "Wilds Banner" },
      { 46165u, "Makeshift Camp" },
      { 46166u, "Pop-up Camp" },
      { 46167u, "Palanx Striking Dummy" },
      { 46168u, "Lyhe Ghiah Stained Crystal Ornament" },
      { 46169u, "Traverse Clocktower" },
      { 46170u, "Traverse Tree" },
      { 46171u, "Everkeep Lamppost" },
      { 46172u, "Legends of Ivalice" },
      { 46173u, "Cosmoseat" },
      { 46174u, "Cosmokitchen Partition" },
      { 46175u, "Portable Exoterminal" },
      { 46176u, "Docking Station Cover" },
      { 46177u, "Cosmic Circumferbench" },
      { 46799u, "Felyne Material Supplier Permit" },
      { 46800u, "Felyne Junkmonger Permit" },
      { 46801u, "Felyne Mender Permit" },
      { 46802u, "Felyne Servant Permit" },
      { 47226u, "Authentic Bouncing Bomb Balloons" },
      { 47227u, "Authentic Rising Table Lamp" },
      { 47228u, "Authentic Flying Pumpkin" },
      { 47348u, "Valentione Round Table" },
      { 47349u, "Rolanberry Valentione Cake" },
      { 47350u, "Authentic Valentione's Day MMXXIV Advertisement" },
      { 47352u, "Weeping Cherry Tree" },
      { 47476u, "Hunter's Camp Chair" },
      { 47477u, "Indoor Hunter's Grill" },
      { 47478u, "Hunter's Lodge Chair" },
      { 48006u, "Authentic Cactuar Dining Table" },
      { 48007u, "Authentic Senor Sabotender Chronometer" },
      { 48100u, "East End Tree" },
      { 48101u, "Gardener's Gate" },
      { 48102u, "Hewn Stone Circle" },
      { 48103u, "Hedge-trimmed Flower Bed" },
      { 48104u, "Chocobo Chick Fountain" },
      { 48105u, "Occult Epitaph" },
      { 48106u, "No-entry Door Partition" },
      { 48107u, "Hingan Dresser" },
      { 48108u, "Hingan Event Partition" },
      { 48109u, "Decorative Hingan Window" },
      { 48110u, "Simple Hingan Pillar" },
      { 48111u, "Hingan Wall Shelf" },
      { 48112u, "Hingan Wall Cabinet" },
      { 48113u, "Hingan Floor Cabinet" },
      { 48114u, "Wooden Frame Partition" },
      { 48115u, "Vault Rib" },
      { 48116u, "Lattice-windowed Cabinet" },
      { 48117u, "Dark Knight Display Crystal" },
      { 48118u, "Machinist Display Crystal" },
      { 48119u, "Astrologian Display Crystal" },
      { 48120u, "Samurai Display Crystal" },
      { 48121u, "Red Mage Display Crystal" },
      { 48122u, "Blue Mage Display Crystal" },
      { 48123u, "Gunbreaker Display Crystal" },
      { 48124u, "Dancer Display Crystal" },
      { 48125u, "Reaper Display Crystal" },
      { 48126u, "Sage Display Crystal" },
      { 48127u, "Viper Display Crystal" },
      { 48128u, "Pictomancer Display Crystal" },
      { 48129u, "Mantelshelf" },
      { 48130u, "Leg of Prosciutto" },
      { 48131u, "Round Stepping Star" },
      { 48132u, "Recollection Sword Stand" },
      { 48133u, "Authentic Copy of Seekers of Eternity" },
      { 48134u, "Blank Minimalist Partition" },
      { 48135u, "Minimalist Partition Door" },
      { 48136u, "Drafting Table" },
      { 48137u, "Tiika Selvaas" },
      { 48138u, "Celestial Sphere" },
      { 48139u, "Crescent Trophy" },
      { 48140u, "Occult Table" },
      { 48141u, "Occult Chair" },
      { 48142u, "Occult Tablets" },
      { 48143u, "Knowledge Crystal Replica" },
      { 48144u, "Occult Crescent Map" },
      { 48732u, "Cosmochair" },
      { 48733u, "Cosmotable" },
      { 48734u, "Cosmolamp" },
      { 48735u, "Stellar Lamppost" },
      { 49838u, "Valentione Chair" },
      { 49839u, "Valentione Rug" },
      { 49840u, "Scattered Archon Eggs" },
      { 49841u, "Filled Archon Egg Basket" },
      { 49842u, "Archon Egg Basket" },
      { 49843u, "Doom-engine Levinbox" },
      { 49845u, "Housing Design Award MMXXV Plaque " },
      { 49846u, "Everkeep Mixing Booth" },
      { 49847u, "Unfriendly Yan Neon Wall Light" },
      { 49848u, "Cosmic Storage Bench" },
      { 49849u, "Imitation Cosmoportal" },
      { 49850u, "Cosmomodule Floor Panel" },
      { 49851u, "Steaming Stew Bowl" },
      { 49852u, "Sampler Board" },
      { 49853u, "Peeling Wallpaper Sticker" },
      { 49854u, "Fortune-teller's Table" },
      { 49855u, "Adventurer's Effects" },
      { 49856u, "Lakeland Planter" },
      { 49857u, "Eulmore Planter" },
      { 49858u, "Il Mheg Planter" },
      { 49859u, "Minishroom Lamp" },
      { 49860u, "Authentic Copy of Into the Mist" },
      { 49861u, "Valentione's Day MMXXV Advertisement" },
      { 49862u, "Little Ladies' Day MMXXV Advertisement" },
      { 49863u, "Tabletop Little Ladies' Day MMXXV Advertisement" },
      { 49864u, "Little Ladies' Day MMXVIII Advertisement" },
      { 49865u, "Hatching-tide MMXXV Advertisement" },
      { 49866u, "Tabletop Hatching-tide MMXXV Advertisement" },
      { 49867u, "Hatching-tide MMXVI Advertisement" },
      { 49868u, "Authentic Starlight Celebration MMXXIII Advertisement" },
      { 49869u, "Authentic All Saints' Wake MMXXIV Advertisement" },
      { 49870u, "Cosmic Safety Lamp" },
      { 49871u, "Cosmic Antenna" },
      { 49872u, "Shady Cosmoshelter" },
      { 49873u, "Lawn Mograft" },
      { 49874u, "Lawn Chocobo Raft" },
      { 49875u, "Lawn Bomb Raft" },
      { 50083u, "Moonfire Faire MMXXV Advertisement" },
      { 50084u, "Tabletop Moonfire Faire MMXXV Advertisement" },
      { 50085u, "Moonfire Faire MMXVII Advertisement" },
      { 50086u, "Make It Rain Campaign MMXXV Advertisement" },
      { 50087u, "Tabletop Make It Rain Campaign MMXXV Advertisement" },
      { 50088u, "Make It Rain Campaign MMXXIV Advertisement" },
      { 50353u, "Authentic Starlight Kinderpunsch" },
      { 50354u, "Authentic Starlight Mug Tower" },
      { 50355u, "Authentic Lominsan Kinderpunsch" },
      { 50356u, "Authentic Lominsan Mug Tower" },
      { 50357u, "Authentic Ul'dahn Kinderpunsch" },
      { 50358u, "Authentic Ul'dahn Mug Tower" },
      { 50359u, "Authentic Oriental New Year's Meal" },
      { 50360u, "Authentic Springtide Fan Stand" },
      { 50416u, "Corvosi Sofa" },
      { 50417u, "Purple Corvosi Carpet" },
      { 50418u, "Green Corvosi Carpet" },
      { 50419u, "Corvosi Screen" },
      { 50420u, "Imitation Corvosi Shisha" },
      { 50421u, "Corvosi Fan" },
      { 50422u, "Corvosi Crystal" },
      { 50423u, "Occult Compass" },
      { 50424u, "Occult Pyramicula" },
      { 50425u, "Mhachi Lamppost" },
      { 51251u, "Lattice Window Seat" },
      { 51252u, "Rod of Nihility Display" },
      { 51253u, "Minimalist Poplar Partition" },
      { 51254u, "Minimalist Poplar Partition Door" },
      { 51255u, "Everkeep Platform Bed" },
      { 51256u, "Everkeep Sofa" },
      { 51257u, "Curved Everkeep Sofa" },
      { 51258u, "Everkeep Dining Table" },
      { 51259u, "Everkeep Round Table" },
      { 51260u, "Everkeep Sideboard" },
      { 51261u, "Everkeep Chest" },
      { 51262u, "Everkeep Monitor" },
      { 51263u, "Everkeep Stereo" },
      { 51264u, "Everkeep Wall Decoration" },
      { 51265u, "Everkeep Rug" },
      { 51266u, "Everkeep Kitchen Counter" },
      { 51267u, "Everkeep Partition" },
      { 51269u, "Garden Mood Lighting" },
      { 51270u, "Garden Canal" },
      { 51271u, "Semicircular Outdoor Stage" },
      { 51272u, "Crumbling Aqueduct" },
      { 51273u, "Beastmaster Display Crystal" },
      { 51275u, "Galactic Way Miniature" },
      { 51276u, "Auxesian Tower Replica" },
      { 51277u, "Cosmic Metal Shelf" },
      { 51278u, "Cosmic Steps" },
      { 51279u, "Cosmic Partition" },
      { 51280u, "Auxesian Waygate" },
      { 51281u, "Auxesian Waylight" },
      { 51282u, "Winged Scalekin Fossil" },
      { 51283u, "Crescent Stone Pillar" },
      { 51284u, "Crescent Wall Rack" },
      { 51285u, "Crescent Display Case" },
      { 51286u, "Blue Crescent Moon" },
      { 51287u, "Yellow Crescent Cube" },
      { 51288u, "Red Crescent Crystal" },
      { 51289u, "Wibbly-wobbly Jibanyan" },
      { 51290u, "Wibbly-wobbly Komasan" },
      { 51291u, "Wibbly-wobbly USApyon" },
      { 51292u, "Smiling Wakeshroom" },
      { 51293u, "Staring Wakeshroom" },
      { 51294u, "Wakesoul Buggy" },
      { 51295u, "Starlight Sled" },
      { 51298u, "Authentic Copy of Trail to the Heavens" },
      { 51299u, "The Rising MMXXV Advertisement" },
      { 51300u, "Tabletop Rising MMXXV Advertisement" },
      { 51301u, "The Rising MMXXIV Advertisement" },
      { 51302u, "All Saints' Wake MMXXV Advertisement" },
      { 51303u, "Tabletop All Saints' Wake MMXXV Advertisement" },
      { 51304u, "All Saints' Wake MMXVI Advertisement" },
      { 51305u, "Starlight Celebration MMXXV Advertisement" },
      { 51306u, "Tabletop Starlight Celebration MMXXV Advertisement" },
      { 51307u, "Starlight Celebration MMXXII Advertisement" },
      { 51314u, "Authentic Gilded Screen" },
      { 52298u, "Occult Apocrytaph" },
    };

    /// <summary>
    /// Anisotropic scatter blocks (F=(1000, 1, 20), tail floats (150, 0.5, 1.0)) —
    /// only Cherry Petal Pile and Hingan Rock Garden carry these. NOT used for the
    /// snap warning; kept for future scatter-placement research.
    /// </summary>
    public static readonly IReadOnlyDictionary<uint, (float X, float Y, float Z)> ItemScatterRanges = new Dictionary<uint, (float X, float Y, float Z)>
    {
      { 17029u, (1000f, 1f, 20f) }, // Eastern Cherry Petal Pile
      { 21873u, (1000f, 1f, 20f) }, // Authentic Eastern Cherry Petal Pile
      { 41146u, (1000f, 1f, 20f) }, // Hingan Rock Garden
    };

    /// <summary>True if the item carries a normal (uniform sphere) snap range.</summary>
    public static bool TryGetSnapRange(uint itemId, out float range) => ItemSnapRanges.TryGetValue(itemId, out range);

    /// <summary>True if the wall-mounted item has a TargetMarker-derived wall snap threshold.</summary>
    public static bool TryGetWallThreshold(uint itemId, out float threshold) => WallItemThresholds.TryGetValue(itemId, out threshold);

    /// <summary>True if the item's collision carries WallCollisionMesh (d3==1).</summary>
    public static bool IsWallReceiver(uint itemId) => WallReceiverItemIds.Contains(itemId);

    /// <summary>
    /// Multi-sided wall receivers — collision carries wall prims on more than one
    /// axis (White Rectangular Partition, Windowed Partition, Marble Alcove Bed).
    /// They accept wall items from every face, so the one-sided front/back filter
    /// must not apply; only the item's back-ray direction decides.
    /// </summary>
    public static readonly HashSet<uint> MultiSidedWallReceiverItemIds = [24514u, 33267u, 30410u];

    /// <summary>True when the receiver accepts wall items from every face.</summary>
    public static bool IsMultiSidedWallReceiver(uint itemId) => MultiSidedWallReceiverItemIds.Contains(itemId);

    /// <summary>True if the item's collision carries TableTop (d4==8).</summary>
    public static bool IsTableReceiver(uint itemId) => TableReceiverItemIds.Contains(itemId);

    /// <summary>Display name for any covered item, or empty string.</summary>
    public static string GetItemName(uint itemId) => ItemNames.TryGetValue(itemId, out var n) ? n : string.Empty;
  }
}

