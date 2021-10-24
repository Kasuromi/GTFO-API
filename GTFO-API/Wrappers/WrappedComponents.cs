using System;
using AIGraph;
using GameData;
using ItemSetup;
using Player;

namespace GTFO.API.Wrappers
{
#pragma warning disable CS1591
    public class ItemWrapped : Item
    {
        public ItemWrapped(IntPtr hdl) : base(hdl) { }

        public override unsafe pItemData Get_pItemData()
        {
            pItemData* data = stackalloc pItemData[1];
            return *Get_pItemDataBase(data, Pointer.ToPointer());
        }

        public override unsafe void Set_pItemData(pItemData data)
            => Set_pItemDataBase(Pointer.ToPointer(), &data);

        public override unsafe pItemData_Custom GetCustomData()
            => *GetCustomDataBase(Pointer.ToPointer());

        public override unsafe void SetCustomData(pItemData_Custom custom, bool sync)
            => SetCustomDataBase(Pointer.ToPointer(), &custom, sync);

        public override unsafe void OnCustomDataUpdated(pItemData_Custom customDataCopy)
            => OnCustomDataUpdatedBase(Pointer.ToPointer(), &customDataCopy);

        public override unsafe void Awake()
            => AwakeBase(Pointer.ToPointer());

        public override unsafe void OnDespawn()
            => OnDespawnBase(Pointer.ToPointer());

        public override unsafe void Setup(ItemDataBlock data)
            => SetupBase(Pointer.ToPointer(), data.Pointer.ToPointer());

        public override unsafe void OnGearSpawnComplete()
            => OnGearSpawnCompleteBase(Pointer.ToPointer());

        public override unsafe void OnPickUp(PlayerAgent player)
            => OnPickUpBase(Pointer.ToPointer(), player.Pointer.ToPointer());

        public override unsafe void SetupBaseModel(ItemModelSetup setup)
            => SetupBaseModelBase(Pointer.ToPointer(), setup.Pointer.ToPointer());

        public override unsafe void SyncedTurnOn(PlayerAgent agent, AIG_CourseNode courseNode)
            => SyncedTurnOnBase(Pointer.ToPointer(), agent.Pointer.ToPointer(), courseNode.Pointer.ToPointer());

        public override unsafe void SyncedTurnOff(PlayerAgent agent)
            => SyncedTurnOffBase(Pointer.ToPointer(), agent.Pointer.ToPointer());

        public override unsafe void SyncedTrigger(PlayerAgent agent)
            => SyncedTriggerBase(Pointer.ToPointer(), agent.Pointer.ToPointer());

        public override unsafe void SyncedTriggerSecondary(PlayerAgent agent)
            => SyncedTriggerSecondaryBase(Pointer.ToPointer(), agent.Pointer.ToPointer());

        public override unsafe void SyncedThrow(PlayerAgent agent)
            => SyncedThrowBase(Pointer.ToPointer(), agent.Pointer.ToPointer());

        public override unsafe void SyncedPickup(PlayerAgent agent)
            => SyncedPickupBase(Pointer.ToPointer(), agent.Pointer.ToPointer());

        public override unsafe void SyncedSetKeyValue(int key, float value)
            => SyncedSetKeyValueBase(Pointer.ToPointer(), key, value);

        public override unsafe Interact_Base GetPickupInteraction()
            => new((IntPtr)GetPickupInteractionBase(Pointer.ToPointer()));

        public override unsafe Item GetItem()
            => new((IntPtr)GetItemBase(Pointer.ToPointer()));

        private static readonly Item__Get_pItemData Get_pItemDataBase = Il2CppAPI.GetIl2CppMethod<Item, Item__Get_pItemData>("Get_pItemData", "Player.pItemData");
        private static readonly Item__Set_pItemData Set_pItemDataBase = Il2CppAPI.GetIl2CppMethod<Item, Item__Set_pItemData>("Set_pItemData", "System.Void", "Player.pItemData");
        private static readonly Item__GetCustomData GetCustomDataBase = Il2CppAPI.GetIl2CppMethod<Item, Item__GetCustomData>("GetCustomData", "Player.pItemData_Custom");
        private static readonly Item__SetCustomData SetCustomDataBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SetCustomData>("SetCustomData", "System.Void", "Player.pItemData_Custom", "System.Boolean");
        private static readonly Item__OnCustomDataUpdated OnCustomDataUpdatedBase = Il2CppAPI.GetIl2CppMethod<Item, Item__OnCustomDataUpdated>("OnCustomDataUpdated", "System.Void", "Player.pItemData_Custom");
        private static readonly Item__Awake AwakeBase = Il2CppAPI.GetIl2CppMethod<Item, Item__Awake>("Awake", "System.Void");
        private static readonly Item__OnDespawn OnDespawnBase = Il2CppAPI.GetIl2CppMethod<Item, Item__OnDespawn>("OnDespawn", "System.Void");
        private static readonly Item__Setup SetupBase = Il2CppAPI.GetIl2CppMethod<Item, Item__Setup>("Setup", "System.Void", "GameData.ItemDataBlock");
        private static readonly Item__OnGearSpawnComplete OnGearSpawnCompleteBase = Il2CppAPI.GetIl2CppMethod<Item, Item__OnGearSpawnComplete>("OnGearSpawnComplete", "System.Void");
        private static readonly Item__OnPickUp OnPickUpBase = Il2CppAPI.GetIl2CppMethod<Item, Item__OnPickUp>("OnPickUp", "System.Void", "Player.PlayerAgent");
        private static readonly Item__SetupBaseModel SetupBaseModelBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SetupBaseModel>("SetupBaseModel", "System.Void", "ItemSetup.ItemModelSetup");
        private static readonly Item__SyncedTurnOn SyncedTurnOnBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SyncedTurnOn>("SyncedTurnOn", "System.Void", "Player.PlayerAgent", "AIGraph.AIG_CourseNode");
        private static readonly Item__SyncedTurnOff SyncedTurnOffBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SyncedTurnOff>("SyncedTurnOff", "System.Void", "Player.PlayerAgent");
        private static readonly Item__SyncedTrigger SyncedTriggerBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SyncedTrigger>("SyncedTrigger", "System.Void", "Player.PlayerAgent");
        private static readonly Item__SyncedTriggerSecondary SyncedTriggerSecondaryBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SyncedTriggerSecondary>("SyncedTriggerSecondary", "System.Void", "Player.PlayerAgent");
        private static readonly Item__SyncedThrow SyncedThrowBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SyncedThrow>("SyncedThrow", "System.Void", "Player.PlayerAgent");
        private static readonly Item__SyncedPickup SyncedPickupBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SyncedPickup>("SyncedPickup", "System.Void", "Player.PlayerAgent");
        private static readonly Item__SyncedSetKeyValue SyncedSetKeyValueBase = Il2CppAPI.GetIl2CppMethod<Item, Item__SyncedSetKeyValue>("SyncedSetKeyValue", "System.Void", "System.Int32", "System.Single");
        private static readonly Item__GetPickupInteraction GetPickupInteractionBase = Il2CppAPI.GetIl2CppMethod<Item, Item__GetPickupInteraction>("GetPickupInteraction", "System.Void");
        private static readonly Item__GetItem GetItemBase = Il2CppAPI.GetIl2CppMethod<Item, Item__GetItem>("GetItem", "Item");
    }
#pragma warning restore CS1591
}
