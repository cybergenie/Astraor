using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using STIL_NET;

namespace MeasurementSensors
{
    class MeasureNoTrigger
    {
        public void GetPoints()
        {
            StilSensor Sensor = new StilSensor();
            Sensor.BufferLength = 5;
            Sensor.PointsNumber = 2000;

            if (Sensor!=null)
                if(Sensor.Init())
                    if(Sensor.OpenNoTrigger(enSensorType.CCS_OPTIMA))
                    {
                        Sensor.SetParameter();
                        if(Sensor.StartAcquisition())
                        {
                            Thread mThread = new Thread(new ThreadStart());
                        }
                    }

            
        }
    }
}
