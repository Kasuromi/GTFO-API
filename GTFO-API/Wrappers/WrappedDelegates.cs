using Player;

namespace GTFO.API.Wrappers
{
#pragma warning disable CS1591
    public unsafe delegate pItemData* Item__Get_pItemData(pItemData* retstr, void* _this);
    public unsafe delegate void Item__Set_pItemData(void* _this, void* data);
    public unsafe delegate pItemData_Custom* Item__GetCustomData(void* _this);
    public unsafe delegate void Item__SetCustomData(void* _this, pItemData_Custom* custom, bool sync);
    public unsafe delegate void Item__OnCustomDataUpdated(void* _this, pItemData_Custom* customDataCopy);
    public unsafe delegate void Item__Awake(void* _this);
    public unsafe delegate void Item__OnDespawn(void* _this);
    public unsafe delegate void Item__Setup(void* _this, void* data);
    public unsafe delegate void Item__OnGearSpawnComplete(void* _this);
    public unsafe delegate void Item__OnPickUp(void* _this, void* player);
    public unsafe delegate void Item__SetupBaseModel(void* _this, void* setup);
    public unsafe delegate void Item__SyncedTurnOn(void* _this, void* agent, void* courseNode);
    public unsafe delegate void Item__SyncedTurnOff(void* _this, void* agent);
    public unsafe delegate void Item__SyncedTrigger(void* _this, void* agent);
    public unsafe delegate void Item__SyncedTriggerSecondary(void* _this, void* agent);
    public unsafe delegate void Item__SyncedThrow(void* _this, void* agent);
    public unsafe delegate void Item__SyncedPickup(void* _this, void* agent);
    public unsafe delegate void Item__SyncedSetKeyValue(void* _this, int key, float value);
    public unsafe delegate void* Item__GetPickupInteraction(void* _this);
    public unsafe delegate void* Item__GetItem(void* _this);
#pragma warning restore CS1591
}
