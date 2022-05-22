using System.Linq;

namespace StagWare.FanControl.Configurations.Validation.Rules
{
    public class UpThresholdMayNotBeLowerThanDownThreshold : IValidationRule<FanControlConfigV2>
    {
        public string Description => "Each threshold's, up-threshold may not be lower than its corresponding down-threshold";

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

            foreach (var t in item.FanConfigurations.SelectMany(x => x.TemperatureThresholds))
            {
                if (t.CpuUpThreshold < t.CpuDownThreshold)
                {
                    v.Result = ValidationResult.Error;
                    v.Reason = $"At least one CPU up-threshold ({t.CpuUpThreshold}) is less than its corresponding CPU down-threshold ({t.CpuDownThreshold})";
                    return v;
                }
                if (t.GpuUpThreshold < t.GpuDownThreshold)
                {
                    v.Result = ValidationResult.Error;
                    v.Reason = $"At least one GPU up-threshold ({t.GpuUpThreshold}) is less than its corresponding GPU down-threshold ({t.GpuDownThreshold})";
                    return v;
                }
            }

            return v;
        }
    }
}
