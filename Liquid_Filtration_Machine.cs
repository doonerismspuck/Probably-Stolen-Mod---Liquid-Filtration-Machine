
using UnityEngine;

namespace Liquid_Filtration_Machine
{
    public static class LiquidFiltrationMachine
    {
        public const string ItemId = "liquid_filter_machine";

        public static GameItem CreateMachine()
        {

            GameSlotInventory inputSlot =
                new GameSlotInventory();

            GameSlotInventory filterSlot =
                new GameSlotInventory();

            GameSlotInventory outputSlot =
                new GameSlotInventory();

            inputSlot.slotPriorityFunc =
                (GameItem item, GameInventory inv,
                 SlotMarker marker) => 2;

            filterSlot.slotPriorityFunc =
                (GameItem item, GameInventory inv,
                 SlotMarker marker) => 2;

            outputSlot.slotPriorityFunc =
                (GameItem item, GameInventory inv,
                 SlotMarker marker) => 2;

            MachineryButton filterButton =
                new MachineryButton("Filter Liquid");

            PixelWindow window =
                MachineHelper.CreateMachineryWindow(
                    true,

                    MachineHelper.Panel(
                        "INPUT",
                        inputSlot,
                        "input"
                    ),

                    MachineHelper.Panel(
                        "FILTER",
                        filterSlot,
                        "filter"
                    ),

                    MachineHelper.Panel(
                        "OUTPUT",
                        outputSlot,
                        "output"
                    ),

                    MachineHelper.Panel(
                        "CONTROL",
                        filterButton,
                        "control"
                    )
                );

            GameItem machine =
                ItemDirectory.CreateEmptyItem()
                    .SetContentWindow(window)
                    .SetSpriteAndShape(
                        "Items/items_aquaculture",
                        "ras_empty"
                    );

            machine.name = "Liquid Filtration Station";

            machine.shortDescription =
                "Transfers liquids through a replaceable filter.";

            machine.unitValue = 350L;

            machine.EnableTag("CONTAINER_TAG");
            machine.EnableTag("STANDARD_MACHINE_TAG");
            machine.SetGameItemType("MACHINE");

            MachineryHelper.InitMachinery(machine, 0);

            inputSlot.mayInventoryAddItemFunc =
                (GameItem item, GameInventory inv) =>
                    GeneralHelper.IsItemOwned(item)
                    && item.IsTag("LIQUID_CONTAINER_TAG");

            inputSlot.SetBackgroundFadeSprite(
                "Items/water_container4",
                "xl"
            );

            outputSlot.mayInventoryAddItemFunc =
                (GameItem item, GameInventory inv) =>
                    GeneralHelper.IsItemOwned(item)
                    && item.IsTag("LIQUID_CONTAINER_TAG");

            outputSlot.SetBackgroundFadeSprite(
                "Items/water_container4",
                "xl"
            );

            filterSlot.mayInventoryAddItemFunc =
                (GameItem item, GameInventory inv) =>
                    GeneralHelper.IsItemOwned(item)
                    && item.IsTag("WATER_FILTER")
                    && item.identifier != "uv_filter";

            filterSlot.SetBackgroundFadeSprite(
                "Items/items_amenities",
                "water_filter"
            );

            filterButton.SetCallback(() =>
            {
                GameItem input = inputSlot.childItem;
                GameItem output = outputSlot.childItem;
                GameItem filter = filterSlot.childItem;

                if (input == null)
                {
                    Debug.Log(
                        "[LiquidFiltration] Input container missing."
                    );
                    return;
                }

                if (output == null)
                {
                    Debug.Log(
                        "[LiquidFiltration] Output container missing."
                    );
                    return;
                }

                if (filter == null)
                {
                    Debug.Log(
                        "[LiquidFiltration] Filter missing."
                    );
                    return;
                }

                Debug.Log(
                    "[LiquidFiltration] All slots valid. " +
                    "Processing liquid."
                );
                LiquidFiltrationLogic.FilterLiquid(
                    inputSlot.childItem,
                    outputSlot.childItem,
                    filterSlot.childItem
                );                

            });

            return machine;
        }
    }
}
