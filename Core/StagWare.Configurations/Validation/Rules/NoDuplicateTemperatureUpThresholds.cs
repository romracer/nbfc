using System.Collections.Generic;

namespace StagWare.FanControl.Configurations.Validation.Rules
{
    public class NoDuplicateTemperatureUpThresholds : IValidationRule<FanControlConfigV2>
    {
        public string Description => "A fan's temperature thresholds must have unique up-thresholds";

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

            foreach (FanConfiguration cfg in item.FanConfigurations)
            {
                if (cfg.TemperatureThresholds == null)
                {
                    continue;
                }
                //todo: cpu/gpu only check
                //todo: should use a list to sort and ensure there are no threshold a and b such that
                //CpuUpThreshold of a is greater than b, but GpuUpThreshold of a is smaller than b.
                //sort their sum.
                var lookup = new HashSet<int[]>();

                foreach (var threshold in cfg.TemperatureThresholds)
                {
                    if (lookup.Contains(new int[2] { threshold.CpuUpThreshold , threshold.GpuUpThreshold }))
                    {
                        v.Result = ValidationResult.Error;
                        v.Reason = "There is at least one duplicate up-threshold: CPU " + threshold.CpuUpThreshold + 
                            " and GPU" + threshold.GpuUpThreshold;
                        return v;
                    }
                    else
                    {
                        lookup.Add(new int[2] { threshold.CpuUpThreshold, threshold.GpuUpThreshold });
                    }
                }
            }

            return v;
        }
    }
}
