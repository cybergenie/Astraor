﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using STIL_NET;


namespace MeasurementSensors
{
    public class StilSensor
    {
        public delegate void SensorStatusHandler(object sender, EventArgs e);
        public event SensorStatusHandler OnSensorStatus;

        private uint buffer_length = 0;
        private uint number_of_buffers = 0;
        private string sensorStatus = null;
        private dll_chr m_dll_chr = null;
        private sensorCCSPrima m_sensor = null;
        private enSensorError sError = enSensorError.MCHR_ERROR_NONE;
        private sensorManager m_sensor_manager = new sensorManager();
        private cAcqParamMeasurement acqParamMeasurement = new cAcqParamMeasurement();
        private cNamedEvent m_measurement_event = new cNamedEvent();
        private cNamedEvent m_exit_event = new cNamedEvent();
        private cNamedEvent m_exit_event_do = new cNamedEvent();
        public StilException StilEx = null;

        public struct SensorValue
        {
            public float Altitude;
            public float Counter;
        }

        private List<SensorValue> sensorValueList = new List<SensorValue>();

        public string SensorStatus
        {
            get { return sensorStatus; }
            set
            {
                sensorStatus = value;
                OnSensorStatus(this, new EventArgs());
            }

        }

        public List<SensorValue> SensorValueList
        {
            get { return sensorValueList; }
        }

        public uint BufferLength
        {
            get { return buffer_length; }
            set { buffer_length = value; }
        }

        public uint PointsNumber
        {
            get { return number_of_buffers; }
            set { number_of_buffers = value; }
        }

        public bool Init()
        {
            bool status = true;
            try {
                m_dll_chr = new dll_chr();

                if (m_dll_chr.Init() == false)
                {
                    throw new StilException("Init:传感器连接错误。");                    
                }

            }
            catch(StilException ex)
            {
                status = false;
                MessageBox.Show(ex.ToString());
            }

            catch
            {
                status = false;               
            }
            return status;


        }

        public bool Release()
        {
            if (m_sensor != null)
            {
                m_sensor.Release();
            }
            m_dll_chr.Release();
            return (true);
        }

        public virtual bool OpenNoTrigger(enSensorType sensorType)
        {
            bool result = true;

            //open sensor
            try
            {
                m_sensor = (sensorCCSPrima)m_sensor_manager.OpenUsbConnection("", sensorType, null, null);
                if (m_sensor != null)
                {
                    m_sensor.OnError += new sensor.ErrorHandler(OnError);
                    //    //get automatic parameters
                    if (acqParamMeasurement.Init(m_sensor) == enSensorError.MCHR_ERROR_NONE)
                    {
                        // Set buffer size (should be > 0)
                        acqParamMeasurement.BufferLength = buffer_length;
                        // Set Number of acquisition buffers per data (should be > 1)
                        acqParamMeasurement.NumberOfBuffers = number_of_buffers;
                        //set altitude and counter buffering enabled
                        acqParamMeasurement.EnableBufferAltitude.Altitude = true;
                        acqParamMeasurement.EnableBufferAltitude.Counter = true;
                        //set timeout acquisition : should be at least = ((BufferLength * averaging) / rate) + 100
                        acqParamMeasurement.Timeout = 2000;
                        //event type (here end of measurements) and callback function
                        acqParamMeasurement.EnableEvent.EventEndBuffer = true;
                        m_sensor.OnEventMeasurement += new sensor.OnEventMeasurementHandler(FuncEventMeasurement);
                    }
                    else
                    {
                        Close();
                        throw new StilException("驱动初始化失败。");                        

                    }
                }
                else
                {
                    throw new StilException("错误(OpenNoTrigger) : 打开传感器失败,没有连接传感器或传感器损坏,请检查传感器。");
                    
                }

            }

            catch (StilException ex)
            {
                result = false;
                MessageBox.Show(ex.ToString());
            }
            catch
            {
                result = false;
            }

            return (result);

        }

