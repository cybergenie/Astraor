using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using STIL_NET;


namespace MeasurementSensors
{
    public class MeasureNoTrigger
    {
        StilSensor Sensor;

        public void StartMeasure()
        {
            Sensor = new StilSensor();
            Sensor.BufferLength = 5;
            Sensor.PointsNumber = 2000;

            if (Sensor != null)
                if (Sensor.Init())
                    if (Sensor.OpenNoTrigger(enSensorType.CCS_PRIMA))
                    {
                        Sensor.SetParameter();
                        if (Sensor.StartAcquisition())
                        {
                            Thread mThread = new Thread(new ThreadStart(Sensor.Execute));
                            mThread.Start();

                        }
                    }
        }

        public void StopMeasure()
        {
            Sensor.StopAcquisition();
            Sensor.Close();
            Sensor.Release();
        }

        public List<StilSensor.SensorValue> GetValueList
        {
            get { return Sensor.SensorValueList; }
        }
    }
}
