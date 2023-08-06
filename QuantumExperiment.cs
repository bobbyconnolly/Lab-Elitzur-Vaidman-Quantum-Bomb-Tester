using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

public class QuantumExperiment
{
    private Canvas _canvas;

    private const double PHOTON_SIZE = 10;

    // All positions correspond to the center point of the object

    private const double INITIAL_PHOTON_X = 50;
    private const double INITIAL_PHOTON_Y = 200;

    private const double BEAM_SPLITTER_X = 200;
    private const double BEAM_SPLITTER_Y = 200;

    private const double BEAM_SPLITTER_WIDTH = 5; 
    private const double BEAM_SPLITTER_HEIGHT = 20;

    private const double BOMB_X = 350;
    private const double BOMB_Y = 200;
    private const double BOMB_SIZE = 20;

    private const double MIRROR1_X = 200;
    private const double MIRROR1_Y = 100;

    private const double MIRROR2_X = 600;
    private const double MIRROR2_Y = 200;

    private const double RECOMBINING_BEAM_SPLITTER_X = 600;
    private const double RECOMBINING_BEAM_SPLITTER_Y = 100;

    private const double MIRROR_WIDTH = 5;
    private const double MIRROR_HEIGHT = 20;

    private delegate void AnimationStep();

    private event AnimationStep SplitPhotonComplete;
    private event AnimationStep TopPhotonArrivesAtMirrorComplete;
    private event AnimationStep BottomPhotonArrivesAtMirrorComplete;

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
            double x = Canvas.GetLeft(_shape) + _shape.Width / 2.0;
            double y = Canvas.GetTop(_shape) + _shape.Height / 2.0;
            return new Point(x, y);
        }

        private void UpdatePosition()
        {
            Canvas.SetLeft(_shape, _centroidX - _shape.Width / 2.0);
            Canvas.SetTop(_shape, _centroidY - _shape.Height / 2.0);
        }
    }

    public class Photon : RenderableEntity
    {
        public Photon(Shape shape, Canvas canvas, double x, double y) : base(shape, canvas, x, y)
        {
            // Additional Photon-specific initialization can go here, if needed.
        }
    }

    public QuantumExperiment(Canvas canvas)
    {
        _canvas = canvas;
    }

    public void Run()
    {
        var photon = AddPhoton();

        AddBomb();

        var beamSplitter = AddBeamSplitter(new Point(BEAM_SPLITTER_X, BEAM_SPLITTER_Y));
        AddBeamSplitter(new Point(RECOMBINING_BEAM_SPLITTER_X, RECOMBINING_BEAM_SPLITTER_Y));

        AddMirror(new Point(MIRROR1_X, MIRROR1_Y));
        AddMirror(new Point(MIRROR2_X, MIRROR2_Y));

        MovePhotonToBeamSplitter(photon, beamSplitter, () =>
        {
            Debug.WriteLine("Photon reached the beam splitter!");
            // Place any additional logic you want to execute here.
        });
    }

    public void MovePhotonToBeamSplitter(Photon photon, RenderableEntity beamSplitter, Action callback)
    {
        AnimatePhoton(photon, beamSplitter.GetCentroid(), 1000, callback);
    }

    private Photon AddPhoton()
    {
        Ellipse photon = new Ellipse
        {
            Width = PHOTON_SIZE,
            Height = PHOTON_SIZE,
            Fill = Brushes.Magenta
        };

        return new Photon(photon, _canvas, INITIAL_PHOTON_X, INITIAL_PHOTON_Y);
    }

    private RenderableEntity AddBomb()
    {
        Ellipse bomb = new Ellipse
        {
            Width = BOMB_SIZE,
            Height = BOMB_SIZE,
            Fill = Brushes.Black
        };

        return new RenderableEntity(bomb, _canvas, BOMB_X, BOMB_Y);
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
        RotateTransform rotateTransform = new RotateTransform(45.0, BEAM_SPLITTER_WIDTH / 2.0, BEAM_SPLITTER_HEIGHT / 2.0);
        beamSplitter.RenderTransform = rotateTransform;

        return new RenderableEntity(beamSplitter, _canvas, centroid.X, centroid.Y);
    }

    private RenderableEntity AddMirror(Point centroid)
    {
        Rectangle mirror = new Rectangle
        {
            Width = MIRROR_WIDTH,
            Height = MIRROR_HEIGHT,
            Fill = Brushes.MediumPurple
        };

        // Rotate the mirror
        RotateTransform rotateTransform = new RotateTransform(45.0, MIRROR_WIDTH / 2.0, MIRROR_HEIGHT / 2.0);
        mirror.RenderTransform = rotateTransform;

        return new RenderableEntity(mirror, _canvas, centroid.X, centroid.Y);
    }

    public void AnimatePhoton(Photon photon, Point targetPosition, double durationMilliseconds, Action onComplete = null)
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
            Stroke = Brushes.Magenta,
            StrokeThickness = 2.0,
            StrokeDashArray = new DoubleCollection { 1.5, 2.0 }
        };
        _canvas.Children.Add(pathDashed);

        timer.Tick += (sender, args) =>
        {
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
                ((DispatcherTimer)sender).Stop();
                stopwatch.Stop();

                onComplete?.Invoke(); // Notify that the animation is complete
            }
        };

        stopwatch.Start();
        timer.Start();
    }
}
