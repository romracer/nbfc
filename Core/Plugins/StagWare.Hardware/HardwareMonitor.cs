using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StagWare.Hardware
{
    public sealed class HardwareMonitor
    {
        #region Private Fields

        private static object syncRoot = new object();
        private static volatile HardwareMonitor instance;

        private Computer computer;
        private IHardware[] cpus;
        private ISensor[][] cpuTempSensors;
        private IHardware[] gpus;
        private ISensor[][] gpuTempSensors;

        #endregion

        #region Constructor

        private HardwareMonitor()
        {
            this.computer = new Computer();
            this.computer.IsCpuEnabled = true;
            this.computer.IsGpuEnabled = true;
            this.computer.Open();
        }

        #endregion

        #region Properties

        public static HardwareMonitor Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new HardwareMonitor();
                        }
                    }
                }

                return instance;
            }
        }

        public KeyValuePair<string, double>[] CpuTemperatures
        {
            get
            {
                if (this.cpus == null)
                {
                    InitializeCpuSensors();
                }

                var results = new KeyValuePair<string, double>[this.cpus.Length];

                for (int i = 0; i < this.cpus.Length; i++)
                {
                    this.cpus[i].Update();
                    results[i] = new KeyValuePair<string, double>(
                        this.cpus[i].Name, 
                        GetAverageTemperature(this.cpuTempSensors[i]));
                }

                return results;
            }
        }

        public KeyValuePair<string, double>[] GpuTemperatures
        {
            get
            {
                if (this.gpus == null)
                {
                    InitializeGpuSensors();
                }

                var results = new KeyValuePair<string, double>[this.gpus.Length];

                for (int i = 0; i < this.gpus.Length; i++)
                {
                    this.gpus[i].Update();
                    results[i] = new KeyValuePair<string, double>(
                        this.gpus[i].Name,
                        GetAverageTemperature(this.gpuTempSensors[i]));
                }

                return results;
            }
        }

        #endregion

        #region Public Methods

        public bool WaitIsaBusMutex(int timeout)
        {
            return this.computer.WaitIsaBusMutex(timeout);
        }

        public void ReleaseIsaBusMutex()
        {
            this.computer.ReleaseIsaBusMutex();
        }

        public void WriteIoPort(int port, byte value)
        {
            this.computer.WriteIoPort(port, value);
        }

        public byte ReadIoPort(int port)
        {
            return this.computer.ReadIoPort(port);
        }

        #endregion

        #region Private Methods

        private static double GetAverageTemperature(ISensor[] sensors)
        {
            double temperatureSum = 0;
            int count = 0;

            foreach (ISensor sensor in sensors)
            {
                if (sensor.Value.HasValue)
                {
                    temperatureSum += sensor.Value.Value;
                    count++;
                }
            }

            return temperatureSum / count;
        }

        private static ISensor[] GetCpuTemperatureSensors(IHardware cpu)
        {
            var sensors = new List<ISensor>();
            cpu.Update();

            foreach (ISensor s in cpu.Sensors)
            {
                if (s.SensorType == SensorType.Temperature)
                {
                    string name = s.Name.ToUpper();

                    if (name.Contains("PACKAGE") || name.Contains("TDIE"))
                    {
                        return new ISensor[] { s };
                    }
                    else
                    {
                        sensors.Add(s);
                    }
                }
            }

            return sensors.ToArray();
        }

        private static ISensor[] GetGpuTemperatureSensors(IHardware gpu)
        {
            gpu.Update();

            foreach (ISensor s in gpu.Sensors)
            {
                if (s.SensorType == SensorType.Temperature)
                {
                    string name = s.Name.ToUpper();

                    if (name.Contains("SPOT"))
                    {
                        return new ISensor[] { s };
                    }
                }
            }
            return new ISensor[] {};
        }

        private void InitializeCpuSensors()
        {
            this.cpus = GetHardware(HardwareType.Cpu);
            this.cpuTempSensors = new ISensor[this.cpus.Length][];
            int sensorsTotal = 0;

            for (int i = 0; i < this.cpus.Length; i++)
            {
                ISensor[] sensors = GetCpuTemperatureSensors(this.cpus[i]);
                sensorsTotal += sensors.Length;
                this.cpuTempSensors[i] = sensors;
            }

            if (sensorsTotal <= 0)
            {
                throw new PlatformNotSupportedException("Failed to access CPU temperature sensors(s).");
            }
        }

        private void InitializeGpuSensors()
        {
            var gpuAll = new List<IHardware>();
            gpuAll.AddRange(GetHardware(HardwareType.GpuAmd));
            gpuAll.AddRange(GetHardware(HardwareType.GpuNvidia));

            var gpuList = new List<IHardware>();
            var tempList = new List<ISensor[]>();

            for (int i = 0; i < gpuAll.Count; i++)
            {
                ISensor[] sensors = GetGpuTemperatureSensors(gpuAll[i]);
 
                if (sensors.Length > 0)
                {
                    gpuList.Add(gpuAll[i]);
                    tempList.Add(sensors);
                }
            }
            this.gpus = gpuList.ToArray();
            this.gpuTempSensors = tempList.ToArray();
        }

        private IHardware[] GetHardware(HardwareType type)
        {
            return this.computer.Hardware.Where(x => x.HardwareType == type).ToArray();
        }

        #endregion
    }
}
