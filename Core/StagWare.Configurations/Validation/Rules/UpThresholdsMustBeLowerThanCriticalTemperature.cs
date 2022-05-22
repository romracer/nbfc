using System.Linq;

namespace StagWare.FanControl.Configurations.Validation.Rules
{
    public class UpThresholdsMustBeLowerThanCriticalTemperature : IValidationRule<FanControlConfigV2>
    {
        public string Description => "All up-threshold values must be lower than the critical temperature";

        public Validation Validate(FanControlConfigV2 item)
        {
            var v = new Validation()
            {
                RuleDescription = this.Description,
                Result = ValidationResult.Success
            };

            if (item.FanConfigurations == null)
            {
                return v;
            }

            foreach (var cfg in item.FanConfigurations)
            {
                var cpuThreshold = cfg.TemperatureThresholds?.FirstOrDefault(x => x.CpuUpThreshold >= item.CpuCriticalTemperature);

                if (cpuThreshold != null)
                {
                    v.Result = ValidationResult.Error;
                    v.Reason = "At least one CPU up-threshold is higher than or equal to the CPU critical temperature: " + cpuThreshold.CpuUpThreshold;
                    return v;
                }

                cpuThreshold = cfg.TemperatureThresholds?.FirstOrDefault(x => x.CpuUpThreshold >= (item.CpuCriticalTemperature - 5));

                if (cpuThreshold != null)
                {
                    v.Result = ValidationResult.Warning;
                    v.Reason = "At least one CPU up-threshold is less than 5 degrees below the CPU critical temperature: " + cpuThreshold.CpuUpThreshold;
                    return v;
                }
            }
            //todo: check gpu if readGpuTemperature

            return v;
        }
    }
}
