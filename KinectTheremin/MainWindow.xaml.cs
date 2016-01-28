using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.CompilerServices;
using Microsoft.Kinect;

using KinectTheremin.ThereminAudio;

namespace KinectTheremin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private KinectSensor _kinect;
        private MultiSourceFrameReader _frameReader;
        private AudioPlaybackThread _playbackThread;

        private const double MAX_HAND_DISTANCE = 1.0;
        private const double MIN_GAIN = .1;
        private const double METERS_PER_SEMITONE = .05;
        private const double MIN_FREQUENCY = 200;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new KinectPositionContext();

            this.Loaded += new RoutedEventHandler(this.MainWindow_Loaded);
            this.Closing += new CancelEventHandler(this.MainWindow_Closing);
            //kinect.IsAvailableChanged += new TypedEventHandler(this.MainWindow_KinectStatusChanged);
        }

        protected void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Initialize the Kinect
            _kinect = KinectSensor.GetDefault();
            _kinect.Open();
            
            //If the sensor is ready, update the UI
            if (_kinect.IsOpen)
            {
                kinectStatusLabel.Content = "READY";
                kinectStatusLabel.Foreground = new SolidColorBrush(Colors.Green);

                _frameReader = _kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Infrared | FrameSourceTypes.Depth | FrameSourceTypes.Color);
                _frameReader.MultiSourceFrameArrived += MainWindow_KinectFrameArrived;
            }
            else
            {
                Debug.WriteLine("Kinect not working!");
            }

            _playbackThread = new AudioPlaybackThread();
            _playbackThread.StartAudioPlayback();
        }

        protected void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _playbackThread.StopAudioPlayback();
        }

        protected void MainWindow_KinectFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //Retrieve a frame from the sensor
            var frameRef = e.FrameReference.AcquireFrame();

            using (var frame = frameRef.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    var bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);

                    Body trackedBody = null;

                    //Get the first body we're actually tracking
                    foreach (var body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            trackedBody = body;
                            break;
                        }
                    }

                    if (trackedBody != null)
                    {
                        //Rretrieve the positions of the two hands
                        var uiContext = (KinectPositionContext)this.DataContext;

                        var leftHandJoint = trackedBody.Joints[JointType.HandLeft];
                        var rightHandJoint = trackedBody.Joints[JointType.HandRight];

                        //Set the data in the UI context so that the interface updates
                        uiContext.LeftHandX = leftHandJoint.Position.X;
                        uiContext.LeftHandY = leftHandJoint.Position.Y;
                        uiContext.LeftHandZ = leftHandJoint.Position.Z;

                        uiContext.RightHandX = rightHandJoint.Position.X;
                        uiContext.RightHandY = rightHandJoint.Position.Y;
                        uiContext.RightHandZ = rightHandJoint.Position.Z;

                        //Calculate the distance between the hands 
                        //(changes the volume of the sine wave)
                        var leftX = leftHandJoint.Position.X;
                        var leftY = leftHandJoint.Position.Y;
                        var leftZ = leftHandJoint.Position.Z;

                        var rightX = rightHandJoint.Position.X;
                        var rightY = rightHandJoint.Position.Y;
                        var rightZ = rightHandJoint.Position.Z;

                        var dX = Math.Abs(rightX - leftX);
                        var dY = Math.Abs(rightY - leftY);
                        var dZ = Math.Abs(rightZ - leftZ);

                        var dist = Math.Sqrt(dX * dX + dY * dY + dZ * dZ);

                        var gain = dist / MAX_HAND_DISTANCE;

                        if (gain > 1.0)
                        {
                            gain = 1.0;
                        }
                        else if (gain <= MIN_GAIN)
                        {
                            gain = 0;
                        }

                        //Calculate the frequency to use
                        var handAvgHeight = (rightY + leftY) / 2;

                        //Hand height scales linearally by semitones (by default every .1m is a semitone)
                        var frequency = MIN_FREQUENCY * Math.Pow(2, (handAvgHeight / METERS_PER_SEMITONE) / 12);

                        if (frequency < MIN_FREQUENCY)
                        {
                            frequency = MIN_FREQUENCY;
                        }

                        _playbackThread.Frequency = frequency;
                        _playbackThread.Gain = gain;
                    }                   
                }
            }
        }
        
    }

    public class KinectPositionContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private double _leftHandX;
        private double _leftHandY;
        private double _leftHandZ;

        private double _rightHandX;
        private double _rightHandY;
        private double _rightHandZ;

        //Left Hand position properties
        public double LeftHandX
        {
            get {
                return _leftHandX;
            }
            set
            {
                _leftHandX = value;
                OnPropertyChanged();
            }
        }

        public double LeftHandY
        {
            get
            {
                return _leftHandY;
            }
            set
            {
                _leftHandY = value;
                OnPropertyChanged();
            }
        }

        public double LeftHandZ
        {
            get
            {
                return _leftHandZ;
            }
            set
            {
                _leftHandZ = value;
                OnPropertyChanged();
            }
        }

        public double RightHandX
        {
            get
            {
                return _rightHandX;
            }
            set
            {
                _rightHandX = value;
                OnPropertyChanged();
            }
        }

        //Right hand properties
        public double RightHandY
        {
            get
            {
                return _rightHandY;
            }
            set
            {
                _rightHandY = value;
                OnPropertyChanged();
            }
        }

        public double RightHandZ
        {
            get
            {
                return _rightHandZ;
            }
            set
            {
                _rightHandZ = value;
                OnPropertyChanged();
            }
        }


        //Handler for when Kinect events set position
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var changeHandler = PropertyChanged;

            if (changeHandler != null)
            {
                changeHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    
}
