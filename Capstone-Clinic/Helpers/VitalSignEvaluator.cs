using Capstone_Clinic.Models;

namespace Capstone_Clinic.Helpers
{
    public static class VitalSignEvaluator
    {
        public static void Evaluate(VitalSignLog vital)
        {
            vital.Status = "Normal";

            List<string> remarks = new();

            // Temperature
            if (vital.Temperature > 39.0)
            {
                vital.Status = "Critical";
                remarks.Add("Critical High Temperature");
            }
            else if (vital.Temperature > 37.5)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("High Temperature");
            }
            else if (vital.Temperature < 35.0)
            {
                vital.Status = "Critical";
                remarks.Add("Critical Low Temperature");
            }
            else if (vital.Temperature < 36.1)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Low Temperature");
            }

            // Heart Rate
            if (vital.HeartRate > 120)
            {
                vital.Status = "Critical";
                remarks.Add("Critical High Heart Rate");
            }
            else if (vital.HeartRate > 100)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("High Heart Rate");
            }
            else if (vital.HeartRate > 0 && vital.HeartRate < 50)
            {
                vital.Status = "Critical";
                remarks.Add("Critical Low Heart Rate");
            }
            else if (vital.HeartRate >= 50 && vital.HeartRate < 60)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Low Heart Rate");
            }

            // SpO₂
            if (vital.OxygenSaturation > 0 && vital.OxygenSaturation < 90)
            {
                vital.Status = "Critical";
                remarks.Add("Critical Low Oxygen Saturation");
            }
            else if (vital.OxygenSaturation >= 90 && vital.OxygenSaturation < 95)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Low Oxygen Saturation");
            }

            // Systolic Blood Pressure
            if (vital.SystolicBP >= 140)
            {
                vital.Status = "Critical";
                remarks.Add("Critical High Systolic Blood Pressure");
            }
            else if (vital.SystolicBP > 120)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("High Systolic Blood Pressure");
            }
            else if (vital.SystolicBP < 80)
            {
                vital.Status = "Critical";
                remarks.Add("Critical Low Systolic Blood Pressure");
            }
            else if (vital.SystolicBP < 90)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Low Systolic Blood Pressure");
            }

            // Diastolic Blood Pressure
            if (vital.DiastolicBP >= 90)
            {
                vital.Status = "Critical";
                remarks.Add("Critical High Diastolic Blood Pressure");
            }
            else if (vital.DiastolicBP > 80)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("High Diastolic Blood Pressure");
            }
            else if (vital.DiastolicBP < 50)
            {
                vital.Status = "Critical";
                remarks.Add("Critical Low Diastolic Blood Pressure");
            }
            else if (vital.DiastolicBP < 60)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Low Diastolic Blood Pressure");
            }

            vital.Remarks = string.Join(", ", remarks);
        }
    }
}