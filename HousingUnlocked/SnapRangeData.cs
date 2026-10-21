using System.Collections.Generic;

namespace HousingUnlocked
{
  /// <summary>
  /// Item-to-item snap ranges extracted from SphereCastRange (LayerEntryType 0x4B) blocks
  /// in the game's housing .sgb files (bgcommon/hou/indoor|outdoor/general/XXXX/asset/*).
  /// 
  /// Extraction: all 1907 housing .sgb files scanned for tag 4B 00 00 00; a block is
  /// real when its tail bytes are 02 00 00 00 64 00.. (normal) or 00 00 16 43.. (scatter).
  /// Normal blocks always carry F1==F2==F3 — a uniform sphere-cast radius in yalms,
  /// which drives item-to-item snap/attach behavior (Hanging Bench 0.4, Stage Panel 0.1,
  /// arcade machines 1.0-1.5, beds 1.8, stables 2.2, Raised Wooden Deck 2.7, ...).
  /// File names were joined to item IDs/names via HousingFurniture/HousingYardObject
  /// ModelKey (fun_b0_m0115.sgb -> ModelKey 115) and the Item sheet.
  /// 
  /// Generated 2026-10-21 — 116 snap items, 3 scatter items.
  /// Refresh per FFXIV patch (housing items are occasionally added).
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
      { 29681u, 0.5265425f }, // Crystallized Coral
      { 29682u, 0.6f }, // White Screen
      { 30382u, 0.4f }, // Sapphire Weapon Bust
      { 30408u, 1.733347f }, // Wooden Footbridge
      { 30410u, 0.56f }, // Marble Alcove Bed
      { 30422u, 0.001f }, // Slanted Wall
      { 30423u, 0.001f }, // Slanted Skylight
      { 30424u, 1f }, // The Moogle's Paw Machine
      { 30425u, 1f }, // Out on a Limb Machine
      { 30426u, 1f }, // The Finer Miner Machine
      { 32206u, 0.5f }, // Emerald Weapon Bust
      { 32247u, 0.4f }, // Snow-dusted Tree
      { 32248u, 0.4f }, // Parkside Tree
      { 32250u, 0.5255005f }, // La Noscean Grape Pergola
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
      { 40651u, 0.6252018f }, // Verdant Pillar
      { 41144u, 1f }, // Imitation Magitek Repair Station (Wall Unit)
      { 41146u, 2.458272f }, // Hingan Rock Garden
      { 41147u, 2.54036f }, // Sharlayan Reflecting Pool
      { 41150u, 0.4f }, // Authentic Illuminated Tree
      { 41429u, 2.55f }, // Imitation Magitek Repair Station (Floor Unit)
      { 41435u, 0.3918906f }, // Aloalo Island Palm
      { 44271u, 1f }, // Flying Pumpkin
      { 44892u, 2f }, // Traumerei Floor Stencil
      { 44927u, 1f }, // Garden Palm Tree
      { 44934u, 0.5f }, // Ja Ja Gural Tree
      { 46106u, 1.8f }, // Training Bench
      { 46120u, 0.4f }, // La Noscean Orange Tree
      { 46168u, 0.1f }, // Lyhe Ghiah Stained Crystal Ornament
      { 46170u, 1f }, // Traverse Tree
      { 47228u, 1f }, // Authentic Flying Pumpkin
      { 47352u, 0.4624746f }, // Weeping Cherry Tree
      { 48100u, 0.5f }, // East End Tree
      { 48106u, 0.05f }, // No-entry Door Partition
      { 48115u, 0.2f }, // Vault Rib
    };

    /// <summary>Item RowId -> display name (shared model variants use the shortest name).</summary>
    public static readonly IReadOnlyDictionary<uint, string> ItemNames = new Dictionary<uint, string>
    {
      { 6476u, "Shroud Broadleaf" },
      { 6477u, "Island Palm" },
      { 6478u, "Desert Ironwood" },
      { 6482u, "Linen Parasol" },
      { 6483u, "Lotus Parasol" },
      { 6486u, "Brass Bench" },
      { 6487u, "Oaken Bench" },
      { 6490u, "Riviera Lamppost" },
      { 6495u, "Sylphic Lamp Tree" },
      { 6503u, "Blue Coral Formation" },
      { 6508u, "Death's Fingers" },
      { 6599u, "Llymlaen's Embrace" },
      { 7064u, "Summoning Bell" },
      { 7989u, "Chocobo Stable" },
      { 8812u, "Adventurers' Guild Aduyses" },
      { 9719u, "Oriental Bathtub" },
      { 9741u, "Eastern Cherry Tree" },
      { 9744u, "Wild Banana Tree" },
      { 12100u, "Royal Plotting Table" },
      { 12103u, "Bearskin Rug" },
      { 12671u, "Sunflower Plot" },
      { 13061u, "Crystal Bell" },
      { 13065u, "Twin Star Ornament" },
      { 13082u, "Paramour Bed" },
      { 13088u, "Sephirot Root" },
      { 13089u, "Sephirot Tree" },
      { 13090u, "Oriental Grass Plot" },
      { 13091u, "Dravanian Down Tree" },
      { 15152u, "Nidhogg Miniature" },
      { 15953u, "Riviera Chocobo Stable" },
      { 15954u, "Glade Chocobo Stable" },
      { 15955u, "Oasis Chocobo Stable" },
      { 15971u, "Knightly Round Table" },
      { 15981u, "Autumnal Maple Tree" },
      { 15982u, "Autumnal Ginkgo Tree" },
      { 15983u, "Maple Leaf Pile" },
      { 15984u, "Ginkgo Leaf Pile" },
      { 15985u, "Snow Drift" },
      { 15996u, "Jumbo Twin Star Ornament" },
      { 16781u, "Troupe Stage" },
      { 17019u, "Plundered Treasure" },
      { 17029u, "Eastern Cherry Petal Pile" },
      { 17039u, "Authentic Paramour Bed" },
      { 17044u, "Authentic Eastern Cherry Tree" },
      { 17137u, "Oriental Chocobo Stable" },
      { 17982u, "Eastern Pine" },
      { 20772u, "Far Eastern Willow" },
      { 20817u, "Handmade Snow Drift" },
      { 21839u, "Stage Panel" },
      { 21864u, "Corner Hedge Partition" },
      { 21865u, "Bamboo Copse" },
      { 21866u, "Hingan Hot Spring" },
      { 21873u, "Authentic Eastern Cherry Petal Pile" },
      { 22521u, "Crystal Tower Striker" },
      { 22577u, "Monster Toss Machine" },
      { 22578u, "Cuff-a-Cur Machine" },
      { 22581u, "Miniature Heaven-on-High" },
      { 23864u, "Doman Dogwood" },
      { 23870u, "Silver Starlight Sentinel" },
      { 23883u, "Bread Rack" },
      { 23892u, "Verdant Partition" },
      { 23893u, "Bathroom Floor Tiles" },
      { 23898u, "Garden Gravel" },
      { 24535u, "Simple Treehouse" },
      { 24537u, "Big One Festival Float" },
      { 27278u, "Hunting Supplies" },
      { 28161u, "Cedar Tree" },
      { 28911u, "Authentic Silver Starlight Sentinel" },
      { 28996u, "Eastern Stage" },
      { 28999u, "Lakeland Elf Tree" },
      { 29681u, "Crystallized Coral" },
      { 29682u, "White Screen" },
      { 30382u, "Sapphire Weapon Bust" },
      { 30408u, "Wooden Footbridge" },
      { 30410u, "Marble Alcove Bed" },
      { 30422u, "Slanted Wall" },
      { 30423u, "Slanted Skylight" },
      { 30424u, "The Moogle's Paw Machine" },
      { 30425u, "Out on a Limb Machine" },
      { 30426u, "The Finer Miner Machine" },
      { 32206u, "Emerald Weapon Bust" },
      { 32247u, "Snow-dusted Tree" },
      { 32248u, "Parkside Tree" },
      { 32250u, "La Noscean Grape Pergola" },
      { 32251u, "Fruitful Fountain" },
      { 32966u, "Authentic Eastern Stage" },
      { 33297u, "Eastern Wisteria" },
      { 35583u, "Hanging Bench" },
      { 35584u, "Coldbare Tree" },
      { 35617u, "Highland Chocobo Stable" },
      { 35807u, "Thavnairian Dhow" },
      { 36380u, "Deluxe Snow-dusted Tree" },
      { 37397u, "Alpine Open-air Bath" },
      { 37400u, "Raised Wooden Deck" },
      { 38624u, "Illuminated Tree" },
      { 39421u, "Smaller Water Otter Fountain" },
      { 40651u, "Verdant Pillar" },
      { 41144u, "Imitation Magitek Repair Station (Wall Unit)" },
      { 41146u, "Hingan Rock Garden" },
      { 41147u, "Sharlayan Reflecting Pool" },
      { 41150u, "Authentic Illuminated Tree" },
      { 41429u, "Imitation Magitek Repair Station (Floor Unit)" },
      { 41435u, "Aloalo Island Palm" },
      { 44271u, "Flying Pumpkin" },
      { 44892u, "Traumerei Floor Stencil" },
      { 44927u, "Garden Palm Tree" },
      { 44934u, "Ja Ja Gural Tree" },
      { 46106u, "Training Bench" },
      { 46120u, "La Noscean Orange Tree" },
      { 46168u, "Lyhe Ghiah Stained Crystal Ornament" },
      { 46170u, "Traverse Tree" },
      { 47228u, "Authentic Flying Pumpkin" },
      { 47352u, "Weeping Cherry Tree" },
      { 48100u, "East End Tree" },
      { 48106u, "No-entry Door Partition" },
      { 48115u, "Vault Rib" },
    };

    /// <summary>
    /// Anisotropic scatter blocks (F=(1000, 1, 20), tail floats (150, 0.5, 1.0)) —
    /// only Cherry Petal Pile and Hingan Rock Garden carry these. NOT used for the
    /// snap warning; kept for future scatter-placement research.
    /// </summary>
    public static readonly IReadOnlyDictionary<uint, (float X, float Y, float Z)> ItemScatterRanges = new Dictionary<uint, (float X, float Y, float Z)>
    {
      { 17029u, (1000f, 1f, 20f) }, // Eastern Cherry Petal Pile — SGB_Outdoors\gar_b0_m0112.sgb
      { 21873u, (1000f, 1f, 20f) }, // Authentic Eastern Cherry Petal Pile — SGB_Outdoors\gar_b0_m0112.sgb
      { 41146u, (1000f, 1f, 20f) }, // Hingan Rock Garden — SGB_Outdoors\gar_b0_m0311.sgb
    };

    /// <summary>True if the item carries a normal (uniform sphere) snap range.</summary>
    public static bool TryGetSnapRange(uint itemId, out float range) => ItemSnapRanges.TryGetValue(itemId, out range);

    /// <summary>Display name for an item in the snap table, or empty string.</summary>
    public static string GetItemName(uint itemId) => ItemNames.TryGetValue(itemId, out var n) ? n : string.Empty;
  }
}

