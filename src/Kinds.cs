namespace Svimma
{
    /// <summary>
    /// Which hand item is which kind of thing, off the item's own data rather than a list of
    /// names, so a modded sword lands in "melee" and a modded rod in "fishing" for free.
    ///
    /// Order matters. Crossbows are ItemType.Bow like bows are, so the skill is read first;
    /// staffs are two-handed weapons that differ only by skill; a torch carries the Clubs skill
    /// and would pass as a club if its item type were not read before it.
    /// </summary>
    internal static class Kinds
    {
        /// <summary>
        /// Whether this item may stay in hand, or be taken out, in deep water. Nothing at all is
        /// allowed trivially. Anything that is not a hand item (armour, food, trinkets) is not,
        /// which keeps vanilla's refusal for exactly the things the ticket was not about.
        /// </summary>
        internal static bool Allowed(ItemDrop.ItemData item)
        {
            if (item == null) return true;

            var shared = item.m_shared;

            if (shared.m_buildPieces != null) return SvimmaConfig.AllowBuildingTools.Value;

            switch (shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Torch:
                    return false;

                case ItemDrop.ItemData.ItemType.Shield:
                    return true;

                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Tool:
                    break;

                default:
                    return false;
            }

            switch (shared.m_skillType)
            {
                case Skills.SkillType.Fishing:
                    return SvimmaConfig.AllowFishing.Value;
                case Skills.SkillType.Pickaxes:
                    return SvimmaConfig.AllowPickaxes.Value;
                case Skills.SkillType.Crossbows:
                    return SvimmaConfig.AllowCrossbows.Value;
                case Skills.SkillType.Bows:
                    return SvimmaConfig.AllowBows.Value;
                case Skills.SkillType.ElementalMagic:
                case Skills.SkillType.BloodMagic:
                    return SvimmaConfig.AllowStaffs.Value;
            }

            if (shared.m_itemType == ItemDrop.ItemData.ItemType.Bow) return SvimmaConfig.AllowBows.Value;

            return SvimmaConfig.AllowMelee.Value;
        }

        internal static string Name(ItemDrop.ItemData item)
        {
            if (item == null) return "-";

            return item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name;
        }
    }
}
