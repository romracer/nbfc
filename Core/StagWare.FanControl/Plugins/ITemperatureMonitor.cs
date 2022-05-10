namespace StagWare.FanControl.Plugins
{
    public interface ITemperatureMonitor : IFanControlPlugin
    {
        double GetCpuTemperature();
        double GetGpuTemperature();
    }
}
