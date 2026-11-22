using Capstone_Clinic.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Capstone_Clinic.Helpers
{
    public static class VitalSignEvaluator
    {
        public static void Evaluate(VitalSignLog vital)
        {
            string preStatus = "Normal";
            string postStatus = "Normal";

            List<string> remarks = new();

            // Evaluate Pre-consultation readings
            List<string> preRemarks = new();

            EvaluateStage(
                vital.Temperature,
                vital.HeartRate,
                vital.RespiratoryRate,
                vital.OxygenSaturation,
                vital.SystolicBP,
                vital.DiastolicBP,
                ref preStatus,
                preRemarks
            );

            // Evaluate Post-consultation readings
            List<string> postRemarks = new();

            EvaluateStage(
                vital.PostTemperature,
                vital.PostHeartRate,
                vital.PostRespiratoryRate,
                vital.PostOxygenSaturation,
                vital.PostSystolicBP,
                vital.PostDiastolicBP,
                ref postStatus,
                postRemarks
            );

            // Preserve remarks from both stages
            remarks.AddRange(preRemarks.Select(r => "Pre: " + r));
            remarks.AddRange(postRemarks.Select(r => "Post: " + r));

            // Overall status = highest severity from either stage
            vital.Status = GetHighestStatus(preStatus, postStatus);

            vital.Remarks = string.Join(", ", remarks.Distinct());
        }

        private static void EvaluateStage(
            double? temperature,
            int? heartRate,
            int? respiratoryRate,
            int? oxygenSaturation,
            int? systolicBP,
            int? diastolicBP,
            ref string status,
            List<string> remarks)
        {
            // Temperature
            if (temperature.HasValue)
            {
                if (temperature.Value > 39.0)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Hyperpyrexia");
                }
                else if (temperature.Value > 37.5)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Fever");
                }
                else if (temperature.Value < 35.0)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Hypothermia");
                }
                else if (temperature.Value < 36.1)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Hypothermia");
                }
            }

            // Pulse Rate
            if (heartRate.HasValue)
            {
                if (heartRate.Value > 120)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Tachycardia");
                }
                else if (heartRate.Value > 100)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Tachycardia");
                }
                else if (heartRate.Value > 0 && heartRate.Value < 50)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Bradycardia");
                }
                else if (heartRate.Value >= 50 && heartRate.Value < 60)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Bradycardia");
                }
            }

            // Respiratory Rate
            if (respiratoryRate.HasValue)
            {
                if (respiratoryRate.Value < 8)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Bradypnea");
                }
                else if (respiratoryRate.Value < 12)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Bradypnea");
                }
                else if (respiratoryRate.Value > 30)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Tachypnea");
                }
                else if (respiratoryRate.Value > 20)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Tachypnea");
                }
            }

            // SpO2
            if (oxygenSaturation.HasValue)
            {
                if (oxygenSaturation.Value > 0 && oxygenSaturation.Value < 90)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Hypoxemia");
                }
                else if (oxygenSaturation.Value >= 90 && oxygenSaturation.Value < 95)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Hypoxemia");
                }
            }

            // Systolic Blood Pressure
            if (systolicBP.HasValue)
            {
                if (systolicBP.Value >= 140)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Hypertension");
                }
                else if (systolicBP.Value > 120)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Hypertension");
                }
                else if (systolicBP.Value < 80)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Hypotension");
                }
                else if (systolicBP.Value < 90)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Hypotension");
                }
            }

            // Diastolic Blood Pressure
            if (diastolicBP.HasValue)
            {
                if (diastolicBP.Value >= 90)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Hypertension");
                }
                else if (diastolicBP.Value > 80)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Hypertension");
                }
                else if (diastolicBP.Value < 50)
                {
                    SetStatus(ref status, "Critical");
                    remarks.Add("Severe Hypotension");
                }
                else if (diastolicBP.Value < 60)
                {
                    SetStatus(ref status, "Warning");
                    remarks.Add("Hypotension");
                }
            }
        }

        private static void SetStatus(ref string current, string newStatus)
        {
            if (GetSeverity(newStatus) > GetSeverity(current))
            {
                current = newStatus;
            }
        }

        private static string GetHighestStatus(string first, string second)
        {
            return GetSeverity(first) >= GetSeverity(second)
                ? first
                : second;
        }

        private static int GetSeverity(string status)
        {
            return status switch
            {
                "Critical" => 3,
                "Warning" => 2,
                "Normal" => 1,
                _ => 0
            };
        }
    }
}