using System;

namespace Corteos.Models
{
    class ExchangeRateModel
    {
        public DateTime date { get; set; }
        public int num_code { get; set; }
        public int nominal { get; set; }
        public float value { get; set; }
        public float vunit_rate { get; set; }
    }
}