        public void Execute()
        {
            float[] Altitude = new float[acqParamMeasurement.BufferLength];
            float[] Counter = new float[acqParamMeasurement.BufferLength];
            float[] Intensity = new float[acqParamMeasurement.BufferLength];
            float[] BufferNullFloat = null;
            uint Len = 0;

            while (m_exit_event.Wait(0) == false)
            {
                try
                {
                    if (m_measurement_event.Wait(10))
                    {
                        sError = m_sensor.GetAltitudeAcquisitionData(ref Altitude, ref Intensity, ref Counter, ref BufferNullFloat, ref BufferNullFloat, ref Len);
                        if (sError == enSensorError.MCHR_ERROR_NONE)
                        {
                            for (uint idx = 0; idx < Len; idx++)
                            {
                                SensorValue temp = new SensorValue();
                                temp.Altitude = Altitude[idx];
                                temp.Counter = Intensity[idx];
                                sensorValueList.Add(temp);
                            }
                        }
                        else
                        {
                            throw new StilException(string.Format("FuncEventMeasurement : 错误 : GetAltitudeAcquisitionData : {0}", sError.ToString()));
                        }
                    }
                }

                catch(StilException ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                catch
                {
                    m_exit_event_do.Set();
                }
            }
            m_exit_event_do.Set();
        }

        public bool EndExecute()
        {
            return (m_exit_event_do.Wait(10));
        }

        void OnError(object sender, cErrorEventArgs e)
        {
            MessageBox.Show(string.Format("错误:{0}-{1}{2}", e.Exception.ErrorType, e.Exception.FunctionName, e.Exception.ErrorDetail));
        }

        public void FuncEventMeasurement(sensor.enSensorAcquisitionEvent ev)
        {

            switch (ev)
            {
                case sensor.enSensorAcquisitionEvent.EV_END_ACQUIRE:
                    m_exit_event.Set();
                    break;
                case sensor.enSensorAcquisitionEvent.EV_END_BUFFER:
                    m_measurement_event.Set();
                    break;
                case sensor.enSensorAcquisitionEvent.EV_END_MEASUREMENT:
                    m_measurement_event.Set();
                    break;
                default:
                    break;
            }
            // throw new StilException(string.Format("事件 : {0}", ev.ToString()));
        }

        public bool SetParameter()
        {
            //set 500hz acquisition frequency
            m_sensor.ScanRate = (enFixedScanRates_CCS_PRIMA)enFixedScanRates_CCS_PRIMA.CCS_PRIMA_1000HZ;
            //set averaging = 1 for acquisition
            m_sensor.Averaging = 1;
            m_sensor.LedAuto = false;
            m_sensor.LedBrightness = 50;      
            return (true);
        }

        public bool StartAcquisition()
        {
            bool result = true;
            try
            {
                if (m_sensor != null)
                {
                    sError = m_sensor.StartAcquisition_Measurement(acqParamMeasurement);
                    if (sError != enSensorError.MCHR_ERROR_NONE)
                    {
                        throw new StilException(string.Format("错误 : StartAcquisition : {0}", sError.ToString()));
                    }
                }
                else
                {
                    throw new StilException("错误(StartAcquisition) : 开始测量异常， 没有连接传感器或传感器损坏,请检查传感器。");
                    
                }
            }

            catch(StilException ex)
            {
                result = false;
                MessageBox.Show(ex.ToString());
            }
            catch 
            {
                result = false;
            }
            return (result);
        }

        public bool StopAcquisition()
        {
            bool result = true;
            try
            {
                if (m_sensor != null)
                {
                    m_sensor.StopAcquisition_Measurement();
                }
                else
                {
                    throw new StilException("错误(StopAcquisition) : 停止数据测量异常，没有连接传感器或传感器损坏,请检查传感器。");
                   
                }
            }
            catch(StilException ex)
            {
                result = false;
                MessageBox.Show(ex.ToString());
            }
            catch
            {
                result = false;
            }
            return (result);
        }

        public bool Close()
        {
            bool result = true;

            try
            {

                if (m_sensor != null)
                {
                    m_sensor.Close();
                }
                else
                {
                    throw new StilException("错误(Close) : 关闭传感器异常,没有连接传感器或传感器损坏,请检查传感器。");
                    
                }

            }

            catch(StilException ex)
            {
                result = false;
                MessageBox.Show(ex.ToString());
            }
            catch
            {
                result = false;
            }

            return (result);
        }
    }

    public class StilException : ApplicationException
    {
        public StilException(string message) : base(message)
        { }
    }

}
