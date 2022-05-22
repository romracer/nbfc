using StagWare.FanControl.Configurations;
using StagWare.FanControl.Plugins;
using System;
using System.Collections.Generic;

namespace StagWare.FanControl
{
    internal class Fan
    {
        #region Constants

        public const int AutoFanSpeed = 101;
        private const int CriticalTemperatureOffset = 8;

        #endregion

        #region Private Fields

        private readonly bool readWriteWords;
        private readonly int cpuCriticalTemperature;
        private readonly int gpuCriticalTemperature;
        private readonly IEmbeddedController ec;
        private readonly FanConfiguration fanConfig;

        private readonly int minSpeedValueWrite;
        private readonly int maxSpeedValueWrite;
        private readonly int minSpeedValueRead;
        private readonly int maxSpeedValueRead;
        private readonly int minSpeedValueReadAbs;
        private readonly int maxSpeedValueReadAbs;
        private readonly TemperatureThresholdManager threshMan;
        private readonly Dictionary<float, FanSpeedPercentageOverride> overriddenPercentages;
        private readonly Dictionary<int, FanSpeedPercentageOverride> overriddenValues;

        private float targetFanSpeed;

        #endregion

        #region Properties

        public float TargetSpeed
        {
            get
            {
                return this.CriticalModeEnabled
                    ? 100.0f
                    : this.targetFanSpeed;
            }
        }

        public bool FanEnabled { get; set; }
        public float CurrentSpeed { get; private set; }
        public bool AutoControlEnabled { get; private set; }
        public bool CriticalModeEnabled { get; private set; }

        #endregion

        #region Constructors

        //this is a hack for test, need to change the test later
        public Fan(IEmbeddedController ec, FanConfiguration config, int cpuCriticalTemperature, bool readWriteWords)
            :this(ec, config, cpuCriticalTemperature, cpuCriticalTemperature, readWriteWords)
        {
        }

        public Fan(IEmbeddedController ec, FanConfiguration config, int cpuCriticalTemperature, int gpuCriticalTemperature, bool readWriteWords)
        {
            this.FanEnabled = true;

            this.ec = ec;
            this.fanConfig = config;
            this.cpuCriticalTemperature = cpuCriticalTemperature;
            this.gpuCriticalTemperature = gpuCriticalTemperature;
            this.readWriteWords = readWriteWords;

            this.overriddenPercentages = new Dictionary<float, FanSpeedPercentageOverride>();
            this.overriddenValues = new Dictionary<int, FanSpeedPercentageOverride>();

            this.minSpeedValueWrite = config.MinSpeedValue;
            this.maxSpeedValueWrite = config.MaxSpeedValue;

            if (config.IndependentReadMinMaxValues)
            {
                this.minSpeedValueRead = config.MinSpeedValueRead;
                this.maxSpeedValueRead = config.MaxSpeedValueRead;
            }
            else
            {
                this.minSpeedValueRead = this.minSpeedValueWrite;
                this.maxSpeedValueRead = this.maxSpeedValueWrite;
            }

            this.minSpeedValueReadAbs = Math.Min(this.minSpeedValueRead, this.maxSpeedValueRead);
            this.maxSpeedValueReadAbs = Math.Max(this.minSpeedValueRead, this.maxSpeedValueRead);

            if (config.TemperatureThresholds != null
                && config.TemperatureThresholds.Count > 0
                &&(config.IsCpuFan || config.IsGpuFan))
            {
                if (!config.IsCpuFan)
                {
                    foreach (TemperatureThreshold temperatureThreshold in config.TemperatureThresholds)
                    {
                        temperatureThreshold.CpuUpThreshold = 200;
                        temperatureThreshold.CpuDownThreshold = 200;
                    }
                }
                if (!config.IsGpuFan)
                {
                    foreach (TemperatureThreshold temperatureThreshold in config.TemperatureThresholds)
                    {
                        temperatureThreshold.GpuUpThreshold = 200;
                        temperatureThreshold.GpuDownThreshold = 200;
                    }
                }
                this.threshMan = new TemperatureThresholdManager(config.TemperatureThresholds);
            }
            else
            {
                this.threshMan = new TemperatureThresholdManager(FanConfiguration.DefaultTemperatureThresholds);
            }

            foreach (FanSpeedPercentageOverride o in config.FanSpeedPercentageOverrides)
            {
                if (o.TargetOperation.HasFlag(OverrideTargetOperation.Write)
                    && !this.overriddenPercentages.ContainsKey(o.FanSpeedPercentage))
                {
                    this.overriddenPercentages.Add(o.FanSpeedPercentage, o);
                }

                if (o.TargetOperation.HasFlag(OverrideTargetOperation.Read)
                    && !this.overriddenValues.ContainsKey(o.FanSpeedValue))
                {
                    this.overriddenValues.Add(o.FanSpeedValue, o);
                }
            }
        }

