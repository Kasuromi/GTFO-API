using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GTFO.API.Attributes;
using GTFO.API.Components;
using GTFO.API.Resources;
using LevelGeneration;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace GTFO.API
{
    [API("Prefab")]
    public static class PrefabAPI
    {
        /// <summary>
        /// Status info for the <see cref="PrefabAPI"/>
        /// </summary>
        public static ApiStatusInfo Status => APIStatus.Prefab;

        /// <summary>
        /// Creates a consumable from the provided asset and applies the necessary shaders and components
        /// </summary>
        /// <param name="assetName">The asset to create a consumable from</param>
        /// <param name="enableEmissive">If the consumable GFX should be emissive</param>
        /// <exception cref="ArgumentException">The asset isn't loaded</exception>
        public static void CreateConsumable(string assetName, bool enableEmissive = false)
        {
            GameObject consumableAsset = AssetAPI.GetLoadedAsset(assetName)?.TryCast<GameObject>() ?? null;
            if (consumableAsset == null) throw new ArgumentException($"Couldn't find a game object asset with the name {assetName}", nameof(assetName));

            ItemEquippable equippableComp = consumableAsset.AddComponent<ItemEquippable>();
            equippableComp.m_isFirstPerson = false;
            equippableComp.m_itemModelHolder = consumableAsset.transform;

            ReplaceShaderInAssetMaterials(consumableAsset, CustomGearShader, enableEmissive ? "ENABLE_EMISSIVE" : null);
        }

        /// <summary>
        /// Creates a consumable pickup and applies the necessary shaders and components
        /// </summary>
        /// <param name="assetName">The asset to create a consumable pickup from</param>
        /// <param name="enableEmissive">If the consumable pickup GFX should be emissive</param>
        /// <exception cref="ArgumentException">The asset isn't loaded</exception>
        /// <exception cref="Exception">The asset doesn't contain a box collider for interaction</exception>
        public static void CreateConsumablePickup(string assetName, bool enableEmissive = false)
        {
            GameObject pickupAsset = AssetAPI.GetLoadedAsset(assetName)?.TryCast<GameObject>() ?? null;
            if (pickupAsset == null) throw new ArgumentException($"Couldn't find a game object asset with the name {assetName}", nameof(assetName));

            BoxCollider interactionCollider = pickupAsset.GetComponentInChildren<BoxCollider>();
            if (interactionCollider == null) throw new Exception($"The Consumable Pickup prefab doesn't contain a BoxCollider for interaction");

            GameObject interactionObject = interactionCollider.gameObject;
            interactionObject.layer = LayerMask.NameToLayer("Interaction");
            Interact_Pickup_PickupItem interactionComp = interactionObject.AddComponent<Interact_Pickup_PickupItem>();
            interactionComp.m_colliderToOwn = interactionCollider;

            ConsumablePickup_Core pickupComp = pickupAsset.AddComponent<ConsumablePickup_Core>();
            pickupComp.m_syncComp = pickupAsset.AddComponent<LG_PickupItem_Sync>();
            pickupComp.m_interactComp = interactionComp;

            ReplaceShaderInAssetMaterials(pickupAsset, CustomGearShader, enableEmissive ? "ENABLE_EMISSIVE" : null);
        }

        /// <summary>
        /// Creates a consumble instance and applies the necessary shaders and components
        /// </summary>
        /// <typeparam name="T">A <see cref="ConsumableInstance"/> script to be attached to the asset for customized behavior</typeparam>
        /// <param name="assetName">The asset to create a consumable instance from</param>
        /// <exception cref="ArgumentException">The asset isn't loaded</exception>
        /// <exception cref="Exception">The asset doesn't contain a rigidbody</exception>
        public static void CreateConsumableInstance<T>(string assetName) where T : ConsumableInstance
        {
            GameObject instanceAsset = AssetAPI.GetLoadedAsset(assetName)?.TryCast<GameObject>() ?? null;
            if (instanceAsset == null) throw new ArgumentException($"Couldn't find a game object asset with the name {assetName}", nameof(assetName));

            if(!ClassInjector.IsTypeRegisteredInIl2Cpp<T>())
                ClassInjector.RegisterTypeInIl2Cpp<T>();

            instanceAsset.layer = LayerMask.NameToLayer("Debris");

            Rigidbody rigidbody = instanceAsset.GetComponent<Rigidbody>();
            ColliderMaterial colliderMatComp = instanceAsset.AddComponent<ColliderMaterial>();
            colliderMatComp.PhysicsBody = rigidbody ?? throw new Exception($"The Consumable Instance prefab doesn't contain a Rigidbody");

            instanceAsset.AddComponent<T>();
        }

        /// <summary>
        /// Creates a gear component and applies necessary shaders and components
        /// </summary>
        /// <param name="assetName">The asset to create a gear component from</param>
        /// <param name="enableEmissive">If the gear component GFX should be emissive</param>
        /// <exception cref="ArgumentException">The asset isn't loaded</exception>
        public static void CreateGearComponent(string assetName, bool enableEmissive = false)
        {
            GameObject gearComponent = AssetAPI.GetLoadedAsset(assetName)?.TryCast<GameObject>() ?? null;
            if (gearComponent == null) throw new ArgumentException($"Couldnt find a game object asset with the name {assetName}", nameof(assetName));

            gearComponent.layer = LayerMask.NameToLayer("FirstPersonItem");

            ReplaceShaderInAssetMaterials(gearComponent, CustomGearShader, "ENABLE_FPS_RENDERING", enableEmissive ? "ENABLE_EMISSIVE" : null);
        }
        
        /// <summary>
        /// Attaches an OnUse action to a syringe by persistent id
        /// </summary>
        /// <param name="itemPersistentId">The persistent ID of the syringe from the <see cref="ItemDataBlock"/></param>
        /// <param name="onUse">The delegate that will be called when the syringe is used</param>
        /// <exception cref="ArgumentException">This persistent ID already has a registered use action</exception>
        public static void CreateSyringe(uint itemPersistentId, Action<SyringeFirstPerson> onUse)
        {
            if (s_SyringeActions.ContainsKey(itemPersistentId))
                throw new ArgumentException($"{itemPersistentId} is already registered with a syringe action.");

            s_SyringeActions.Add(itemPersistentId, onUse);
        }

        internal static bool OnSyringeUsed(SyringeFirstPerson syringe)
        {
            if (s_SyringeActions.TryGetValue(syringe.ItemDataBlock.persistentID, out var handler)) {
                handler.Invoke(syringe);
                return true;
            }
            return false;
        }

        private static void ReplaceShaderInAssetMaterials(GameObject asset, Shader newShader, params string[] addedKeywords)
        {
            addedKeywords = addedKeywords.Where((x) => !string.IsNullOrEmpty(x)).ToArray();

            foreach (var meshRenderer in asset.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (var material in meshRenderer.materials)
                {
                    material.shader = newShader;
                    if (addedKeywords.Length > 0)
                    {
                        string[] keywords = material.shaderKeywords;
                        int originalSize = keywords.Length;
                        Array.Resize(ref keywords, keywords.Length + addedKeywords.Length);

                        for(int i = addedKeywords.Length; i < addedKeywords.Length; i++)
                        {
                            keywords[originalSize + i] = addedKeywords[i];
                        }
                        material.shaderKeywords = keywords;
                    }
                }
            }
        }

        private static Shader CustomGearShader
        {
            get
            {
                if (s_CustomGearShader == null) s_CustomGearShader = Shader.Find(ShaderConstants.CUSTOM_GEAR_SHADER);
                return s_CustomGearShader;
            }
        }
        private static Shader s_CustomGearShader;
        private static readonly Dictionary<uint, Action<SyringeFirstPerson>> s_SyringeActions = new();
    }
}
