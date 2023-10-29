using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

public class QuantumExperiment
{
    // Nested Struct for PhotonPath
    private struct PhotonPath
    {
        public string PathName { get; set; }
        public Polyline Polyline { get; set; }
    }

    // Constants for Animation and Photon Characteristics
    private const double ANIMATION_FPS = 60.0;
    private const double PHOTON_SIZE = 10;
    private const double PHOTON_VELOCITY = 0.8;
    private const double PHOTON_ACTUAL_PATH_THICKNESS = 3.0;
    private const double PHOTON_SUPERPOSITION_PATH_THICKNESS = 2.0;

    // Constants for Experiment Component Positions and Dimensions
    private const double INITIAL_PHOTON_X = 100;
    private const double INITIAL_PHOTON_Y = 300;
    private const double BEAM_SPLITTER_X = 200;
    private const double BEAM_SPLITTER_Y = 300;
    private const double BEAM_SPLITTER_WIDTH = 5; 
    private const double BEAM_SPLITTER_HEIGHT = 20;
    private const double BOMB_X = 300;
    private const double BOMB_Y = 300;
    private const double BOMB_SIZE = 20;
    private const double TOP_MIRROR_X = 200;
    private const double TOP_MIRROR_Y = 150;
    private const double BOTTOM_MIRROR_X = 500;
    private const double BOTTOM_MIRROR_Y = 300;
    private const double MIRROR_WIDTH = 5;
    private const double MIRROR_HEIGHT = 20;
    private const double RECOMBINATOR_X = 500;
    private const double RECOMBINATOR_Y = 150;
    private const double DETECTOR_A_X = 600;
    private const double DETECTOR_A_Y = 150;
    private const double DETECTOR_B_X = 500;
    private const double DETECTOR_B_Y = 50;
    private const double DETECTOR_SIZE = 20;

    // Experiment components
    // The '?' denotes nullable types, allowing these references to be null, offering flexibility during initialization and runtime scenarios.
    // The '_' prefix is a naming convention for private fields in C#, though other conventions like 'm_' or no prefix are also common.
    private Bomb? _bomb;
    private Photon? _initialPhoton;
    private Photon? _upperPhoton;
    private Photon? _lowerPhoton;
    private Photon? _recombinedPhoton;
    private RenderableEntity? _beamSplitter;
    private RenderableEntity? _upperPathMirror;
    private RenderableEntity? _lowerPathMirror;
    private RenderableEntity? _recombinator;
    private Detector? _detectorA;
    private Detector? _detectorB;

    // Experiment state
    private bool _isExperimentWithBomb;
    private bool _didPhotonActuallyTakeLowerPath;

    // Experiment trackers and utilities
    private int _photonsArrivedCounter;
    private DispatcherTimer? _waitForOtherPhotonTimer;
    private Canvas _canvas;
    private List<PhotonPath> _photonPaths;
    private Random _random;

    public QuantumExperiment(Canvas canvas)
    {
        _canvas = canvas;
        _photonPaths = new List<PhotonPath>();
        _random = new Random();
    }

    public void Reset()
    {
        // Clear the canvas
        _canvas.Children.Clear();

        // Clear the photons' paths list
        _photonPaths.Clear();

        // Clear the arrived photons counter
        _photonsArrivedCounter = 0;

        // Stop and reset timer
        if (_waitForOtherPhotonTimer != null)
        {
            _waitForOtherPhotonTimer.Stop();
            _waitForOtherPhotonTimer = null;
        }

        // (Optional) Add other cleanup or resetting of variables if necessary.
    }

    public void Run(bool isExperimentWithBomb)
    {
        // Reset the state of the experiment.
        Reset();

        // Set the state of the experiment based on whether it includes the bomb or not.
        _isExperimentWithBomb = isExperimentWithBomb;

        // Randomly decide if the photon takes the lower path.
        _didPhotonActuallyTakeLowerPath = _random.Next(2) == 0;

        // Set up the initial entities and components for the experiment.
        AddPhotonSourceSymbol(new Point(INITIAL_PHOTON_X, INITIAL_PHOTON_Y));

        _initialPhoton = AddPhoton(new Point(INITIAL_PHOTON_X, INITIAL_PHOTON_Y), isInSuperposition: false);
        _beamSplitter = AddBeamSplitter(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y));

        // If the experiment includes a bomb, add it. Randomly decide if it's a dud.
        if (_isExperimentWithBomb)
        {
            _bomb = AddBomb(isDud: _random.Next(2) == 0);
        }

