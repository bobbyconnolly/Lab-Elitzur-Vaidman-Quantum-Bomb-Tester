using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

public class QuantumExperiment
{
    // All positions correspond to the entity's centroid
    private const double PHOTON_SIZE = 10;
    private const double PHOTON_VELOCITY = 0.3;

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

    public class RenderableEntity
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

    public class Photon : RenderableEntity
    {
        bool _isFaded;
        bool _isDead;

        public Photon(Shape shape, Canvas canvas, double x, double y, bool isFaded) : base(shape, canvas, x, y)
        {
            _isFaded = isFaded;
        }

        public bool IsFaded
        {
            get { return _isFaded; }
        }

        public bool IsDead
        {
            get { return _isDead; }
            set { _isDead = value; }
        }
    }

    public class Bomb : RenderableEntity
    {
        bool _isBombLive;

        public Bomb(Shape shape, Canvas canvas, double x, double y, bool isBombLive) : base(shape, canvas, x, y)
        {
            _isBombLive = isBombLive;
        }

        public bool IsBombLive
        {
            get { return _isBombLive; }
        }
    }

    public class Detector : RenderableEntity
    {
        Color _color;
        bool _hasDetectedPhoton;

        public Detector(Shape shape, Canvas canvas, double x, double y, Color color) : base(shape, canvas, x, y)
        {
            _color = color;
            _hasDetectedPhoton = false;
        }

        public bool HasDetectedPhoton
        {
            get { return _hasDetectedPhoton; }
            set { 
                _hasDetectedPhoton = value;
                UpdateColor();
            }
        }

        private void UpdateColor()
        {
            if (_hasDetectedPhoton)
            {
                base.Shape.Fill = new SolidColorBrush(_color); // Change to dark color
            }
        }
    }

    public QuantumExperiment(Canvas canvas)
    {
        _canvas = canvas;
        _recombinator = null;
    }

