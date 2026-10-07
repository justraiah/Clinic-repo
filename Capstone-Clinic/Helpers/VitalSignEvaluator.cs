using Capstone_Clinic.Models;
using System.Linq;


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
                remarks.Add("Hyperpyrexia");
            }
            else if (vital.Temperature > 37.5)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Fever");
            }
            else if (vital.Temperature < 35.0)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Hypothermia");
            }
            else if (vital.Temperature < 36.1)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Hypothermia");
            }

            // Heart Rate
            if (vital.HeartRate > 120)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Tachycardia");
            }
            else if (vital.HeartRate > 100)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Tachycardia");
            }
            else if (vital.HeartRate > 0 && vital.HeartRate < 50)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Bradycardia");
            }
            else if (vital.HeartRate >= 50 && vital.HeartRate < 60)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Bradycardia");
            }
            // Respiratory Rate
            if (vital.RespiratoryRate < 8)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Bradypnea");
            }
            else if (vital.RespiratoryRate < 12)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Bradypnea");
            }
            else if (vital.RespiratoryRate > 30)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Tachypnea");
            }
            else if (vital.RespiratoryRate > 20)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Tachypnea");
            }

            // SpO₂
            if (vital.OxygenSaturation > 0 && vital.OxygenSaturation < 90)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Hypoxemia");
            }
            else if (vital.OxygenSaturation >= 90 && vital.OxygenSaturation < 95)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Hypoxemia");
            }

            // Systolic Blood Pressure
            if (vital.SystolicBP >= 140)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Hypertension");
            }
            else if (vital.SystolicBP > 120)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Hypertension");
            }
            else if (vital.SystolicBP < 80)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Hypotension");
            }
            else if (vital.SystolicBP < 90)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Hypotension");
            }

            // Diastolic Blood Pressure
            if (vital.DiastolicBP >= 90)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Hypertension");
            }
            else if (vital.DiastolicBP > 80)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Hypertension");
            }
            else if (vital.DiastolicBP < 50)
            {
                vital.Status = "Critical";
                remarks.Add("Severe Hypotension");
            }
            else if (vital.DiastolicBP < 60)
            {
                if (vital.Status != "Critical")
                    vital.Status = "Warning";

                remarks.Add("Hypotension");
            }

            vital.Remarks = string.Join(", ", remarks.Distinct());
        }
    }
}