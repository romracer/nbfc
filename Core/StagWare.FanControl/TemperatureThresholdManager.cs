using StagWare.FanControl.Configurations;
using System.Collections.Generic;

namespace StagWare.FanControl
{
    internal class TemperatureThresholdManager
    {
        #region Private Fields

        private LinkedList<TemperatureThreshold> thresholds;
        private LinkedListNode<TemperatureThreshold> current;

        #endregion

        #region Properties

        public TemperatureThreshold SelectedThreshold
        {
            get
            {
                return this.current == null ? null : this.current.Value;
            }
        }

        #endregion

        #region Constructors

        public TemperatureThresholdManager(IEnumerable<TemperatureThreshold> thresholds)
        {
            this.thresholds = new LinkedList<TemperatureThreshold>();

            foreach (TemperatureThreshold t in thresholds)
            {
                LinkedListInsertSorted(t);
            }
        }

        #endregion

        #region Public Methods

        /*
        public void ResetCurrentThreshold(double cpuTemperature)
        {
            // Linked list must be ordered by ascending
            var node = thresholds.Last;

            while (node != null)
            {
                if ((current == null) || (node.Value.UpThreshold >= cpuTemperature))
                {
                    current = node;
                }
                else
                {
                    break;
                }

                node = node.Previous;
            }
        }
        */

        public TemperatureThreshold AutoSelectThreshold(double cpuTemperature, double gpuTemperature)
        {
            if (this.current == null)
            {
                this.current = thresholds.First;
            }

            if (cpuTemperature <= this.current.Value.CpuDownThreshold && gpuTemperature <= this.current.Value.GpuDownThreshold)
            {
                if (this.current.Previous != null)
                {
                    this.current = this.current.Previous;
                }
            }
            else if ((this.current.Next != null)
                && (cpuTemperature >= this.current.Next.Value.CpuUpThreshold || gpuTemperature >= this.current.Next.Value.GpuUpThreshold))
            {
                this.current = this.current.Next;
            }

            return this.current == null ? null : this.current.Value;
        }        

        #endregion

        #region Private Methods

        private void LinkedListInsertSorted(TemperatureThreshold item)
        {
            var node = thresholds.First;

            while (node != null)
            {
                //we've checked the consistency in xxxx.cs
                if ((item.CpuUpThreshold + item.GpuUpThreshold) <= (node.Value.CpuUpThreshold + node.Value.GpuUpThreshold))
                {
                    thresholds.AddBefore(node, item);
                    return;
                }

                node = node.Next;
            }

            thresholds.AddLast(item);
        }

        #endregion
    }
}