    public void Reset()
    {
        // Clear the canvas
        _canvas.Children.Clear();

        // Clear the active photons list
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

        _initialPhoton = AddPhoton(new Point(INITIAL_PHOTON_X, INITIAL_PHOTON_Y), isFaded: false);
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

        // TODO: Fix "pyramid of doom" callback hell by using events/delegates instead
        MoveInitialPhotonToBeamSplitter(() =>
        {
            Debug.WriteLine("Photon reached the beam splitter!");

            // Replace the photon with two photons in a state of quantum superposition
            _initialPhoton.Kill();
            _upperPhoton = AddPhoton(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y), isFaded: true);
            _lowerPhoton = AddPhoton(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y), isFaded: true);

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

    public void MoveInitialPhotonToBeamSplitter(Action callback)
    {
        AnimatePhoton(_initialPhoton!, _beamSplitter!.GetCentroid(), (_beamSplitter.CentroidX - _initialPhoton!.CentroidX) / PHOTON_VELOCITY, callback);
    }

    public void MoveUpperPhotonToMirror(Action callback)
    {
        AnimatePhoton(_upperPhoton!, _upperPathMirror!.GetCentroid(), (-1) * (_upperPathMirror.CentroidY - _beamSplitter!.CentroidY) / PHOTON_VELOCITY, callback);
    }

    public void MoveLowerPhotonToBomb(Action callback)
    {
        AnimatePhoton(_lowerPhoton!, _bomb!.GetCentroid(), (_bomb.CentroidX - _beamSplitter!.CentroidX) / PHOTON_VELOCITY, callback);
    }

    public void MoveUpperPhotonToRecombinator(Action callback)
    {
        AnimatePhoton(_upperPhoton!, _recombinator!.GetCentroid(), (_recombinator.CentroidX - _upperPathMirror!.CentroidX) / PHOTON_VELOCITY, callback);
    }

    public void MoveLowerPhotonFromBeamSplitterToMirror(Action callback)
    {
        AnimatePhoton(_lowerPhoton!, _lowerPathMirror!.GetCentroid(), (_lowerPathMirror!.CentroidX - _beamSplitter!.CentroidX) / PHOTON_VELOCITY, callback);
    }

    public void MoveLowerPhotonFromBombToMirror(Action callback)
    {
        if (_didPhotonActuallyTakeLowerPath && _bomb!.IsBombLive)
        {
            Debug.WriteLine("The bomb exploded!");
            _bomb.Kill();
            _lowerPhoton!.Kill();
            _upperPhoton!.Kill();
            _upperPhoton.IsDead = true;
        }
        else
        {
            AnimatePhoton(_lowerPhoton!, _lowerPathMirror!.GetCentroid(), (_lowerPathMirror!.CentroidX - _bomb!.CentroidX) / PHOTON_VELOCITY, callback);
        }
    }

    public void MoveLowerPhotonToRecombinator(Action callback)
    {
        AnimatePhoton(_lowerPhoton!, _recombinator!.GetCentroid(), (-1) * (_recombinator!.CentroidY - _lowerPathMirror!.CentroidY) / PHOTON_VELOCITY, callback);
    }

    private void PhotonArrivedAtRecombinator()
    {
        _photonsArrivedCounter++;

        if (_photonsArrivedCounter == 1) // First photon arrived
        {
            _waitForOtherPhotonTimer = new DispatcherTimer();
            _waitForOtherPhotonTimer.Interval = TimeSpan.FromMilliseconds(100); // 100 ms, adjust as needed
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

                // NOTE: Visually, we arbitrarily choose which photon (upper/lower) goes to detector A or B. In reality, these are representations of superposed photons and their paths are indeterminate until observed.
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
                _upperPhoton!.Kill();
                _lowerPhoton!.Kill();

                _recombinedPhoton = AddPhoton(_recombinator!.GetCentroid(), isFaded: false);

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
        AnimatePhoton(_upperPhoton!, _detectorB!.GetCentroid(), (-1) * (_detectorB!.CentroidY - _recombinator!.CentroidY) / PHOTON_VELOCITY, callback);
    }

    public void MoveLowerPhotonToDetectorA(Action callback)
    {
        AnimatePhoton(_lowerPhoton!, _detectorA!.GetCentroid(), (_detectorA!.CentroidX - _recombinator!.CentroidX) / PHOTON_VELOCITY, callback);
    }

    public void MoveRecombinedPhotonToDetectorA(Action callback)
    {
        AnimatePhoton(_recombinedPhoton!, _detectorA!.GetCentroid(), (_detectorA!.CentroidX - _recombinator!.CentroidX) / PHOTON_VELOCITY, callback);
    }

    private void PhotonArrivedAtDetector()
    {
        _photonsArrivedCounter++;

        if (_photonsArrivedCounter == 1) // First photon arrived
        {
            _waitForOtherPhotonTimer = new DispatcherTimer();
            _waitForOtherPhotonTimer.Interval = TimeSpan.FromMilliseconds(100); // 100 ms, adjust as needed
            _waitForOtherPhotonTimer.Tick += (sender, args) => throw new Exception("Only one photon arrived within the time frame!");
            _waitForOtherPhotonTimer.Start();
        }
        else if (_photonsArrivedCounter == 2) // Second photon arrived
        {
            if (_waitForOtherPhotonTimer != null)
            {
                _waitForOtherPhotonTimer.Stop();
                _waitForOtherPhotonTimer = null;
            }

            _upperPhoton!.Kill();
            _lowerPhoton!.Kill();

            if (_bomb!.IsBombLive == false)
            {
                Debug.WriteLine("Detected at A, you don't know if the bomb is live or a dud");
                _detectorA!.HasDetectedPhoton = true;
                return;
            }
            else
            {
                if (_random.Next(2) == 0)
                {
                    Debug.WriteLine("Detected at A, you don't know if the bomb is live or a dud");
                    _detectorA!.HasDetectedPhoton = true;
                } 
                else
                {
                    Debug.WriteLine("Detected at B, bomb is live AND didn't explode (photon took upper path)");
                    _detectorB!.HasDetectedPhoton = true;
                }
            }

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

    private Photon AddPhoton(Point centroid, bool isFaded)
    {
        Ellipse photon = new Ellipse
        {
            Width = PHOTON_SIZE,
            Height = PHOTON_SIZE,
            Fill = isFaded ? new SolidColorBrush(LightenColor(Brushes.Magenta.Color, 0.6)) : Brushes.Magenta
        };

        return new Photon(photon, _canvas, centroid.X, centroid.Y, isFaded);
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

    private void AnimatePhoton(Photon photon, Point targetPosition, double durationMilliseconds, Action? onComplete = null)
    {
        // This timer will fire every frame
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0) // 60 FPS
        };

        Point startPosition = photon.GetCentroid();

        var stopwatch = new Stopwatch();

        // Create a new Polyline for the photon's path (dashed line)
        var pathDashed = new Polyline
        {
            Stroke = photon.IsFaded ? new SolidColorBrush(LightenColor(Brushes.Magenta.Color, 0.6)) : Brushes.Magenta,
            StrokeThickness = photon.IsFaded ? 2.0 : 3.0,
            StrokeDashArray = new DoubleCollection { 1.5, 2.0 }
        };
        _canvas.Children.Add(pathDashed);

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
            pathDashed.Points.Add(newPoint);

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
}
