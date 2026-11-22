
using System;
using System.Collections.Generic;

namespace Liquid_Filtration_Machine
{
    public static class LiquidFiltrationMerchants
    {
        private static bool isEnabled = false;

        public static void Register()
        {
            if (isEnabled)
                return;

            isEnabled = true;

            ModHook.OnGenerateCustomerVeryLate += OnGenerateCustomer;
            ModHook.OnPlaceInventorInventoryItemLate += OnJacksonStock;
        }

        public static void Unregister()
        {
            if (!isEnabled)
                return;

            isEnabled = false;

            ModHook.OnGenerateCustomerVeryLate -= OnGenerateCustomer;
            ModHook.OnPlaceInventorInventoryItemLate -= OnJacksonStock;
        }

        private static bool PlayerOwnsMachine()
        {
            return PlayerStore.Instance.IsPlayerOwnThisItem(
                LiquidFiltrationMachine.ItemId
            );
        }

        private static void OfferMachine()
        {
            if (!isEnabled || PlayerOwnsMachine())
                return;

            PlayerStore.Instance.AddDirectSellingItemToTable(
                LiquidFiltrationMachine.ItemId
            );
        }

        private static void OnGenerateCustomer(
            StoreClientManager manager)
        {
            foreach (StoreClient client in manager.clientStack)
            {
                if (client.identifier != "retired_water_merchant" &&
                    client.identifier != "retired_chemist")
                {
                    continue;
                }

                Action originalAction = client.mainDialogue.endAction;

                client.mainDialogue.SetEndAction(() =>
                {
                    originalAction?.Invoke();
                    OfferMachine();
                });
            }
        }

        private static void OnJacksonStock(
            List<GameItem> ownedItems)
        {
            OfferMachine();
        }
    }
}
