using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using CircularBuffer;

namespace VRTracker.Manager
{
    /// <summary>
    /// VR Tracker
    /// Represent a Tracker Endpoint composed or an IMU or IMU + LED or IMU + vib
    /// This is to extend Tag functionnality when it has multiple IMU / IR connected for 
    /// hand tracking and body tracking
    /// </summary>
    public class VRT_TagEndpoint
    {
        // For Quaternion orientation from Tag
        protected bool orientationUsesQuaternion = false;
        protected Quaternion orientation_quat; // Tag V2 and above
        protected Vector3 orientation_; // Tag V1 and old V2 (not udpated)
        protected Vector3 acceleration_;

        private long initialTimeMs = 0; // Time at start in milliseconds

        private double imuTimestampModulo = 6.5536; // (16 bits)
        private int imuTimestampModuloCount = 0; // Count how many time we should add the modulo value have the time now
        private double imuTimestamp = 0;
        private CircularBuffer<double> imuTimestampOffsetBuffer; // Buffer to calculate a moving average offset between IMU timestamp and system time
        private double imuTimestampOffsetAvg = 0;
        private double positionTimestamp = 0;
        private double positionTimestampOffset = 0;
        private CircularBuffer<double> positionTimestampOffsetBuffer;

        protected Vector3 positionReceived;     //Position received from VR Tracker system

        private bool positionFilter = true; // Check to enable position filtering
        protected VRTracker.Utils.VRT_PositionFilter filter;

        protected VRT_Tag parentTag; // The Tag to which this endpoint is connected to

        public delegate void PositionUpdate(Vector3 position);
        public delegate void OrientationUpdate(Quaternion orientation);
        public PositionUpdate positionUpdateHandler;
        public OrientationUpdate orientationUpdateHandler;

        // Use this for initialization
        public VRT_TagEndpoint(VRT_Tag tag)
        {
            imuTimestampOffsetBuffer = new CircularBuffer<double>(20);
            positionTimestampOffsetBuffer = new CircularBuffer<double>(20);

            parentTag = tag;
            // Get the time at start
            initialTimeMs = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;

            filter = new VRTracker.Utils.VRT_PositionFilter();
            filter.Init();
        }

        // Update is called once per frame
        public void Update()
        {
            if (positionFilter)
            {
                Vector3 newPosition = filter.GetPosition(((System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond) - initialTimeMs) / 1000.0d);
                if (newPosition != Vector3.zero && positionUpdateHandler != null)
                    positionUpdateHandler(newPosition);
            }
            else if (positionUpdateHandler != null)
                positionUpdateHandler(positionReceived);

            if(orientationUpdateHandler != null)
                orientationUpdateHandler(orientation_quat);
        }

        /// <summary>
        /// Gets the orientation of the tag
        /// </summary>
        /// <returns>The orientation.</returns>
        public Vector3 getOrientation()
        {
            if (orientationUsesQuaternion)
                return orientation_quat.eulerAngles;
            else
                return orientation_;
        }

        /// <summary>
        /// Gets the position received from the system
        /// </summary>
        /// <returns>The position.</returns>
        public Vector3 GetPosition()
        {
            return this.positionReceived;
        }

        /// <summary>
        /// Updates the position and add the timestamp
        /// Currently not used
        /// </summary>
        /// <param name="position_">Position.</param>
        /// <param name="timestamp">Timestamp.</param>
        public void UpdatePosition(Vector3 position_, int timestamp)
        {
            UpdatePosition(position_);
        }

        /// <summary>
        /// Updates the position and store the data
        /// </summary>
        /// <param name="position_">Position.</param>
        public void UpdatePosition(Vector3 position_)
        {
            // PREDICTION
            this.positionReceived = position_;
            if (positionFilter)
                filter.AddPositionMeasurement(((System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond) - initialTimeMs) / 1000.0d, position_);
        }

        /// <summary>
        /// Updates the Oriention from IMU For Tag V1
        /// </summary>
        /// <param name="neworientation">Neworientation.</param>
        public void UpdateOrientation(Vector3 neworientation)
        {
            orientation_ = neworientation;
            orientationUsesQuaternion = false;
        }

