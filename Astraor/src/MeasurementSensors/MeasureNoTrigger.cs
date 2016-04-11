using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STIL_NET;

namespace MeasurementSensors
{
    class MeasureNoTrigger:StilSensor
    {
        public override bool Open(enSensorType sensorType)
        {
            return base.Open(sensorType);
        }
    }
}
