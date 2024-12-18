using DV.Logic.Job;
using HarmonyLib;
using System;

namespace ConductorBoard
{
    [HarmonyPatch]
    internal class Patches
    {
        public static Action OnTaskUpdated;
        [HarmonyPatch(typeof(DV.Logic.Job.Task), nameof(Task.SetState))]
        [HarmonyPostfix]
        public static void UpdateTaskState()
        {
            OnTaskUpdated?.Invoke();
        }

        [HarmonyPatch(typeof(JobSaveManager), nameof(JobSaveManager.LoadJobSaveGameData))]
        [HarmonyPostfix]
        public static void LoadJobSaveGameData()
        {
            OnTaskUpdated?.Invoke();
        }

        [HarmonyPatch(typeof(JobChainController), nameof(JobChainController.UpdateTrainCarPlatesOfCarsOnJob))]
        [HarmonyPostfix]
        public static void TrainCarPlatesChange()
        {
            OnTaskUpdated?.Invoke();
        }
    }
}
