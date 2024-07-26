using System;

namespace Corteos.Models
{
    [Serializable]
    public class ValuteModel
    {
        public int NumCode { get; set; }
        public string CharCode { get; set; }
        public int Nominal { get; set; }
        public string Name { get; set; }
        public float Value { get; set; }
        public float VunitRate { get; set; }
    }
}
