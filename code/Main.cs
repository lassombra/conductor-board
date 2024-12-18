using GadgetUnity;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace ConductorBoard
{
    public class Main
    {
        private static UnityModManager.ModEntry ModEntry { get; set; }
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(modEntry.Assembly);
            custom_item_mod.CustomGadgetBaseMap.RegisterGadgetImplementation(typeof(FakeManifestGadget), typeof(ManifestGadget), ApplyCustomization);
            return true;
        }

        private static void ApplyCustomization(custom_item_components.GadgetBase sourceGadget, ref DV.Customization.Gadgets.GadgetBase targetGadget)
        {
            var manifest = targetGadget as ManifestGadget;
            var source = sourceGadget as FakeManifestGadget;
            if (manifest != null && source != null)
            {
                manifest.header = source.header;
                manifest.body = source.body;
                manifest.train = source.train;
                var itemSource = manifest.gameObject.GetComponentInParentIncludingInactive<ItemSaveMapping>();
                if (itemSource != null)
                {
                    var item = itemSource.gameObject.AddComponent<ConductorBoardData>();
                    item.header = itemSource.header;
                    item.body = itemSource.body;
                    item.train = itemSource.train;
                    Object.Destroy(itemSource);
                }
            }
        }

        public static void Log(string message)
        {
            ModEntry.Logger.Log(message);
        }

        public static void Error(string message)
        {
            ModEntry.Logger.Error(message);
        }
    }
}
