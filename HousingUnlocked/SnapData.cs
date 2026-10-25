using System.Collections.Generic;

namespace HousingUnlocked
{
  /// <summary>
  /// Per-item housing snap behavior, extracted from Re:MakePlace's ItemData.uasset.
  /// Tags identify items that the FFXIV housing engine snaps to floor or wall when
  /// placed within the engine's minimum-distance threshold.
  /// </summary>
  /// <remarks>
  /// IDs are FFXIV Item RowIds (matching <c>HousingFurniture.Item.Value.RowId</c>).
  /// Counts: 655 floor-snap items, 358 wall-snap items.
  /// Source: Re:MakePlace standalone app (UE5 IoStore Zen package). Refresh per FFXIV patch.
  /// </remarks>
  public static class SnapData
  {
    public static readonly HashSet<uint> FloorSnapItemIds =
    [
      6513u, 6514u, 6515u, 6516u, 6517u, 6518u, 6519u, 6520u, 6521u, 6523u, 6524u, 6525u,
      6526u, 6527u, 6528u, 6529u, 6530u, 6531u, 6532u, 6533u, 6534u, 6535u, 6536u, 6537u,
      6538u, 6539u, 6540u, 6542u, 6543u, 6544u, 6545u, 6546u, 6547u, 6548u, 6549u, 6550u,
      6551u, 6552u, 6553u, 6554u, 6555u, 6556u, 6557u, 6558u, 6559u, 6560u, 6561u, 6562u,
      6563u, 6564u, 6565u, 6566u, 6567u, 6568u, 6569u, 6570u, 6571u, 6572u, 6573u, 6574u,
      6575u, 6577u, 6578u, 6579u, 6580u, 6581u, 6582u, 6583u, 6584u, 6585u, 6586u, 6587u,
      6588u, 6589u, 6590u, 6591u, 6592u, 6593u, 6594u, 6595u, 6596u, 6597u, 6598u, 6599u,
      6601u, 6602u, 6603u, 6604u, 6605u, 6606u, 6607u, 6608u, 6609u, 6610u, 6611u, 6612u,
      6613u, 6614u, 6615u, 6616u, 6617u, 6618u, 6619u, 6620u, 6621u, 6622u, 6623u, 6624u,
      6625u, 6626u, 6627u, 6628u, 6629u, 6630u, 6631u, 6632u, 6633u, 6634u, 6645u, 6646u,
      6673u, 6674u, 6675u, 6676u, 6677u, 6678u, 6679u, 6680u, 6681u, 6682u, 6683u, 6684u,
      6685u, 7064u, 7065u, 7066u, 7068u, 7069u, 7070u, 7071u, 7072u, 7075u, 7076u, 7077u,
      7078u, 7079u, 7080u, 7081u, 7083u, 7084u, 7087u, 7088u, 7089u, 7090u, 7091u, 7092u,
      7098u, 7099u, 7102u, 7108u, 7109u, 7110u, 7111u, 7112u, 7113u, 7114u, 7972u, 7973u,
      7974u, 7975u, 7977u, 7978u, 7979u, 7980u, 7981u, 7982u, 7983u, 7984u, 7986u, 7987u,
      7988u, 8777u, 8778u, 8779u, 8780u, 8781u, 8782u, 8783u, 8784u, 8785u, 8786u, 8787u,
      8788u, 8789u, 8790u, 8791u, 8793u, 8794u, 8795u, 8796u, 8797u, 8806u, 8813u, 8814u,
      8815u, 9718u, 9719u, 9720u, 9722u, 9723u, 9724u, 9732u, 9733u, 9734u, 9736u, 9737u,
      12084u, 12085u, 12095u, 12096u, 12097u, 12098u, 12099u, 12100u, 12101u, 12102u, 12103u, 12104u,
      12105u, 12106u, 12107u, 13061u, 13063u, 13069u, 13071u, 13072u, 13073u, 13075u, 13076u, 13077u,
      13078u, 13082u, 14045u, 14048u, 14049u, 14050u, 14051u, 15143u, 15144u, 15147u, 15150u, 15151u,
      15153u, 15971u, 15974u, 15975u, 15976u, 15990u, 16780u, 16781u, 16782u, 17019u, 17024u, 17025u,
      17028u, 17039u, 17953u, 17956u, 17957u, 17960u, 17973u, 17974u, 17978u, 17989u, 20726u, 20727u,
      20728u, 20729u, 20730u, 20731u, 20732u, 20733u, 20734u, 20735u, 20736u, 20737u, 20738u, 20739u,
      20740u, 21106u, 21107u, 21108u, 21109u, 21207u, 21811u, 21816u, 21817u, 21829u, 21830u, 21831u,
      21832u, 21833u, 21839u, 21844u, 21845u, 21849u, 22521u, 22536u, 22538u, 22539u, 22540u, 22548u,
      22554u, 22555u, 22556u, 22558u, 22559u, 22569u, 22574u, 22578u, 23097u, 23856u, 23857u, 23859u,
      23864u, 23883u, 23884u, 23892u, 23893u, 24491u, 24504u, 24507u, 24509u, 24514u, 24517u, 24525u,
      24528u, 27271u, 27273u, 27281u, 27282u, 27283u, 27284u, 27298u, 28131u, 28134u, 28140u, 28143u,
      28154u, 28635u, 28637u, 28638u, 28639u, 28963u, 28964u, 28971u, 28973u, 28974u, 28981u, 28982u,
      28991u, 28992u, 29680u, 29682u, 29685u, 29686u, 30383u, 30385u, 30390u, 30394u, 30404u, 30405u,
      30407u, 30410u, 30414u, 30416u, 30417u, 32214u, 32215u, 32221u, 32222u, 32225u, 32226u, 32228u,
      32229u, 33252u, 33258u, 33259u, 33260u, 33261u, 33267u, 33268u, 33271u, 33273u, 33276u, 33279u,
      33281u, 33282u, 33283u, 33284u, 33285u, 35556u, 35560u, 35561u, 35564u, 35565u, 35567u, 35572u,
      35576u, 35578u, 35580u, 35581u, 35801u, 37347u, 37348u, 37349u, 37352u, 37355u, 37357u, 37361u,
      37363u, 37365u, 37366u, 37367u, 37368u, 37369u, 37370u, 37374u, 37375u, 37376u, 37377u, 37378u,
      37379u, 37381u, 37382u, 38579u, 38581u, 38584u, 38586u, 38593u, 38594u, 38595u, 38596u, 38597u,
      38598u, 38599u, 38600u, 38601u, 38602u, 38610u, 38611u, 39378u, 39381u, 39391u, 39392u, 39393u,
      39394u, 39396u, 39402u, 39403u, 39404u, 39405u, 39407u, 39408u, 39409u, 39411u, 39414u, 39415u,
      39416u, 40623u, 40627u, 40628u, 40637u, 40639u, 40640u, 40641u, 40643u, 40644u, 40648u, 40649u,
      40651u, 40652u, 40657u, 40658u, 40659u, 40662u, 40663u, 40672u, 40673u, 40764u, 41088u, 41089u,
      41090u, 41091u, 41099u, 41100u, 41105u, 41109u, 41110u, 41111u, 41150u, 41152u, 41428u, 41432u,
      41651u, 41816u, 41817u, 41824u, 41825u, 41826u, 41827u, 41828u, 41829u, 44111u, 44114u, 44115u,
      44116u, 44118u, 44269u, 44272u, 44873u, 44875u, 44876u, 44880u, 44885u, 44886u, 44889u, 44890u,
      44892u, 44895u, 44896u, 44898u, 44900u, 44902u, 44903u, 44905u, 44912u, 44913u, 44925u, 44930u,
      44931u, 44932u, 44933u, 44970u, 44971u, 46090u, 46091u, 46092u, 46093u, 46094u, 46095u, 46096u,
      46097u, 46099u, 46100u, 46101u, 46102u, 46103u, 46105u, 46106u, 46107u, 46109u, 46110u, 46111u,
      46112u, 46113u, 46114u, 46115u, 46121u, 46122u, 46123u, 46124u, 46125u, 46126u, 46127u, 46128u,
      46129u, 46134u, 46165u, 46166u, 46167u, 46169u, 46170u, 46173u, 46174u, 46176u, 46177u, 47348u,
      47352u, 47476u, 47478u, 48007u, 48100u, 48101u, 48102u, 48103u, 48104u, 48105u, 48106u, 48107u,
      48108u, 48110u, 48113u, 48114u, 48115u, 48116u, 48129u, 48131u, 48134u, 48135u, 48136u, 48140u,
      48141u, 48143u, 48732u, 48733u, 48735u, 49838u, 49839u, 49846u, 49848u, 49850u, 49854u, 49856u,
      49857u, 49858u, 49870u, 49871u, 49872u, 49873u, 49874u, 49875u, 50416u, 50417u, 50418u, 50419u,
      50421u, 50422u, 50425u, 51251u, 51253u, 51254u, 51255u, 51256u, 51257u, 51258u, 51259u, 51260u,
      51261u, 51265u, 51266u, 51267u, 51269u, 51271u, 51314u
    ];

