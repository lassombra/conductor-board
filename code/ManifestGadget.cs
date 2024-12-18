using DV.Customization.Gadgets;
using DV.Logic.Job;
using DV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace ConductorBoard
{
    public class ManifestGadget : GadgetBase
    {
        public TextMeshPro header;
        public TextMeshPro body;
        public TextMeshPro train;
        private ConductorBoardData data;

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
            var trainText = $"============================\nMass: {Math.Ceiling(mass / 1000)}t | Length: {Math.Ceiling(length)}m";
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
        }
        private string DestinationTrack(TrainCar car)
        {
            var job = SingletonBehaviour<JobsManager>.Instance.GetJobOfCar(car);
            if (job == null)
            {
                return "No Dest";
            }
            return GetNextTrack(job);
        }
        private string GetNextTrack(Job job)
        {
            foreach (var task in job.tasks)
            {
                if (!task.IsTaskCompleted())
                {
                    return GetNextTrack(task);
                }
            }
            return "No Dest";
        }
        private String GetNextTrack(DV.Logic.Job.Task task)
        {
            switch (task.InstanceTaskType)
            {
                case TaskType.Sequential:
                    return GetNextTrack(((SequentialTasks)task).currentTask.Value);
                case TaskType.Parallel:
                    return GetNextTrack(((ParallelTasks)task).tasks);
                default:
                    return task.GetTaskData().destinationTrack.ID.FullDisplayID;
            }
        }
        private String GetNextTrack(List<DV.Logic.Job.Task> tasks)
        {
            foreach (var task in tasks)
            {
                if (!task.IsTaskCompleted())
                {
                    return GetNextTrack(task);
                }
            }
            return "No Dest";
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
