﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using AForge.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;

namespace AgentSensorFaceLib
{
    public class Sensor
    {
        #region EVENTS

        #region FACE_DETECTED
        public delegate void FaceDetected(object sender, Face args);

        public event FaceDetected OnFaceDetected;

        private void updateFaceDetected(Rectangle rect)
        {
            if (OnFaceDetected != null)
            {
                OnFaceDetected(this, new Face(rect));
            }
        }
        #endregion

        #region FRAME_RECEIVED
        public delegate void FrameReceived(object sender, Frame frame);

        public event FrameReceived OnFrameReceived;

        private void updateFrameReceived(Bitmap bmp)
        {
            if (OnFrameReceived != null)
            {
                OnFrameReceived(this, new Frame(bmp));
            }
        }
        #endregion

        #region ERROR_OCCURED
        public delegate void ErrorOccured(object sender, SensorError error);

        public event ErrorOccured OnSensorError;

        private void updateError(String error, Exception ex = null)
        {
            if (OnSensorError != null)
            {
                OnSensorError(this, new SensorError(error, ex));
            }
        }
        #endregion

        #region MOTION
        public delegate void MotionDetected(object sender, bool IsMotion);
        public event MotionDetected OnMotionDetected;
        private void updateMotion(bool isMotion)
        {
            if(OnMotionDetected != null)
            {
                OnMotionDetected(this, isMotion);
            }
        }
        #endregion
        #endregion

        #region FIELDS_AND_PROPS
        private bool isLocalCamera = true;
        private int localCameraIndex = 0;
        private IVideoSource video = null;
        private Bitmap bmp = null;
        private String cameraUrl;
        private String cameraLogin;
        private String cameraPassword;
        private MotionDetector motion;


        #endregion

        #region CONSTRUCTORS

        public Sensor()
        {
            this.localCameraIndex = 0;
            this.isLocalCamera = true;
            this.init();
        }

        public Sensor(int localCameraIndex)
        {
            this.localCameraIndex = localCameraIndex;
            this.isLocalCamera = true;
            this.init();
        }

        public Sensor(String url, String login = "", String password = "")
        {
            this.isLocalCamera = false;
            this.cameraUrl = url;
            this.cameraLogin = "";
            this.cameraPassword = "";
            this.init();
        }

        public void WakeUp()
        {
            try
            {
                video.Start();
            }
            catch (Exception ex)
            {
                updateError("Brak połączenia z kamerą");
            }
        }

        public void GoSleep()
        {
            try
            {
                 video.SignalToStop();
            }
            catch (Exception ex)
            {
                updateError("Brak połączenia z kamerą");
            }
        }

        private void init()
        {
            try
            {
                if (isLocalCamera)
                {
                    var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    video = new VideoCaptureDevice(videoDevices[localCameraIndex].MonikerString);
                }
                else
                {
                    video = new MJPEGStream(this.cameraUrl);
                    ((MJPEGStream)video).Login = this.cameraLogin;
                    ((MJPEGStream)video).Password = this.cameraPassword;
                }
                video.NewFrame += new NewFrameEventHandler(processFrame);
                video.VideoSourceError += new VideoSourceErrorEventHandler(processFrameError);

                motion = new MotionDetector(
                    new SimpleBackgroundModelingDetector( ),
                    new MotionAreaHighlighting( ) 
                );

            }
            catch (Exception ex)
            {
                updateError(ex.Message);
            }
        }

        private void processFrameError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            updateError(eventArgs.Description);
        }

        private void processFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Rectangle rect = new Rectangle(0, 0, eventArgs.Frame.Width, eventArgs.Frame.Height);
            Bitmap frame = eventArgs.Frame.Clone(rect, eventArgs.Frame.PixelFormat);
            BitmapData data = frame.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, frame.PixelFormat);
            UnmanagedImage ui = new UnmanagedImage(data);

            if (OnMotionDetected != null && motion.ProcessFrame(ui) > 0.15)
            {
                updateMotion(true);                
            }
            else
            {
                updateMotion(false);
            }

            frame.UnlockBits(data);

            updateFrameReceived(frame);
            

        }
        #endregion
    }
}