    public static readonly HashSet<uint> WallSnapItemIds =
    [
      6656u, 6657u, 6658u, 6659u, 6660u, 6661u, 6662u, 6663u, 6664u, 6665u, 6666u, 6667u,
      6668u, 6669u, 6670u, 6671u, 6672u, 7074u, 7082u, 7093u, 7095u, 7096u, 7097u, 7152u,
      7958u, 7968u, 7969u, 7970u, 8730u, 8731u, 8805u, 8807u, 8808u, 8809u, 8810u, 8811u,
      8812u, 9730u, 9731u, 12093u, 13064u, 13065u, 13289u, 13339u, 15156u, 15970u, 15994u, 15995u,
      15996u, 16147u, 16779u, 16935u, 16936u, 16937u, 17018u, 17022u, 17955u, 17975u, 20306u, 20310u,
      20623u, 20639u, 20742u, 20743u, 20744u, 20752u, 20753u, 20754u, 20764u, 20768u, 20816u, 21311u,
      21315u, 21812u, 21815u, 21820u, 21826u, 21827u, 21835u, 21836u, 21872u, 22405u, 22407u, 22541u,
      22542u, 22546u, 22547u, 22560u, 22572u, 23339u, 23863u, 23865u, 23867u, 23869u, 23877u, 23891u,
      23894u, 24501u, 24508u, 24510u, 24511u, 24512u, 24513u, 24526u, 27272u, 27274u, 27276u, 27280u,
      27285u, 27288u, 27289u, 27290u, 27299u, 27300u, 27306u, 27307u, 27308u, 27309u, 27310u, 27311u,
      27981u, 27983u, 28129u, 28133u, 28139u, 28152u, 28501u, 28502u, 28503u, 28636u, 28915u, 28931u,
      28932u, 28933u, 28934u, 28966u, 28972u, 28975u, 28979u, 28989u, 28990u, 28995u, 28997u, 29684u,
      30381u, 30387u, 30389u, 30397u, 30398u, 30400u, 30401u, 30403u, 30406u, 30411u, 30429u, 30883u,
      31706u, 31707u, 31708u, 31709u, 31710u, 31711u, 31713u, 32209u, 32219u, 32224u, 32232u, 32961u,
      32963u, 32967u, 32969u, 33265u, 33286u, 33287u, 33291u, 33293u, 33304u, 35557u, 35562u, 35563u,
      35571u, 35573u, 35575u, 35577u, 35799u, 35803u, 36382u, 36631u, 37351u, 37353u, 37354u, 37359u,
      37360u, 37362u, 37364u, 37371u, 37373u, 37388u, 37389u, 37390u, 37391u, 37392u, 38576u, 38577u,
      38580u, 38582u, 38583u, 38587u, 38591u, 38603u, 38605u, 38606u, 38607u, 38608u, 38609u, 38613u,
      38614u, 38615u, 38670u, 38676u, 38677u, 38678u, 38679u, 38680u, 38681u, 38682u, 38683u, 38697u,
      38698u, 38699u, 38700u, 38701u, 38702u, 38703u, 38704u, 38705u, 38706u, 38707u, 38708u, 39380u,
      39384u, 39388u, 39389u, 39395u, 39401u, 39410u, 39417u, 39418u, 39433u, 39435u, 40624u, 40630u,
      40631u, 40634u, 40635u, 40636u, 40645u, 40646u, 40647u, 40655u, 40656u, 40660u, 40664u, 40665u,
      40666u, 40759u, 40760u, 40761u, 40762u, 40763u, 41085u, 41087u, 41097u, 41103u, 41104u, 41113u,
      41115u, 41116u, 41117u, 41118u, 41119u, 41120u, 41121u, 41122u, 41123u, 41124u, 41125u, 41126u,
      41127u, 41128u, 41129u, 41131u, 41132u, 41133u, 41134u, 41135u, 41136u, 41153u, 41154u, 41430u,
      41822u, 41823u, 43558u, 43559u, 43560u, 43561u, 43562u, 43563u, 44119u, 44120u, 44332u, 44333u,
      44874u, 44877u, 44878u, 44879u, 44883u, 44891u, 44897u, 44899u, 44904u, 44906u, 44907u, 44908u,
      44911u, 44914u, 46104u, 46117u, 46119u, 46163u, 46168u, 46172u, 47350u, 48006u, 48109u, 48111u,
      48112u, 48133u, 48144u, 49845u, 49847u, 49849u, 49853u, 49860u, 49861u, 49862u, 49864u, 49865u,
      49867u, 49868u, 49869u, 50083u, 50085u, 50086u, 50088u, 50360u, 51264u, 51298u
    ];