        #endregion

        #region Public Methods

        //test used only, need to change the test
        public virtual void SetTargetSpeed(float speed, float cpuTemperature, bool readOnly)
        {
            SetTargetSpeed(speed, cpuTemperature, 0, readOnly);
        }

        public virtual void SetTargetSpeed(float speed, float cpuTemperature, float gpuTemperature, bool readOnly)
        {
            HandleCriticalMode(cpuTemperature, gpuTemperature);
            this.AutoControlEnabled = (speed < 0) || (speed > 100);

            if (AutoControlEnabled)
            {
                var threshold = this.threshMan.AutoSelectThreshold(cpuTemperature, gpuTemperature);

                if (threshold != null)
                {
                    this.targetFanSpeed = threshold.FanSpeed;
                }
            }
            else
            {
                this.targetFanSpeed = speed;
            }

            speed = CriticalModeEnabled ? 100.0f : this.targetFanSpeed;

            if (!readOnly && FanEnabled)
            {
                ECWriteValue(PercentageToFanSpeed(speed));
            }
        }

        public virtual float GetCurrentSpeed()
        {
            int speed = 0;

            // If the value is out of range 3 or more times,
            // minFanSpeed and/or maxFanSpeed are probably wrong.
            for (int i = 0; i <= 2; i++)
            {
                speed = ECReadValue();

                if ((speed >= minSpeedValueReadAbs) && (speed <= maxSpeedValueReadAbs))
                {
                    break;
                }
            }

            CurrentSpeed = FanSpeedToPercentage(speed);
            return CurrentSpeed;
        }

        public virtual void Reset()
        {
            if (fanConfig.ResetRequired)
            {
                ECWriteValue(fanConfig.FanSpeedResetValue);
            }
        }

        #endregion

        #region Private Methods

        private int PercentageToFanSpeed(float percentage)
        {
            if ((percentage > 100) || (percentage < 0))
            {
                throw new ArgumentOutOfRangeException(
                    "percentage",
                    "Percentage must be greater or equal 0 and less or equal 100");
            }

            if (this.overriddenPercentages.ContainsKey(percentage))
            {
                return this.overriddenPercentages[percentage].FanSpeedValue;
            }
            else
            {
                return (int)Math.Round(minSpeedValueWrite
                    + (((maxSpeedValueWrite - minSpeedValueWrite) * percentage) / 100.0));
            }
        }

        private float FanSpeedToPercentage(int fanSpeed)
        {
            if (this.overriddenValues.ContainsKey(fanSpeed))
            {
                return this.overriddenValues[fanSpeed].FanSpeedPercentage;
            }
            else
            {
                if (minSpeedValueRead == maxSpeedValueRead)
                {
                    return 0;
                }
                else
                {
                    return ((float)(fanSpeed - minSpeedValueRead)
                        / (maxSpeedValueRead - minSpeedValueRead)) * 100;
                }
            }
        }

        private void ECWriteValue(int value)
        {
            if (readWriteWords)
            {
                this.ec.WriteWord((byte)this.fanConfig.WriteRegister, (ushort)value);
            }
            else
            {
                this.ec.WriteByte((byte)this.fanConfig.WriteRegister, (byte)value);
            }
        }

        private int ECReadValue()
        {
            return readWriteWords
                ? this.ec.ReadWord((byte)this.fanConfig.ReadRegister)
                : this.ec.ReadByte((byte)this.fanConfig.ReadRegister);
        }

        private void HandleCriticalMode(double cpuTemperature, double gpuTemperature)
        {
            if (this.CriticalModeEnabled
                && (cpuTemperature < (this.cpuCriticalTemperature - CriticalTemperatureOffset))
                && (gpuTemperature < (this.gpuCriticalTemperature - CriticalTemperatureOffset)))
            {
                this.CriticalModeEnabled = false;
            }
            else if (cpuTemperature > this.cpuCriticalTemperature
                || gpuTemperature > this.gpuCriticalTemperature)
            {
                this.CriticalModeEnabled = true;
            }
        }

        #endregion
    }
}
