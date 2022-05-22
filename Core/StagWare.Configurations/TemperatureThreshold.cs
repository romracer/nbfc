using System;

namespace StagWare.FanControl.Configurations
{
    public class TemperatureThreshold : ICloneable
    {
        #region Private Fields

        private int cpuUpThreshold;
        private int cpuDownThreshold;
        private int gpuUpThreshold;
        private int gpuDownThreshold;
        private float fanSpeed;

        #endregion

        #region Properties

        public int CpuUpThreshold
        {
            get { return cpuUpThreshold; }
            set { cpuUpThreshold = value; }
        }

        public int CpuDownThreshold
        {
            get { return cpuDownThreshold; }
            set { cpuDownThreshold = value; }
        }
 
        public int GpuUpThreshold
        {
            get { return gpuUpThreshold; }
            set { gpuUpThreshold = value; }
        }

        public int GpuDownThreshold
        {
            get { return gpuDownThreshold; }
            set { gpuDownThreshold = value; }
        }

        public float FanSpeed
        {
            get 
            { 
                return fanSpeed; 
            }

            set
            {
                if (value > 100)
                {
                    fanSpeed = 100;
                }
                else if (value < 0)
                {
                    fanSpeed = 0;
                }
                else
                {
                    fanSpeed = value;
                }
            }
        }

        #endregion

        #region Constructors

        public TemperatureThreshold()
        { }

        public TemperatureThreshold(
            int cpuUpThreshold, 
            int cpuDownThreshold, 
            int gpuUpThreshold, 
            int gpuDownThreshold, 
            float fanSpeed)
        {
            this.CpuUpThreshold = cpuUpThreshold;
            this.CpuDownThreshold = cpuDownThreshold;
            this.GpuUpThreshold = gpuUpThreshold;
            this.GpuDownThreshold = gpuDownThreshold;
            this.FanSpeed = fanSpeed;
        }

        #endregion

        #region ICloneable implementation

        public virtual object Clone()
        {
            return new TemperatureThreshold(
                this.cpuUpThreshold, 
                this.cpuDownThreshold, 
                this.gpuUpThreshold, 
                this.gpuDownThreshold, 
                this.fanSpeed);
        }

        #endregion
    }
}