    public static bool HasFloorSnap(uint itemId) => FloorSnapItemIds.Contains(itemId);
    public static bool HasWallSnap(uint itemId) => WallSnapItemIds.Contains(itemId);

    /// <summary>
    /// Determines whether the currently-active item is at risk of snapping
    /// to whatever surface is directly below it.
    /// </summary>
    /// <param name="itemId">FFXIV Item RowId of the selected item (0 if unknown).</param>
    /// <param name="distanceToFloor">Distance from item base to nearest collision surface
    /// directly below, as measured by a downward raycast.</param>
    /// <param name="threshold">Distance below which the snap fires.</param>
    /// <returns>True if the item is FloorSnap-tagged AND its distance to the
    /// surface below is at or under <paramref name="threshold"/>.</returns>
    /// <remarks>
    /// This is the same general check the game's own placement validator performs.
    /// Because the surface comes from a real raycast, this works correctly for
    /// stairs, railings, stacked items, the basement, upper floors, terraces,
    /// outdoor terrain — anywhere the housing engine recognizes a placement surface.
    /// </remarks>
    public static bool IsInFloorSnapZone(uint itemId, float distanceToFloor, float threshold)
    {
      if (itemId == 0 || !HasFloorSnap(itemId)) return false;
      // Small tolerance — float math frequently shows e.g. 0.10000001 vs 0.0999999
      // for values typed as 0.1, which would otherwise cause boundary flicker.
      // Kept at 0.02 mm so the exact model-box boundaries reproduce to the
      // millimetre (White Rect 0.112 red / 0.113 green; Log Pillar 0.371 / 0.372).
      const float Epsilon = 0.00002f;
      return distanceToFloor <= threshold + Epsilon;
    }
  }
}
