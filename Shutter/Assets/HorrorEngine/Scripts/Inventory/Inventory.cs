using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorEngine
{
   

    public class EquippedItemChangedMessage : CharacterMessage
    {
        public InventoryEntry InventoryEntry;
        public EquipmentSlot Slot;
    }

    public class InventoryItemAddedMessage : CharacterMessage
    {
        public ItemData Item;
    }

    public class InventoryItemRemovedMessage : CharacterMessage
    {
        public ItemData Item;
    }
    
    public enum EquipmentSlot
    {
        None,
        Primary,
        Secondary
    }

    [System.Serializable]
    public class InventoryEntry
    {
        public ItemData Item;
        public int Count;
        public int SecondaryCount;
        public float Status;

        public static void Swap(InventoryEntry entry1, InventoryEntry entry2)
        {
            ItemData itemTmp = entry1.Item;
            int countTmp = entry1.Count;
            int secondaryCountTmp = entry1.SecondaryCount;
            float statusTmp = entry1.Status;

            entry1.Item = entry2.Item;
            entry1.Count = entry2.Count;
            entry1.Status = entry2.Status;
            entry1.SecondaryCount = entry2.SecondaryCount;

            entry2.Item = itemTmp;
            entry2.Count = countTmp;
            entry2.Status = statusTmp;
            entry2.SecondaryCount = secondaryCountTmp;
        }

        public void Copy(InventoryEntry other)
        {
            Item = other.Item;
            Count = other.Count;
            Status = other.Status;
            SecondaryCount = other.SecondaryCount;
        }
    }

    [System.Serializable]
    public class AmmoEntry // Entry used for the ammo that's inside the weapons
    {
        public int Amount;
        public WeaponData Weapon;
    }


    [System.Serializable]
    public struct InventoryEntrySaveData
    {
        public string ItemId;
        public int Count;
        public int SecondaryCount;
        public float Status;
    }

    [System.Serializable]
    public struct InventoryEquippedEntrySaveData
    {
        public int InventoryIndex;
        public InventoryEntrySaveData OptionalData;
        public EquipmentSlot Slot;
    }

    [System.Serializable]
    public struct InventorySaveData
    {
        public List<InventoryEntrySaveData> Items;
        public List<InventoryEquippedEntrySaveData> EquippedItems;
        public int EquipedWeaponEntry;
        public List<string> Documents;
        public List<string> Maps;
        public int MaxItems;
    }

    [System.Serializable]
    public class InventorySetup 
    {
        [Tooltip("Activating this will stack added items with existing ones in the inventory (Only works for stackable items)")]
        public bool AutoStack;
        public List<InventoryItemCombination> Combinations;
        public DialogData OnFullDialog;
    }

    [System.Serializable]
    public class Inventory : ISavable<InventorySaveData>
    {
        public static int k_DefaultSize = 8;

        [HideInInspector] public InventoryEntry[] Items;
        [HideInInspector] public HashSet<DocumentData> Documents = new HashSet<DocumentData>();
        [HideInInspector] public HashSet<MapData> Maps = new HashSet<MapData>();

        [SerializeField] private int m_MaxItems = 8;
        
        public EquipmentSlot WeaponSlot = EquipmentSlot.Primary;

        public bool Expanded { get; set; }
        public int PreExpansionSize { get; private set; }

        public Dictionary<EquipmentSlot, InventoryEntry> Equipped => m_Equipped;

        private Dictionary<EquipmentSlot, InventoryEntry> m_Equipped = new Dictionary<EquipmentSlot, InventoryEntry>();
        private EquippedItemChangedMessage m_EquippedChangedMsg = new EquippedItemChangedMessage();
        private InventoryItemAddedMessage m_ItemAddedMsg = new InventoryItemAddedMessage();
        private InventoryItemRemovedMessage m_ItemRemovedMsg = new InventoryItemRemovedMessage();

        
        // --------------------------------------------------------------------

        public void Init(CharacterData character)
        {
            m_EquippedChangedMsg.Character = character;
            m_ItemAddedMsg.Character = character;
            m_ItemRemovedMsg.Character = character;

            m_MaxItems = character.InitialInventorySize;
            Items = new InventoryEntry[m_MaxItems];
            for (int i = 0; i < Items.Length; ++i)
                Items[i] = new InventoryEntry();

            foreach (var item in character.InitialInventory)
            {
                Add(item);
            }

            foreach (var doc in character.InitialDocuments)
            {
                Documents.Add(doc);
            }

            foreach (var map in character.InitialMaps)
            {
                Maps.Add(map);
            }
        }

        // --------------------------------------------------------------------

        public void Clear()
        {
            Items = new InventoryEntry[m_MaxItems];
            for (int i = 0; i < Items.Length; ++i)
                Items[i] = new InventoryEntry();

            m_Equipped.Clear();
            Documents.Clear();
            Maps.Clear();
        }

        // --------------------------------------------------------------------

        public InventoryEntry Add(InventoryEntry entry)
        {
            return Add(entry.Item, entry.Count, entry.SecondaryCount, entry.Status);
        }

        // --------------------------------------------------------------------

        private List<InventoryEntry> m_FoundInventoryEntries = new List<InventoryEntry>();
        public InventoryEntry Add(ItemData item, int amount = 1, int secondaryAmount = 0, float status = 0f)
        {
            Debug.Assert(amount > 0, "Amount of items to add to the inventory has to be greater than 0 (0 is allowed since reloadable weapons use Count for ammo)");
            if (amount < 0) return null;

            m_ItemAddedMsg.Item = item;

            if (item.Flags.HasFlag(ItemFlags.Stackable)) // Put all added items together
            {
                if (GameManager.Instance.InventorySetup.AutoStack) // Put all added items with already existing ones
                {
                    m_FoundInventoryEntries.Clear();
                    GetAll(item, m_FoundInventoryEntries);
                    if (m_FoundInventoryEntries.Count > 0)
                    {
                        foreach(var entry in m_FoundInventoryEntries)
                        {
                            // Can be autostack as long as they don't surpass the MaxStackSize
                            if (item.MaxStackSize == 0 || (entry.Count + amount) <= item.MaxStackSize)
                            {
                                entry.Count += amount;
                                entry.SecondaryCount += secondaryAmount;
                                entry.Status += status;
                                return entry;
                            }
                        }
                    }
                }

                int index = GetNextAvailableIndex();
                Debug.Assert(index >= 0, "Trying to add an item to the inventory, but the inventory is full");
                Items[index].Count = amount;
                Items[index].Item = item;
                Items[index].SecondaryCount = secondaryAmount;
                Items[index].Status = status;
                MessageBuffer<InventoryItemAddedMessage>.Dispatch(m_ItemAddedMsg);

                return Items[index];
            }
            else
            {
                int lastIndex = 0;
                for (int i = 0; i < amount; ++i)
                {
                    int index = GetNextAvailableIndex();
                    Debug.Assert(index >= 0, "Trying to add an non-stackable items to the inventory, but the inventory is full");
                    Items[index].Count = 1;
                    Items[index].Item = item;
                    Items[index].SecondaryCount = secondaryAmount;
                    Items[index].Status = status;
                    lastIndex = index;
                }

                MessageBuffer<InventoryItemAddedMessage>.Dispatch(m_ItemAddedMsg);

                return Items[lastIndex];
            }
        }

        // --------------------------------------------------------------------

        private int GetNextAvailableIndex()
        {
            for (int i = 0; i < Items.Length; ++i)
                if (!Items[i].Item)
                    return i;

            return -1;
        }

        // --------------------------------------------------------------------

        public void Remove(ItemData item, int amount = 1, bool unequip = true)
        {
            for (int i = 0; i < Items.Length; ++i)
            {
                if (item == Items[i].Item)
                {
                    Remove(Items[i], amount, unequip);
                    return;
                }
            }
        }

        // --------------------------------------------------------------------

        public void Remove(InventoryEntry entry, int amount = 1, bool unequip = true, bool isDrop = false)
        {
            entry.Count -= amount;
            m_ItemRemovedMsg.Item = entry.Item;
            Debug.Assert(entry.Count >= 0, "Item amount went negative");
            if (entry.Count == 0)
            {
                if (unequip)
                {
                    EquipmentSlot slotToUnequip = EquipmentSlot.None;
                    foreach (var equip in m_Equipped)
                    {
                        if (entry == equip.Value)
                        {
                            slotToUnequip = equip.Key;
                        }
                    }

                    if (slotToUnequip != EquipmentSlot.None)
                        Unequip(slotToUnequip, isDrop);
                }

                entry.Item = null; // This needs to happen after unequip

                MessageBuffer<InventoryItemRemovedMessage>.Dispatch(m_ItemRemovedMsg);
            }
        }

        // --------------------------------------------------------------------

        public void Drop(InventoryEntry inventoryEntry)
        {
            Remove(inventoryEntry, inventoryEntry.Count, true, true);
        }

        // --------------------------------------------------------------------

        public bool TryGet(ItemData item, out InventoryEntry outEntry)
        {
            outEntry = null;

            foreach (var entry in Items)
            {
                if (entry.Item == item)
                {
                    outEntry = entry;
                    return true;
                }
            }

            return false;
        }

        // --------------------------------------------------------------------

        public void GetAll(ItemData item, List<InventoryEntry> outEntries)
        {
            foreach (var entry in Items)
            {
                if (entry.Item == item)
                {
                    outEntries.Add(entry);
                }
            }
        }


        // --------------------------------------------------------------------

        public bool Contains(ItemData item)
        {
            return TryGet(item, out InventoryEntry outEntry);
        }

        // --------------------------------------------------------------------

        public InventoryEntry Combine(InventoryEntry entry1, InventoryEntry entry2)
        {
            foreach (var combi in GameManager.Instance.InventorySetup.Combinations)
            {
                if (combi.CanCombine(entry1, entry2))
                {
                    return combi.OnCombine(entry1, entry2);
                }
                else if (combi.CanCombine(entry2, entry1))
                {
                    return combi.OnCombine(entry2, entry1);
                }
            }

            // Auto combine stackable items
            if (entry1.Item == entry2.Item && entry1.Item.Flags.HasFlag(ItemFlags.Stackable) && entry1 != entry2)
            {
                int sum = entry1.Count + entry2.Count;
                int diff = sum - entry1.Item.MaxStackSize;
                if (diff > 0 && entry1.Item.MaxStackSize > 0)
                {
                    entry1.Count = sum - diff;
                    entry2.Count = diff;
                }
                else
                {
                    entry1.Count = sum;
                    Remove(entry2, entry2.Count);
                }
                
                return entry1;
            }

            // Auto combine weapon/ammo
            ReloadableWeaponData reloadable1 = entry1.Item as ReloadableWeaponData;
            ReloadableWeaponData reloadable2 = entry2.Item as ReloadableWeaponData;
            if (reloadable1 || reloadable2)
            {
                InventoryEntry reloadableEntry = reloadable1 ? entry1 : entry2;
                InventoryEntry ammoEntry = reloadable1 ? entry2 : entry1;
                
                ReloadableWeaponData reloadable = reloadableEntry.Item as ReloadableWeaponData;
                if (reloadable.AmmoItem == ammoEntry.Item)
                {
                    return ReloadWeapon(reloadableEntry, ammoEntry);
                }
            }


            return entry1;
        }

        // --------------------------------------------------------------------

        public InventoryEntry ReloadWeapon(InventoryEntry weaponEntry, InventoryEntry ammoEntry)
        {
            ReloadableWeaponData weapon = weaponEntry.Item as ReloadableWeaponData;
            
            int prevAmmo = weaponEntry.SecondaryCount;
            int newAmmo = Mathf.Min(prevAmmo + ammoEntry.Count, weapon.MaxAmmo);
            weaponEntry.SecondaryCount = newAmmo;
            int dif = newAmmo - prevAmmo;

            Remove(ammoEntry, dif);
            return weaponEntry;
        }

        // --------------------------------------------------------------------

        public bool Use(ItemData item)
        {
            if (TryGet(item, out InventoryEntry outEntry))
            {
                return Use(outEntry);
            }

            return false;
        }

        // --------------------------------------------------------------------

        public bool Use(InventoryEntry entry)
        {
            if (entry.Item.InventoryAction == InventoryMainAction.None)
                return false;

            entry.Item.OnUse(entry);

            if (entry.Item)
            {
                if (entry.Item.Flags.HasFlag(ItemFlags.UseOnInteractive))
                {
                    InteractionColliderDetector interactionDetect = GameManager.Instance.Player.GetComponentInChildren<InteractionColliderDetector>();
                    if (interactionDetect.FocusedInteractive)
                    {
                        List<Interactive> interactives = interactionDetect.DetectedInteractives;
                        bool used = false;
                        foreach (var interactive in interactives)
                        {
                            InteractiveWithItemUse[] itemUses = interactive.GetComponents<InteractiveWithItemUse>();
                            foreach (var itemUse in itemUses) 
                            {
                                used = itemUse.UseWithItem(entry.Item);
                            }
                        }

                        return used;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (entry.Item.Flags.HasFlag(ItemFlags.ConsumeOnUse))
                    Remove(entry, 1);
            }

            return true;
        }

        // --------------------------------------------------------------------

        public void Equip(InventoryEntry entry)
        {
            EquipableItemData equipable = entry.Item as EquipableItemData;
            Debug.Assert(equipable, "Trying to equip an item that is not a EquipableItemData", entry.Item);

            m_Equipped.TryGetValue(equipable.Slot, out InventoryEntry prevEquippedEntry);
            
            if (equipable.MoveOutOfInventoryOnEquip)
            {
                entry = new InventoryEntry()
                {
                    Count = entry.Count,
                    SecondaryCount = entry.SecondaryCount,
                    Status = entry.Status,
                    Item = entry.Item
                };
                Remove(entry.Item, 1, false);
            }

            Unequip(equipable.Slot); // Needs to happen after the new item is removed from inventory
            
            m_Equipped.Add(equipable.Slot, entry);
            
            m_EquippedChangedMsg.InventoryEntry = entry;
            m_EquippedChangedMsg.Slot = equipable.Slot;
            MessageBuffer<EquippedItemChangedMessage>.Dispatch(m_EquippedChangedMsg);
        }

        // --------------------------------------------------------------------

        public void Unequip(EquipmentSlot type, bool isDrop = false)
        {
            m_Equipped.TryGetValue(type, out InventoryEntry entry);
            if (entry != null)
            {
                EquipableItemData equipable = entry.Item as EquipableItemData;
                if (equipable && equipable.MoveOutOfInventoryOnEquip && !isDrop)
                {
                    if (IsFull())
                    {
                        Debug.LogWarning("Couldn't return equipment to inventory. Inventory was full");
                        return;
                    }
                    else
                    {
                        Add(entry);
                    }
                }
            }

            ClearEquipped(type);
        }

        // --------------------------------------------------------------------

        public void ClearEquipped(EquipmentSlot type)
        {
            m_Equipped.Remove(type);

            m_EquippedChangedMsg.InventoryEntry = null;
            m_EquippedChangedMsg.Slot = type;
            MessageBuffer<EquippedItemChangedMessage>.Dispatch(m_EquippedChangedMsg);
        }

        // --------------------------------------------------------------------

        public bool IsFull()
        {
            return GetNextAvailableIndex() == -1;
        }

        // --------------------------------------------------------------------

        public bool CheckIsFull()
        {
            bool isFull = IsFull();
            if (isFull)
            {
                UIManager.Get<UIDialog>().Show(GameManager.Instance.InventorySetup.OnFullDialog);
            }
            return isFull;
        }

        // --------------------------------------------------------------------

        public bool CanReloadEquippedWeapon()
        {
            if (m_Equipped.TryGetValue(WeaponSlot, out var entry))
            {
                return CanReloadWeapon(entry);
            }

            return false;
        }

        // --------------------------------------------------------------------

        public bool CanReloadWeapon(InventoryEntry weaponEntry)
        {
            ReloadableWeaponData reloadableWeapon = weaponEntry.Item as ReloadableWeaponData;
            if (reloadableWeapon)
            {
                return weaponEntry.SecondaryCount < reloadableWeapon.MaxAmmo &&
                    TryGet(reloadableWeapon.AmmoItem, out InventoryEntry ammoEntry);
            }

            return false;
        }

        // --------------------------------------------------------------------

        public InventoryEntry GetEquipped(EquipmentSlot slot) 
        {
            if (m_Equipped.TryGetValue(slot, out InventoryEntry entry))
                return entry;
            else
                return null;
        }

        // --------------------------------------------------------------------

        public InventoryEntry GetEquippedWeapon()
        {
            if (m_Equipped.TryGetValue(WeaponSlot, out InventoryEntry entry))
                return entry;
            else
                return null;
        }
        
        // --------------------------------------------------------------------

        public EquipmentSlot GetOccupyingEquipmentSlot(InventoryEntry entry)
        {
            foreach (var v in m_Equipped)
            {
                if (v.Value == entry)
                {
                    return v.Key;
                }
            }
            return EquipmentSlot.None;
        }

        // --------------------------------------------------------------------

        public void Expand(int amount)
        {
            int prevSize = m_MaxItems;
            m_MaxItems += amount;
            Array.Resize(ref Items, m_MaxItems);

            for (int i = prevSize; i < m_MaxItems; ++i)
            {
                Items[i] = new InventoryEntry();
            }

            PreExpansionSize = prevSize;
            Expanded = true;
        }

        //----------------------------------------------
        // ISavable implementation
        //----------------------------------------------
        public InventorySaveData GetSavableData()
        {
            InventorySaveData saveData = new InventorySaveData();

            // Save items
            saveData.Items = new List<InventoryEntrySaveData>();
            foreach (var entry in Items)
            {
                if (entry != null && entry.Item)
                {
                    saveData.Items.Add( new InventoryEntrySaveData()
                    {
                        ItemId = entry.Item.UniqueId,
                        Count = entry.Count,
                        SecondaryCount = entry.SecondaryCount,
                        Status = entry.Status
                    });
                }
                else
                {
                    saveData.Items.Add(new InventoryEntrySaveData());
                }
            }

            // Save equipped items
            saveData.EquippedItems = new List<InventoryEquippedEntrySaveData>();
            foreach (var equipped in Equipped)
            {
                InventoryEntry entry = equipped.Value;
                int invIndex = Array.IndexOf(Items, entry);
                saveData.EquippedItems.Add(new InventoryEquippedEntrySaveData()
                {
                    InventoryIndex = invIndex,
                    OptionalData = new InventoryEntrySaveData() // This info is useless for invIndex > -1
                    {
                        ItemId = entry.Item.UniqueId,
                        Count = entry.Count,
                        SecondaryCount = entry.SecondaryCount,
                        Status = entry.Status
                    },
                    Slot = equipped.Key
                });
            }

            // Save ammo in weapons
            /*
            int ammoCount = WeaponAmmo.Keys.Count;
            saveData.WeaponAmmo = new InventoryAmmoSaveData[ammoCount];
            int index = 0;
            foreach(var ammoEntry in WeaponAmmo)
            {
                saveData.WeaponAmmo[index] = new InventoryAmmoSaveData()
                {
                    WeaponId = ammoEntry.Key.UniqueId,
                    Amount = ammoEntry.Value
                };
                ++index;
            }
            */


            // Save documents
            saveData.Documents = new List<string>();
            foreach (var doc in Documents)
            {
                saveData.Documents.Add(doc.UniqueId);
            }

            // Save maps
            saveData.Maps = new List<string>();
            foreach (var map in Maps)
            {
                saveData.Maps.Add(map.UniqueId);
            }
            saveData.MaxItems = m_MaxItems;

            return saveData;
        }

        public void SetFromSavedData(InventorySaveData savedData)
        {
            Expanded = false;
            m_MaxItems = savedData.MaxItems;
            if (m_MaxItems == 0) 
                m_MaxItems = k_DefaultSize;

            Clear();

            // Load items
            for (int i = 0; i < Items.Length; ++i)
            {
                if (i < savedData.Items.Count && !string.IsNullOrEmpty(savedData.Items[i].ItemId))
                {
                    Items[i].Item = GameManager.Instance.ItemDatabase.GetRegister(savedData.Items[i].ItemId);
                    Items[i].Count = savedData.Items[i].Count;
                    Items[i].SecondaryCount = savedData.Items[i].SecondaryCount;
                    Items[i].Status = savedData.Items[i].Status;
                }
                else
                {
                    Items[i].Item = null;
                    Items[i].Count = 0;
                    Items[i].SecondaryCount = 0;
                    Items[i].Status = 0;
                }
            }

            // Load equipped items
            List<InventoryEntry> equipped = new List<InventoryEntry>();
            foreach (var savedEquipment in savedData.EquippedItems)
            {
                if (savedEquipment.InventoryIndex >= 0)
                {
                    equipped.Add(Items[savedEquipment.InventoryIndex]);
                }
                else
                {
                    equipped.Add(new InventoryEntry()
                    {
                        Item = GameManager.Instance.ItemDatabase.GetRegister(savedEquipment.OptionalData.ItemId),
                        Count = savedEquipment.OptionalData.Count,
                        SecondaryCount = savedEquipment.OptionalData.SecondaryCount,
                        Status = savedEquipment.OptionalData.Status,
                    });
                }
            }

            // Load Documents
            foreach (var docId in savedData.Documents)
            {
                Documents.Add(GameManager.Instance.DocumentDatabase.GetRegister(docId));
            }

            // Load Documents
            foreach (var mapId in savedData.Maps)
            {
                Maps.Add(GameManager.Instance.MapDatabase.GetRegister(mapId));
            }

            foreach (var e in equipped)
            {
                Equip(e);
            }
        }
    }

}