        /// <summary>
        /// Updates the Oriention from IMU For Tag V2
        /// </summary>
        /// <param name="neworientation">Neworientation.</param>
        public void UpdateOrientationQuat(Quaternion neworientation)
        {
            orientationUsesQuaternion = true;
            orientation_quat = neworientation;
            orientation_quat = orientation_quat * Quaternion.Euler(180f, 0, 0);
            orientation_ = orientation_quat.eulerAngles;
        }


        /// <summary>
        /// Updates the orientation and acceleration from tag data
        /// </summary>
        /// <param name="neworientation">Neworientation.</param>
        /// <param name="newacceleration">Newacceleration.</param>
        public void UpdateOrientationAndAcceleration(Vector3 neworientation, Vector3 newacceleration)
        {
            //TODO: Review this
            Vector3 flippedRotation = new Vector3(-neworientation.z, neworientation.x + 90.0f, neworientation.y);

            Quaternion quattest = Quaternion.Euler(flippedRotation);
            quattest = quattest * Quaternion.Euler(180f, 0, 0);
            quattest = quattest * Quaternion.Euler(0, -90f, 0);
            orientation_ = quattest.eulerAngles;
            acceleration_ = newacceleration;
            orientationUsesQuaternion = false;
        }

        public void UpdateOrientationAndAcceleration(Quaternion neworientation, Vector3 newacceleration)
        {
            UpdateOrientationAndAcceleration(((System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond) - initialTimeMs) / 1000.0d, neworientation, newacceleration);
        }

        /// <summary>
        /// Updates the orientation using quaternion and acceleration from tag data
        /// </summary>
        /// <param name="neworientation">Neworientation.</param>
        /// <param name="newacceleration">Newacceleration.</param>
        public void UpdateOrientationAndAcceleration(double timestamp, Quaternion neworientation, Vector3 newacceleration)
        {
            orientationUsesQuaternion = true;
            orientation_quat = neworientation;


            //=================== START TIMESTAMP CORRECTION ===================
            // Handle timestamp correction and synchronisation
            // The IMU timestamp overflowed and went back to 0
            while (timestamp + (imuTimestampModuloCount * imuTimestampModulo) < imuTimestamp)
                imuTimestampModuloCount++;
            imuTimestamp = timestamp + (imuTimestampModuloCount * imuTimestampModulo);

            // Udpate Moving Average of system timestamp and IMU timestamp (mandatory to avoid clock drift issue, and sync all clock while avoid network timing jitter)
            double newImuTimestampOffset = (((System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond) - initialTimeMs) / 1000.0d) - imuTimestamp;
            imuTimestampOffsetBuffer.PushFront(newImuTimestampOffset);
            imuTimestampOffsetAvg = 0;

            //TODO: Use a better moving average algorithm like here : https://cheind.wordpress.com/2010/01/23/simple-moving-average/
            foreach (double ts in imuTimestampOffsetBuffer)
                imuTimestampOffsetAvg += ts;
            imuTimestampOffsetAvg /= imuTimestampOffsetBuffer.Size;
            imuTimestamp += imuTimestampOffsetAvg;

            //==================== END TIMESTAMP CORRECTION ====================

            // For TAG V3 only
            if (parentTag.tagVersion == VRT_Tag.TagVersion.V3)
                orientation_quat = new Quaternion(-neworientation.x, neworientation.y, -neworientation.z, neworientation.w);

            orientation_ = orientation_quat.eulerAngles;
            orientation_.y -= VRT_Manager.Instance.roomNorthOffset;
            orientation_quat = Quaternion.Euler(orientation_);

            // Convert acceleration axis
            // TAG V2
            if (parentTag.tagVersion == VRT_Tag.TagVersion.V2)
            {
                acceleration_ = new Vector3(newacceleration.x, newacceleration.z, newacceleration.y);
            }
            // TAG V3
            else if (parentTag.tagVersion == VRT_Tag.TagVersion.V3 || parentTag.tagVersion == VRT_Tag.TagVersion.Gun)
                acceleration_ = new Vector3(-newacceleration.x, newacceleration.z, -newacceleration.y);

            // Transform acceleration from local to world coordinate
            acceleration_ = orientation_quat * acceleration_;

            if (positionFilter)
                filter.AddAccelerationMeasurement(timestamp, acceleration_);
        }
    }
}