        // Add the remaining components for the experiment.
        _upperPathMirror = AddMirror(new Point(TOP_MIRROR_X, TOP_MIRROR_Y));
        _lowerPathMirror = AddMirror(new Point(BOTTOM_MIRROR_X, BOTTOM_MIRROR_Y));
        _recombinator = AddBeamSplitter(new Point(RECOMBINATOR_X, RECOMBINATOR_Y));
        _detectorA = AddDetector(new Point(DETECTOR_A_X, DETECTOR_A_Y), Colors.Green);
        _detectorB = AddDetector(new Point(DETECTOR_B_X, DETECTOR_B_Y), Colors.Red);

        // Begin the experiment by moving the initial photon towards the beam splitter.
        MoveInitialPhotonToBeamSplitter(() =>
        {
            Debug.WriteLine("Photon reached the beam splitter!");

            // Once the photon reaches the beam splitter, it is replaced with two photons in quantum superposition.
            _initialPhoton.Kill();
            _upperPhoton = AddPhoton(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y), isInSuperposition: true);
            _lowerPhoton = AddPhoton(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y), isInSuperposition: true);

            // Move the photon along the upper path.
            MoveUpperPhotonToMirror(() =>
            {
                Debug.WriteLine("Upper photon reached the mirror!");

                MoveUpperPhotonToRecombinator(() =>
                {
                    Debug.WriteLine("Upper photon reached the recombinator!");

                    PhotonArrivedAtRecombinator();
                });
            });

