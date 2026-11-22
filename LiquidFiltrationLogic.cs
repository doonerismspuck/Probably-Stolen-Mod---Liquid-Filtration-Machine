using System;
using UnityEngine;

namespace Liquid_Filtration_Machine
{
    /// <summary>
    /// Filters liquid from one container into another using a filter in a separate slot.
    /// Simulates each batch first; no item is changed by a rejected batch.
    /// All volumes are in the game's internal units (1 mL = 1000 units).
    /// </summary>
    public static class LiquidFiltrationLogic
    {
        private const int PreferredBatch = 1000 * 1000; // 1 L
        private const int MinimumBatch = 1000;          // 1 mL, except final remainder
        private const int NativeIterationLimit = 10000;

        private sealed class BatchResult
        {
            public int[] Taken;
            public int[] Produced;
            public int InputVolume;
            public int OutputVolume;
            public int DurabilityUsed;
            public bool Complete;
        }

        public static int FilterLiquid(GameItem input, GameItem output, GameItem filter)
        {
            if (input == null || output == null || filter == null ||
                ReferenceEquals(input, output) ||
                !input.IsTag("LIQUID_CONTAINER_TAG") ||
                !output.IsTag("LIQUID_CONTAINER_TAG") ||
                !filter.IsTag("WATER_FILTER") ||
                filter.identifier == "uv_filter")
            {
                Debug.Log("[Liquid Filtration] Missing or invalid input, output, or filter.");
                return 0;
            }

            int processedTotal = 0;
            int filterSize = filter.GetTagReadonly("FILTER_SIZE_TAG").GetInt();

            while (true)
            {
                int durability = DurabilityHelper.GetRealDurability(filter);
                if (durability <= 0)
                    break;

                int[] sourceParts = WaterHelper.GetLiquidParts(input);
                int[] targetParts = WaterHelper.GetLiquidParts(output);
                int sourceVolume = SumParts(sourceParts);
                int targetVolume = SumParts(targetParts);
                int outputCapacity = WaterHelper.GetTotalCapacity(output);

                if (sourceVolume <= 0)
                    break;

                int freeCapacity = Math.Max(0, outputCapacity - targetVolume);
                int proposed = Math.Min(PreferredBatch, sourceVolume);
                int minimum = Math.Min(MinimumBatch, sourceVolume);
                BatchResult accepted = null;

                while (proposed >= minimum)
                {
                    int[] taken = WaterHelper.SplitProportionally(sourceParts, proposed);
                    BatchResult trial = Simulate(taken, durability, filterSize);

                    if (trial.Complete && trial.InputVolume > 0 &&
                        trial.OutputVolume <= freeCapacity)
                    {
                        accepted = trial;
                        break;
                    }

                    if (proposed == minimum)
                        break;

                    proposed = Math.Max(minimum, proposed / 2);
                }

                if (accepted == null)
                    break;

                bool outputWasEmpty = targetVolume == 0;

                if (accepted.OutputVolume > 0)
                {
                    ChemicalHelper.HandleReceiveLiquidTemp(
                        output, accepted.OutputVolume, input);
                }

                for (int i = 0; i < accepted.Taken.Length; i++)
                {
                    Liquid liquid = Liquid.Liquids[i];
                    int taken = accepted.Taken[i];
                    int produced = accepted.Produced[i];

                    if (taken > 0)
                    {
                        int newSourceAmount = sourceParts[i] - taken;
                        input.ModifyTag(liquid.currentPart,
                            state => state.Enable().SetInt(newSourceAmount),
                            skipValidate: true);
                    }

                    if (produced > 0)
                    {
                        int newTargetAmount = targetParts[i] + produced;
                        output.ModifyTag(liquid.currentPart,
                            state => state.Enable().SetInt(newTargetAmount),
                            skipValidate: true);
                    }
                }

                if (accepted.DurabilityUsed > 0)
                    DurabilityHelper.ChangeDurability(filter, -accepted.DurabilityUsed);

                if (accepted.OutputVolume > 0)
                    WaterFeatureHelper.OnLiquidContainerPourInto(output, input, outputWasEmpty);

                WaterFeatureHelper.OnLiquidContainerPourOutOf(input);
                processedTotal += accepted.InputVolume;

                if (DurabilityHelper.GetRealDurability(filter) <= 0)
                    break;
            }

            if (processedTotal > 0)
            {
                WaterHelper.UpdateWaterBottleSound(input);
                WaterHelper.UpdateWaterBottleSound(output);
                Debug.Log("[Liquid Filtration] Processed " +
                          (processedTotal / 1000.0).ToString("0.###") + " mL.");
            }

            return processedTotal;
        }

        private static BatchResult Simulate(int[] taken, int durability, int filterSize)
        {
            BatchResult result = new BatchResult
            {
                Taken = taken,
                Produced = new int[taken.Length],
                Complete = true
            };

            long remainingDurability = durability;
            long usedDurability = 0;

            for (int i = 0; i < taken.Length; i++)
            {
                int amount = taken[i];
                if (amount <= 0)
                    continue;

                Liquid liquid = Liquid.Liquids[i];
                result.InputVolume += amount;
                int produced = amount;

                bool filterable = liquid.identifier != "microbe" &&
                                  filterSize <= liquid.particleSize;

                if (filterable)
                {
                    int minimumRemaining = (int)((double)amount * liquid.filterMinimumPurity);
                    minimumRemaining = Math.Max(0, Math.Min(amount, minimumRemaining));

                    if (amount > minimumRemaining)
                    {
                        int removalPerOperation = liquid.filterVolumeRemoved;
                        int durabilityPerOperation = liquid.filterDurabilityUsage;

                        if (removalPerOperation <= 0 || durabilityPerOperation < 0)
                        {
                            result.Complete = false;
                            return result;
                        }

                        long requiredOperations =
                            ((long)amount - minimumRemaining + removalPerOperation - 1L)
                            / removalPerOperation;

                        if (requiredOperations > NativeIterationLimit)
                        {
                            result.Complete = false;
                            return result;
                        }

                        if (durabilityPerOperation > 0)
                        {
                            long availableOperations = remainingDurability > 0
                                ? (remainingDurability + durabilityPerOperation - 1L)
                                  / durabilityPerOperation
                                : 0L;

                            if (requiredOperations > availableOperations)
                            {
                                result.Complete = false;
                                return result;
                            }

                            long cost = requiredOperations * durabilityPerOperation;
                            long charged = Math.Min(remainingDurability, cost);
                            remainingDurability -= charged;
                            usedDurability += charged;
                        }
                        else if (remainingDurability <= 0)
                        {
                            result.Complete = false;
                            return result;
                        }

                        long afterRemoval = (long)amount -
                                            requiredOperations * removalPerOperation;
                        produced = (int)Math.Max(minimumRemaining,
                                                 Math.Min((long)amount, afterRemoval));
                    }
                }

                result.Produced[i] = produced;
                result.OutputVolume += produced;
            }

            result.DurabilityUsed = (int)usedDurability;
            return result;
        }

        private static int SumParts(int[] parts)
        {
            long total = 0;
            for (int i = 0; i < parts.Length; i++)
                total += Math.Max(0, parts[i]);
            return (int)Math.Min(int.MaxValue, total);
        }
    }
}
