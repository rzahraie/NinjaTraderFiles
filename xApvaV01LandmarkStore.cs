using System;
using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01LandmarkStore
    {
        private readonly List<ApvaLandmark> landmarks = new List<ApvaLandmark>();

        public IReadOnlyList<ApvaLandmark> ActiveLandmarks
        {
            get { return landmarks.Where(x => x.IsActive).ToList(); }
        }

        public IReadOnlyList<ApvaLandmark> AllLandmarks
        {
            get { return landmarks.ToList(); }
        }

        public void Add(ApvaLandmark landmark)
        {
            if (landmark == null)
                throw new ArgumentNullException(nameof(landmark));

            landmarks.Add(landmark);
        }

        public void AgeActiveLandmarks()
        {
            foreach (var landmark in landmarks)
            {
                if (landmark.IsActive)
                    landmark.Age++;
            }
        }

        public void Confirm(Guid landmarkId)
        {
            var landmark = FindById(landmarkId);
            if (landmark == null)
                return;

            landmark.IsConfirmed = true;
        }

        public void Invalidate(Guid landmarkId)
        {
            var landmark = FindById(landmarkId);
            if (landmark == null)
                return;

            landmark.IsInvalidated = true;
            landmark.IsActive = false;
        }

        public void Deactivate(Guid landmarkId)
        {
            var landmark = FindById(landmarkId);
            if (landmark == null)
                return;

            landmark.IsActive = false;
        }

        public ApvaLandmark FindById(Guid id)
        {
            return landmarks.FirstOrDefault(x => x.LandmarkId == id);
        }

        public ApvaLandmark GetLastActive(ApvaLandmarkType type)
        {
            return landmarks
                .Where(x => x.IsActive && x.Type == type)
                .OrderByDescending(x => x.BarIndex)
                .FirstOrDefault();
        }

        public ApvaLandmark GetLastActivePeak(ApvaDirection direction)
        {
            return landmarks
                .Where(x =>
                    x.IsActive &&
                    x.Type == ApvaLandmarkType.PeakVolume &&
                    x.Direction == direction)
                .OrderByDescending(x => x.BarIndex)
                .FirstOrDefault();
        }

        public ApvaLandmark GetHighestVolumeActive(ApvaDirection direction)
        {
            return landmarks
                .Where(x =>
                    x.IsActive &&
                    x.Direction == direction)
                .OrderByDescending(x => x.Volume)
                .FirstOrDefault();
        }

        public ApvaLandmark GetHighestVolumeActive(ApvaLandmarkType type, ApvaDirection direction)
        {
            return landmarks
                .Where(x =>
                    x.IsActive &&
                    x.Type == type &&
                    x.Direction == direction)
                .OrderByDescending(x => x.Volume)
                .FirstOrDefault();
        }

        public IEnumerable<ApvaLandmark> GetActiveByType(ApvaLandmarkType type)
        {
            return landmarks.Where(x => x.IsActive && x.Type == type);
        }

        public void RemoveInactiveOlderThan(int maxAge)
        {
            landmarks.RemoveAll(x => !x.IsActive && x.Age > maxAge);
        }

        public void Clear()
        {
            landmarks.Clear();
        }
    }
}