            // If the experiment includes a bomb, move the photon along the lower path to interact with the bomb.
            if (_isExperimentWithBomb)
            {
                MoveLowerPhotonToBomb(() =>
                {
                    Debug.WriteLine("Lower photon reached the bomb!");

                    MoveLowerPhotonFromBombToMirror(() =>
                    {
                        Debug.WriteLine("Lower photon reached the mirror!");

                        MoveLowerPhotonToRecombinator(() =>
                        {
                            Debug.WriteLine("Lower photon reached the recombinator!");

                            PhotonArrivedAtRecombinator();
                        });
                    });
                });
            }
            else
            {
                // If no bomb, simply move the photon along the lower path.
                MoveLowerPhotonFromBeamSplitterToMirror(() =>
                {
                    Debug.WriteLine("Lower photon reached the mirror!");

                    MoveLowerPhotonToRecombinator(() =>
                    {
                        Debug.WriteLine("Lower photon reached the recombinator!");

                        PhotonArrivedAtRecombinator();
                    });
                });
            }
        });
    }

    private void MoveInitialPhotonToBeamSplitter(Action callback)
    {
        AnimatePhoton(_initialPhoton!, _beamSplitter!.GetCentroid(), (_beamSplitter.CentroidX - _initialPhoton!.CentroidX) / PHOTON_VELOCITY, "initial", callback);
    }

    private void MoveUpperPhotonToMirror(Action callback)
    {
        // NOTE: In WPF, Y increases top-to-bottom; the (-1) factor adjusts for this coordinate direction.
        AnimatePhoton(_upperPhoton!, _upperPathMirror!.GetCentroid(), (-1) * (_upperPathMirror.CentroidY - _beamSplitter!.CentroidY) / PHOTON_VELOCITY, "upper", callback);
    }

    private void MoveLowerPhotonToBomb(Action callback)
    {
        AnimatePhoton(_lowerPhoton!, _bomb!.GetCentroid(), (_bomb.CentroidX - _beamSplitter!.CentroidX) / PHOTON_VELOCITY, "lower", callback);
    }

    private void MoveUpperPhotonToRecombinator(Action callback)
    {
        AnimatePhoton(_upperPhoton!, _recombinator!.GetCentroid(), (_recombinator.CentroidX - _upperPathMirror!.CentroidX) / PHOTON_VELOCITY, "upper", callback);
    }

    private void MoveLowerPhotonFromBeamSplitterToMirror(Action callback)
    {
        AnimatePhoton(_lowerPhoton!, _lowerPathMirror!.GetCentroid(), (_lowerPathMirror!.CentroidX - _beamSplitter!.CentroidX) / PHOTON_VELOCITY, "lower", callback);
    }

    private void MoveLowerPhotonFromBombToMirror(Action callback)
    {
        if (_didPhotonActuallyTakeLowerPath && _bomb!.IsBombLive)
        {
            // If the photon takes the lower path and the bomb is live, the bomb explodes.
            Debug.WriteLine("The bomb exploded!");
            _bomb.Kill();
            _lowerPhoton!.Kill();
            _upperPhoton!.Kill();
            _upperPhoton.IsDead = true;

            // Update the visual representation to show that the upper photon path is now inactive.
            _canvas.Children.Remove(_photonPaths.Where(p => p.PathName == "upper").Last().Polyline);
            var polyline = _photonPaths.Where(p => p.PathName == "lower").Last().Polyline;
            polyline.Stroke = Brushes.Magenta;
            polyline.StrokeThickness = PHOTON_ACTUAL_PATH_THICKNESS;
            polyline.StrokeDashArray = null;
        }
        else
        {
            // If the bomb doesn't explode, animate the photon moving from the bomb to the mirror.
            AnimatePhoton(_lowerPhoton!, _lowerPathMirror!.GetCentroid(), (_lowerPathMirror!.CentroidX - _bomb!.CentroidX) / PHOTON_VELOCITY, "lower", callback);
        }
    }

    private void MoveLowerPhotonToRecombinator(Action callback)
    {
        AnimatePhoton(_lowerPhoton!, _recombinator!.GetCentroid(), (-1) * (_recombinator!.CentroidY - _lowerPathMirror!.CentroidY) / PHOTON_VELOCITY, "lower", callback);
    }

    private void PhotonArrivedAtRecombinator()
    {
        _photonsArrivedCounter++;

        if (_photonsArrivedCounter == 1) 
        {
            // Handle the first photon's arrival at the recombinator.

            // Ensure that both photons arrive closely synchronized. A 100ms window is set for this purpose.
            _waitForOtherPhotonTimer = new DispatcherTimer();
            _waitForOtherPhotonTimer.Interval = TimeSpan.FromMilliseconds(100);

            // If only one photon arrives within this window, an exception is thrown.
            _waitForOtherPhotonTimer.Tick += (sender, args) => throw new Exception("Only one photon arrived at the recombinator within the time frame!"); 
            _waitForOtherPhotonTimer.Start();
        }
        else if (_photonsArrivedCounter == 2) // Second photon arrived
        {
            // Handle the second photon's arrival.

            Debug.WriteLine("Both photons reached the recombinator!");

            // Stop the timer, both photons have arrived.
            _waitForOtherPhotonTimer?.Stop();
            _waitForOtherPhotonTimer = null;

            if (_isExperimentWithBomb)
            {
                _photonsArrivedCounter = 0;

                // In a bomb experiment, photon paths remain uncertain until detected.
                MoveLowerPhotonToDetectorA(PhotonArrivedAtDetector);
                MoveUpperPhotonToDetectorB(PhotonArrivedAtDetector);
            }
            else
            {
                // In the absence of a bomb, photons undergo specific interference patterns:
                // - Destructive interference towards detector B
                // - Constructive interference towards detector A
                _upperPhoton!.Kill();
                _lowerPhoton!.Kill();

                _recombinedPhoton = AddPhoton(_recombinator!.GetCentroid(), isInSuperposition: false);

                MoveRecombinedPhotonToDetectorA(() =>
                {
                    _recombinedPhoton!.Kill();
                    _detectorA!.HasDetectedPhoton = true;
                });
            }
        }
    }

    public void MoveUpperPhotonToDetectorB(Action callback)
    {
        AnimatePhoton(_upperPhoton!, _detectorB!.GetCentroid(), (-1) * (_detectorB!.CentroidY - _recombinator!.CentroidY) / PHOTON_VELOCITY, "upper", callback);
    }

    public void MoveLowerPhotonToDetectorA(Action callback)
    {
        AnimatePhoton(_lowerPhoton!, _detectorA!.GetCentroid(), (_detectorA!.CentroidX - _recombinator!.CentroidX) / PHOTON_VELOCITY, "lower", callback);
    }

    public void MoveRecombinedPhotonToDetectorA(Action callback)
    {
        AnimatePhoton(_recombinedPhoton!, _detectorA!.GetCentroid(), (_detectorA!.CentroidX - _recombinator!.CentroidX) / PHOTON_VELOCITY, "recombined", callback);
    }

    private void PhotonArrivedAtDetector()
    {
        _photonsArrivedCounter++;

        if (_photonsArrivedCounter == 1)  
        {
            // First photon arrival handling:

            // Consider refactoring the timer setup into a dedicated method for clarity and to adhere to the DRY principle.
            _waitForOtherPhotonTimer = new DispatcherTimer();
            _waitForOtherPhotonTimer.Interval = TimeSpan.FromMilliseconds(100);

            // Throw an exception if only one photon arrives within the stipulated time frame.
            _waitForOtherPhotonTimer.Tick += (sender, args) => throw new Exception("Only one photon arrived within the time frame!");
            _waitForOtherPhotonTimer.Start();
        }
        else if (_photonsArrivedCounter == 2) // Second photon arrived
        {
            // Second photon arrival handling:

            Debug.WriteLine("Both photons reached the recombinator!");

            // Disabling the timer since both photons have now arrived.
            _waitForOtherPhotonTimer?.Stop();
            _waitForOtherPhotonTimer = null;

            _upperPhoton!.Kill();
            _lowerPhoton!.Kill();

            // Photon behavior is influenced by the bomb's state.
            if (_bomb!.IsBombLive == false)
            {
                // In the case of a dud bomb, the photon behaves as if the bomb is absent and consistently gets detected at Detector A.
                DetectedAtDetectorA();
            }
            else
            {
                // For a live bomb in the upper path:
                // - Detection at Detector A doesn't confirm the bomb's state.
                // - Detection at Detector B conclusively confirms the bomb is live.
                // Hence, there's a 50-50 detection between Detectors A and B.
                if (_random.Next(2) == 0)
                {
                    DetectedAtDetectorA();
                }
                else
                {
                    DetectedAtDetectorB();
                }
            }
        }
    }

    private void DetectedAtDetectorA()
    {
        Debug.WriteLine("Detected at A, you don't know if the bomb is live or a dud");

        _detectorA!.HasDetectedPhoton = true;

        _canvas.Children.Remove(_photonPaths.Where(p => p.PathName == "upper").Last().Polyline);
        var polyline = _photonPaths.Where(p => p.PathName == "lower").Last().Polyline;
        polyline.Stroke = Brushes.Magenta;
        polyline.StrokeThickness = PHOTON_ACTUAL_PATH_THICKNESS;
        polyline.StrokeDashArray = null;
    }

    private void DetectedAtDetectorB()
    {
        Debug.WriteLine("Detected at B, bomb is LIVE and didn't explode. Photon took the upper path, showcasing quantum behavior!");

        // Update the visual representation to indicate the photon's path.

        _detectorB!.HasDetectedPhoton = true;

        foreach (PhotonPath photonPath in _photonPaths.Where(p => p.PathName == "lower"))
        {
            _canvas.Children.Remove(photonPath.Polyline);
        }

        foreach (PhotonPath photonPath in _photonPaths.Where(p => p.PathName == "upper"))
        {
            photonPath.Polyline.Stroke = Brushes.Magenta;
            photonPath.Polyline.StrokeThickness = PHOTON_ACTUAL_PATH_THICKNESS;
            photonPath.Polyline.StrokeDashArray = null;
        }
    }

    private RenderableEntity AddPhotonSourceSymbol(Point centroid)
    {
        double width = 20;
        double height = 10;
        double trapezoidWidth = 10;

        // Create PathGeometry
        PathGeometry pathGeometry = new PathGeometry();

        PathFigure figure = new PathFigure();
        figure.StartPoint = new Point(0, 0);
        figure.Segments.Add(new LineSegment(new Point(width, 0), true));
        figure.Segments.Add(new LineSegment(new Point(width + trapezoidWidth, -height / 2), true));
        figure.Segments.Add(new LineSegment(new Point(width, -height), true));
        figure.Segments.Add(new LineSegment(new Point(0, -height), true));
        figure.IsClosed = true;

        pathGeometry.Figures.Add(figure);

        // Create Path
        Path sourcePath = new Path();
        sourcePath.Data = pathGeometry;
        sourcePath.Fill = Brushes.Magenta;

        // Translate to centroid position
        double translateX = centroid.X - (width + trapezoidWidth) / 2;  // Adjusting for width to make sure centroid is in the center
        double translateY = centroid.Y + height / 2;  // Adjusting for height
        sourcePath.RenderTransform = new TranslateTransform(translateX, translateY);

        return new RenderableEntity(sourcePath, _canvas, centroid.X, centroid.Y);
    }

    private Photon AddPhoton(Point centroid, bool isInSuperposition)
    {
        Ellipse photon = new Ellipse
        {
            Width = PHOTON_SIZE,
            Height = PHOTON_SIZE,
            Fill = isInSuperposition ? new SolidColorBrush(LightenColor(Brushes.Magenta.Color, 0.6)) : Brushes.Magenta
        };

        return new Photon(photon, _canvas, centroid.X, centroid.Y, isInSuperposition);
    }

    private Color LightenColor(Color originalColor, double factor)
    {
        return Color.FromArgb(
            originalColor.A,
            (byte)Math.Min(255, originalColor.R + (255 - originalColor.R) * factor),
            (byte)Math.Min(255, originalColor.G + (255 - originalColor.G) * factor),
            (byte)Math.Min(255, originalColor.B + (255 - originalColor.B) * factor)
        );
    }

    private Bomb AddBomb(bool isDud)
    {
        Ellipse bomb = new Ellipse
        {
            Width = BOMB_SIZE,
            Height = BOMB_SIZE,
            Fill = Brushes.Black
        };

        Panel.SetZIndex(bomb, 1); // Set it to a higher value to bring it to the foreground

        return new Bomb(bomb, _canvas, BOMB_X, BOMB_Y, isDud);
    }

    private RenderableEntity AddBeamSplitter(Point centroid)
    {
        Rectangle beamSplitter = new Rectangle
        {
            Width = BEAM_SPLITTER_WIDTH,
            Height = BEAM_SPLITTER_HEIGHT,
            Fill = Brushes.LightGreen
        };

        // Rotate the beam splitter
        RotateTransform rotateTransform = new RotateTransform(45, BEAM_SPLITTER_WIDTH / 2, BEAM_SPLITTER_HEIGHT / 2);
        beamSplitter.RenderTransform = rotateTransform;

        return new RenderableEntity(beamSplitter, _canvas, centroid.X, centroid.Y);
    }

    private RenderableEntity AddMirror(Point centroid)
    {
        Rectangle mirror = new Rectangle
        {
            Width = MIRROR_WIDTH,
            Height = MIRROR_HEIGHT,
            Fill = Brushes.Purple
        };

        // Rotate the mirror
        RotateTransform rotateTransform = new RotateTransform(45, MIRROR_WIDTH / 2, MIRROR_HEIGHT / 2);
        mirror.RenderTransform = rotateTransform;

        return new RenderableEntity(mirror, _canvas, centroid.X, centroid.Y);
    }

    private Detector AddDetector(Point centroid, Color color)
    {
        Ellipse mirror = new Ellipse
        {
            Width = DETECTOR_SIZE,
            Height = DETECTOR_SIZE,
            Fill = new SolidColorBrush(LightenColor(color, 0.8))
        };

        Canvas.SetZIndex(mirror, 1); // Set it to a higher value to bring it to the foreground

        return new Detector(mirror, _canvas, centroid.X, centroid.Y, color);
    }

    private void AnimatePhoton(Photon photon, Point targetPosition, double durationMilliseconds, string pathName, Action? onComplete = null)
    {
        // This timer will fire every frame. At 60.0 FPS, this is approximately one frame every 16.7 milliseconds.
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / ANIMATION_FPS) 
        };

        Point startPosition = photon.GetCentroid();

        var stopwatch = new Stopwatch();

        // Create a new Polyline for the photon's path (dashed line)
        var polyline = new Polyline
        {
            Stroke = photon.IsInSuperposition ? new SolidColorBrush(LightenColor(Brushes.Magenta.Color, 0.6)) : Brushes.Magenta,
            StrokeThickness = photon.IsInSuperposition ? PHOTON_SUPERPOSITION_PATH_THICKNESS : PHOTON_ACTUAL_PATH_THICKNESS,
            StrokeDashArray = photon.IsInSuperposition ? new DoubleCollection { 1.5, 2.0 } : null
        };
        
        _photonPaths.Add(new PhotonPath { PathName = pathName, Polyline = polyline });
        
        _canvas.Children.Add(polyline);

        // === TIMER TICK REGISTRATION START ===
        timer.Tick += (sender, args) =>
        {
            // Check if the photon has been destroyed or is no longer active
            if (photon.IsDead)
            {
                ((DispatcherTimer)sender!).Stop(); // Stop the animation timer
                stopwatch.Stop();                  // Stop the stopwatch
                return;
            }

            // Calculate the proportion of the animation's total duration that has elapsed (capped at 1.0)
            double progress = Math.Min(1.0, stopwatch.ElapsedMilliseconds / durationMilliseconds);

            // Calculate the photon's new position based on the progress
            double newX = startPosition.X + (targetPosition.X - startPosition.X) * progress;
            double newY = startPosition.Y + (targetPosition.Y - startPosition.Y) * progress;

            // TODO: Replace individual centroid assignments with a SetCentroid method for clarity and encapsulation.
            photon.CentroidX = newX;
            photon.CentroidY = newY;

            // Append the photon's current position to the polyline
            var newPoint = new Point(newX, newY);
            polyline.Points.Add(newPoint);

            // If the animation has completed (or exceeded its intended duration)
            if (progress >= 1.0)
            {
                ((DispatcherTimer)sender!).Stop(); // Stop the animation timer
                stopwatch.Stop();                  // Stop the stopwatch

                onComplete?.Invoke(); // Signal that the animation has finished
            }
        };
        // === TIMER TICK REGISTRATION END ===

        // Note: The order between the TIMER TICK REGISTRATION block and the TIMER & STOPWATCH STARTING SECTION 
        // does not affect the behavior of the code. This illustrates the asynchronous nature of the DispatcherTimer 
        // and how event subscriptions work in C#.

        // === TIMER & STOPWATCH STARTING SECTION START ===
        stopwatch.Start(); // Start measuring elapsed time for animation progress.
        timer.Start();     // Start the timer, which will trigger the Tick event based on the defined interval.
        // === TIMER & STOPWATCH STARTING SECTION END ===
    }

    private class RenderableEntity
    {
        private Shape _shape;
        private Canvas _canvas;

        private double _centroidX;
        private double _centroidY;

        public Shape Shape
        {
            get => _shape;
            private set
            {
                _shape = value;
                UpdatePosition();
            }
        }

        public double CentroidX
        {
            get => _centroidX;
            set
            {
                _centroidX = value;
                UpdatePosition();
            }
        }

        public double CentroidY
        {
            get => _centroidY;
            set
            {
                _centroidY = value;
                UpdatePosition();
            }
        }

        public RenderableEntity(Shape shape, Canvas canvas, double x, double y)
        {
            _shape = shape;
            _canvas = canvas;
            _centroidX = x;
            _centroidY = y;

            _canvas.Children.Add(_shape);
            UpdatePosition();
        }

        public Point GetCentroid()
        {
            double x = Canvas.GetLeft(_shape) + _shape.Width / 2;
            double y = Canvas.GetTop(_shape) + _shape.Height / 2;
            return new Point(x, y);
        }

        public void Kill()
        {
            if (_canvas != null && _shape != null)
            {
                _canvas.Children.Remove(_shape);
            }
        }

        private void UpdatePosition()
        {
            Canvas.SetLeft(_shape, _centroidX - _shape.Width / 2);
            Canvas.SetTop(_shape, _centroidY - _shape.Height / 2);
        }
    }

    private class Photon : RenderableEntity
    {
        private bool _isInSuperposition;
        private bool _isDead;

        public Photon(Shape shape, Canvas canvas, double x, double y, bool isInSuperposition) : base(shape, canvas, x, y)
        {
            _isInSuperposition = isInSuperposition;
        }

        public bool IsInSuperposition
        {
            get { return _isInSuperposition; }
        }

        public bool IsDead
        {
            get { return _isDead; }
            set { _isDead = value; }
        }
    }

    private class Bomb : RenderableEntity
    {
        private bool _isBombLive;

        public Bomb(Shape shape, Canvas canvas, double x, double y, bool isBombLive) : base(shape, canvas, x, y)
        {
            _isBombLive = isBombLive;
        }

        public bool IsBombLive
        {
            get { return _isBombLive; }
        }
    }

    // TODO: Manage both lightened and original colors within the `Detector` class.
    private class Detector : RenderableEntity
    {
        private Color _color;
        private bool _hasDetectedPhoton;

        public Detector(Shape shape, Canvas canvas, double x, double y, Color color) : base(shape, canvas, x, y)
        {
            _color = color;
            _hasDetectedPhoton = false;
        }

        public bool HasDetectedPhoton
        {
            get { return _hasDetectedPhoton; }
            set
            {
                _hasDetectedPhoton = value;
                UpdateColor();
            }
        }

        private void UpdateColor()
        {
            if (_hasDetectedPhoton)
            {
                // NOTE: The initial color of the detector is a lightened version of the provided color. 
                // Once a photon is detected, the detector assumes its original, darker shade.
                base.Shape.Fill = new SolidColorBrush(_color);
            }
        }
    }
}
