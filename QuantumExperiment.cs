using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

/**
 * The Elitzur-Vaidman bomb tester
 * 
 * Let's break it down step by step:
 * 
 * 1. Initialization: There's a 50/50 chance the bomb is live or a dud.
 * 
 * 2. Photon Path Selection: A photon can take either the upper or lower path with equal probability.
 * 
 * 3. Photon Takes Lower Path:
 * 
 *    • Bomb is Live: The photon triggers the bomb and gets detected there.
 * 
 *    • Bomb is Dud: The photon doesn't trigger anything and continues on its path.
 * 
 * 4. Photon Takes Upper Path:
 * 
 *    • After reflecting off the mirror on the upper path, it heads toward the recombinator (or second beam splitter).
 * 
 *    • Bomb is Dud: Due to quantum interference (as a result of the photon's superposition state with its counterpart on the lower path), the photon will always emerge toward detector A.
 * 
 *    • Bomb is Live: Since the lower-path photon was absorbed by the bomb, no interference occurs. Therefore, the upper photon has a 50/50 chance of going to either detector A or detector B when it reaches the recombinator.
 * 
 * 
 * 
 * TODO :
 * 
 * 1) Animation Mistake Alert: Currently, upon photon detection at detector B (the red one), we remove all dashed lines. This is incorrect. The segment of the dashed line between the initial beam splitter and the bomb should remain intact.
 * 
 * 2) If the bomb explodes, replace it with an explosion symbol
 * 
 * 3) Label Components: Add textual labels to various parts of the graphic. Examples include "Detector A", "Detector B", "Mirror", "Beam Splitter", "Photon Source", etc.
 * 
 * 4) Add a label above the bomb that shows either "Possibly a dud" or "It's live!" depending on whether it's detected in A or B, respectively
 * 
 * 5) Data Analysis Table: Set up a table outside the canvas to analyze the results. The table should have two columns: "Trial #" (a sequential number) and "Result" (possible values: "A", "B", "Boom!").
 * 
 * 6) Refactor Callbacks: Reduce the deeply nested callback functions by leveraging C# events. This will help improve the code's readability and maintainability.
 * 
 * 7) Add your own features. Use your creativity.
 * 
 * 8) Add guard clauses (i.e.: throw new InvalidOperationException) in order to remove all the non-null assertions (!) scattered throughout the code
 * 
 * 9) Make any other improvements to the readability of the code (separate portions of the code into different files perhaps, or refactor some parts of the code, etc...)
 * 
 * 10) User Interaction Lockout: Implement a mechanism to prevent users from triggering the experiment while it's running. Some potential solutions:
 *     • Disable the start button until the experiment concludes.
 *     • Display a visual indicator (like a loading spinner) to signify experiment progress.
 *     • Overlay a translucent layer over the experiment UI.
 *     • Show a warning pop-up if the user tries to interact mid-experiment.
 * 
 */

public class QuantumExperiment
{
    // Position values (X, Y) correspond to an entity's centroid
    private const double PHOTON_SIZE = 10;
    private const double PHOTON_VELOCITY = 0.3;

    private const double PHOTON_ACTUAL_PATH_THICKNESS = 3.0;
    private const double PHOTON_SUPERPOSITION_PATH_THICKNESS = 2.0;

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

    private bool _isExperimentWithBomb;
    private bool _didPhotonActuallyTakeLowerPath;
     
    private int _photonsArrivedCounter;
    private DispatcherTimer? _waitForOtherPhotonTimer;

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

    private Canvas _canvas;
    private Random _random = new Random();

    public QuantumExperiment(Canvas canvas)
    {
        _canvas = canvas;
        _recombinator = null;
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
        Reset();

        _isExperimentWithBomb = isExperimentWithBomb;
        _didPhotonActuallyTakeLowerPath = _random.Next(2) == 0;

        AddPhotonSourceSymbol(new Point(INITIAL_PHOTON_X, INITIAL_PHOTON_Y));

        _initialPhoton = AddPhoton(new Point(INITIAL_PHOTON_X, INITIAL_PHOTON_Y), isInSuperposition: false);
        _beamSplitter = AddBeamSplitter(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y));

        if (_isExperimentWithBomb)
        {
            _bomb = AddBomb(isDud: _random.Next(2) == 0);
        }

        _upperPathMirror = AddMirror(new Point(TOP_MIRROR_X, TOP_MIRROR_Y));
        _lowerPathMirror = AddMirror(new Point(BOTTOM_MIRROR_X, BOTTOM_MIRROR_Y));
        _recombinator = AddBeamSplitter(new Point(RECOMBINATOR_X, RECOMBINATOR_Y));
        _detectorA = AddDetector(new Point(DETECTOR_A_X, DETECTOR_A_Y), Colors.Green);
        _detectorB = AddDetector(new Point(DETECTOR_B_X, DETECTOR_B_Y), Colors.Red);

        // TODO: Refactor to reduce nested callbacks using C# events for clarity.
        MoveInitialPhotonToBeamSplitter(() =>
        {
            Debug.WriteLine("Photon reached the beam splitter!");

            // Replace the photon with two photons in a state of quantum superposition
            _initialPhoton.Kill();
            _upperPhoton = AddPhoton(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y), isInSuperposition: true);
            _lowerPhoton = AddPhoton(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y), isInSuperposition: true);

