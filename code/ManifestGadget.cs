using DV.Customization.Gadgets;
using DV.Logic.Job;
using DV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Jobs;

namespace ConductorBoard
{
    public class ManifestGadget : GadgetBase
    {
        public TextMeshPro header;
        public TextMeshPro body;
        public TextMeshPro train;
        private ConductorBoardData data;
        private Dictionary<Job, List<Task>> taskMap = new ();

        protected override void OnItemAssigned()
        {
            base.OnItemAssigned();
            data = GadgetItem.gameObject.GetComponent<ConductorBoardData>();
        }
        protected override void OnAfterLinked()
        {
            base.OnAfterLinked();
            if (base.IsOnTrainCar)
            {
                base.TrainCar.TrainsetChanged += OnTrainsetChanged;
                UpdateText();
            }
            Patches.OnTaskUpdated += OnTaskUpdated;
        }

        private void OnTaskUpdated()
        {
            UpdateText();
        }

        protected override void OnBeforeUnlinked()
        {
            if (base.IsOnTrainCar)
            {
                base.TrainCar.TrainsetChanged -= OnTrainsetChanged;
            }
            Patches.OnTaskUpdated -= OnTaskUpdated;
        }
        public void OnTrainsetChanged(Trainset trainset)
        {
            UpdateText();
        }
        private void UpdateText()
        {
            if (base.TrainCar == null)
            {
                WriteText("", "", "");
            }
            var carId = base.TrainCar.logicCar.ID;
            var cars = WalkTrain();
            var headerText = $"{carId}\n=================";
            var bodyText = "║  Track  | Cars | Mass | Length ║";
            var length = 0f;
            var mass = 0f;
            foreach (var destination in cars)
            {
                var track = destination.track.PadLeft(7);
                var displayLength = $"{Math.Ceiling(destination.length)}m".PadLeft(6);
                var displayCount = $"{destination.count}".PadLeft(4);
                var displayMass = $"{Math.Ceiling(destination.mass / 1000)}t".PadLeft(4);
                bodyText += $"\n║ {track} | {displayCount} | {displayMass} | {displayLength} ║";
                length += destination.length;
                mass += destination.mass;
            }
            var trainText = $"============================\nMass: {System.Math.Ceiling(mass / 1000)}t | Length: {System.Math.Ceiling(length)}m";
            WriteText(headerText, bodyText, trainText);
        }
        private void WriteText(string header, string body, string train)
        {
            this.header.text = header;
            data.Header = header;
            this.body.text = body;
            data.Body = body;
            this.train.text = train;
            data.Train = train;
        }
        private List<DestinationList> WalkTrain()
        {
            if (null == base.TrainCar)
            {
                return new List<DestinationList>();
            }
            return WalkTrainIterable().ToList();
        }

        private IEnumerable<DestinationList> WalkTrainIterable()
        {
            var currentCar = base.TrainCar.frontCoupler.coupledTo?.train;
            var previousCar = base.TrainCar;
            while (currentCar != null)
            {
                if (currentCar.frontCoupler.coupledTo?.train != previousCar)
                {
                    previousCar = currentCar;
                    currentCar = currentCar.frontCoupler.coupledTo?.train;
                } else
                {
                    previousCar = currentCar;
                    currentCar = currentCar.rearCoupler.coupledTo?.train;
                }
            }
            currentCar = previousCar;
            previousCar = null;
            while (currentCar != null)
            {
                var destination = new DestinationList();
                string track = DestinationTrack(currentCar);
                destination.track = track;
                while (currentCar != null && destination.track.Equals(track))
                {
                    destination.count++;
                    destination.length += currentCar.logicCar.length;
                    destination.mass += currentCar.massController.TotalMass;
                    if (currentCar.frontCoupler.coupledTo?.train == previousCar)
                    {
                        previousCar = currentCar;
                        currentCar = currentCar.rearCoupler.coupledTo?.train;
                    } else
                    {
                        previousCar = currentCar;
                        currentCar = currentCar.frontCoupler.coupledTo?.train;
                    }
                    if (currentCar != null)
                    {
                        track = DestinationTrack(currentCar);
                    }
                }
                yield return destination;
            }
            taskMap.Clear();
        }
        private string DestinationTrack(TrainCar car)
        {
            var job = SingletonBehaviour<JobsManager>.Instance.GetJobOfCar(car);
            if (job == null)
            {
                return "-----";
            }
            return GetNextTrack(job, car);
        }
        private string GetNextTrack(Job job, TrainCar car)
        {
            var tasks = new List<Task>();
            if (taskMap.ContainsKey(job))
            {
                tasks = taskMap[job];
            } else
            {
                tasks = GetTracks(job).ToList();
                taskMap[job] = tasks;
            }
            foreach (var task in tasks)
            {
                if (!task.IsTaskCompleted() && IsTaskForCar(task, car))
                {
                    return TrackForTask(task);
                }
            }
            return "-----";
        }

        private string TrackForTask(Task task)
        {
            switch (task.InstanceTaskType)
            {
                case TaskType.Warehouse:
                    return ((WarehouseTask)task).warehouseMachine.WarehouseTrack.ID.FullDisplayID;
                case TaskType.Transport:
                    return ((TransportTask)task).destinationTrack.ID.FullDisplayID;
            }
            return "-----";
        }

        private bool IsTaskForCar(Task task, TrainCar car)
        {
            switch (task.InstanceTaskType) {
                case TaskType.Warehouse:
                    return ((WarehouseTask)task).cars.Contains(car.logicCar);
                case TaskType.Transport:
                    return ((TransportTask)task).cars.Contains(car.logicCar);
            }
            return false;
        }
        private IEnumerable<Task> GetTracks(Job job)
        {
            return GetSubTasks(job.tasks);
        }
        private IEnumerable<Task> GetSubTasks(List<Task> taskList)
        {
            foreach (var task in taskList)
            {
                switch (task.InstanceTaskType)
                {
                    case TaskType.Warehouse:
                    case TaskType.Transport:
                        yield return task;
                        break;
                    case TaskType.Parallel:
                        foreach (var subTask in GetSubTasks((ParallelTasks)task))
                        {
                            yield return subTask;
                        }
                        break;
                    case TaskType.Sequential:
                        foreach (var subTask in GetSubTasks((SequentialTasks)task))
                        {
                            yield return subTask;
                        }
                        break;
                }
            }
        }
        private IEnumerable<Task> GetSubTasks(ParallelTasks task)
        {
            return GetSubTasks(task.tasks);
        }
        private IEnumerable<Task> GetSubTasks(SequentialTasks task)
        {
            return GetSubTasks(task.tasks.ToList());
        }

        private struct DestinationList
        {
            public string track;
            public int count;
            public float length;
            public float mass;
        }
    }
}