            MoveUpperPhotonToMirror(() =>
            {
                Debug.WriteLine("Upper photon reached the mirror!");

                MoveUpperPhotonToRecombinator(() =>
                {
                    Debug.WriteLine("Upper photon reached the recombinator!");

                    PhotonArrivedAtRecombinator();
                });
            });

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
            Debug.WriteLine("The bomb exploded!");
            _bomb.Kill();
            _lowerPhoton!.Kill();
            _upperPhoton!.Kill();
            _upperPhoton.IsDead = true;

            _canvas.Children.Remove(_photonPaths.Where(p => p.PathName == "upper").Last().Polyline);
            var polyline = _photonPaths.Where(p => p.PathName == "lower").Last().Polyline;
            polyline.Stroke = Brushes.Magenta;
            polyline.StrokeThickness = PHOTON_ACTUAL_PATH_THICKNESS;
            polyline.StrokeDashArray = null;
        }
        else
        {
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

        if (_photonsArrivedCounter == 1) // First photon arrived
        {
            // NOTE: We set a 100 ms window to allow both photons to arrive at the recombinator beam splitter.
            // The program will crash if this condition isn't satisfied.
            _waitForOtherPhotonTimer = new DispatcherTimer();
            _waitForOtherPhotonTimer.Interval = TimeSpan.FromMilliseconds(100); // NOTE: We're using a 100 ms interval for the timer, which should be sufficient given a frame rate of 60 FPS. However, there's a potential for minor visual inconsistencies or "jankiness" due to this timing. Adjust as needed to improve smoothness if necessary.
            _waitForOtherPhotonTimer.Tick += (sender, args) => throw new Exception("Only one photon arrived within the time frame!"); 
            _waitForOtherPhotonTimer.Start();
        }
        else if (_photonsArrivedCounter == 2) // Second photon arrived
        {
            Debug.WriteLine("Both photons reached the recombinator!");

            if (_waitForOtherPhotonTimer != null)
            {
                _waitForOtherPhotonTimer.Stop();
                _waitForOtherPhotonTimer = null;
            }

            if (_isExperimentWithBomb)
            {
                _photonsArrivedCounter = 0;

                // NOTE: When using a bomb, the photons' paths are uncertain until we observe the outcome at the detectors.
                MoveLowerPhotonToDetectorA(() =>
                {
                    PhotonArrivedAtDetector();
                });

                MoveUpperPhotonToDetectorB(() =>
                {
                    PhotonArrivedAtDetector();
                });
            }
            else
            {
                // NOTE: In the absence of a bomb, the photons consistently exhibit destructive interference towards detector B and constructive interference towards detector A (which is in their original direction).
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

        if (_photonsArrivedCounter == 1) // First photon arrived
        {
            // TODO: Make this DRY. Consider abstracting out the timer setup into a separate method, e.g., `SetupPhotonArrivalTimer()`.
            _waitForOtherPhotonTimer = new DispatcherTimer();
            _waitForOtherPhotonTimer.Interval = TimeSpan.FromMilliseconds(100); 
            _waitForOtherPhotonTimer.Tick += (sender, args) => throw new Exception("Only one photon arrived within the time frame!");
            _waitForOtherPhotonTimer.Start();
        }
        else if (_photonsArrivedCounter == 2) // Second photon arrived
        {
            Debug.WriteLine("Both photons reached the recombinator!");

            if (_waitForOtherPhotonTimer != null)
            {
                _waitForOtherPhotonTimer.Stop();
                _waitForOtherPhotonTimer = null;
            }

            _upperPhoton!.Kill();
            _lowerPhoton!.Kill();

            if (_bomb!.IsBombLive == false)
            {
                // With a dud bomb, the photon behaves as if there's no bomb, consistently getting detected at Detector A due to interference.
                DetectedAtDetectorA();
            }
            else
            {
                // With a live bomb on the upper path, detection is 50-50 between Detectors A and B. Detection at A is inconclusive about the bomb's state, while detection at B confirms it's live.
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

        Canvas.SetZIndex(bomb, 1); // Set it to a higher value to bring it to the foreground

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

    private struct PhotonPath
    {
        public string PathName { get; set; }
        public Polyline Polyline { get; set; }
    }

    private List<PhotonPath> _photonPaths = new List<PhotonPath>();

    private void AnimatePhoton(Photon photon, Point targetPosition, double durationMilliseconds, string pathName, Action? onComplete = null)
    {
        // This timer will fire every frame
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0) // 60 FPS
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

        timer.Tick += (sender, args) =>
        {
            if (photon.IsDead)
            {
                ((DispatcherTimer)sender!).Stop();
                stopwatch.Stop();
                return;
            }

            // Determine how far along the duration we are (clamped to 1)
            double progress = Math.Min(1.0, stopwatch.ElapsedMilliseconds / durationMilliseconds);

            double newX = startPosition.X + (targetPosition.X - startPosition.X) * progress;
            double newY = startPosition.Y + (targetPosition.Y - startPosition.Y) * progress;

            // Move the photon to the current position
            photon.CentroidX = newX;
            photon.CentroidY = newY;

            // Add a new point to the Polyline at the photon's current position
            var newPoint = new Point(newX, newY);
            polyline.Points.Add(newPoint);

            // Stop the timer and the animation when we're done
            if (progress >= 1.0)
            {
                ((DispatcherTimer)sender!).Stop();
                stopwatch.Stop();

                onComplete?.Invoke(); // Notify that the animation is complete
            }
        };

        stopwatch.Start();
        timer.Start();
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

    // TODO: Refactor Detector to manage both lightened and original colors internally for clarity.
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
