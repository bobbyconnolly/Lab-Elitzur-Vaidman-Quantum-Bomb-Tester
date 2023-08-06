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

    private const double INITIAL_PHOTON_X = 50;
    private const double INITIAL_PHOTON_Y = 200;

    private const double BEAM_SPLITTER_X = 200;
    private const double BEAM_SPLITTER_Y = 200;

    private const double BEAM_SPLITTER_WIDTH = 5; 
    private const double BEAM_SPLITTER_HEIGHT = 20;

    private const double BOMB_X = 300;
    private const double BOMB_Y = 200;
    private const double BOMB_SIZE = 20;

    private const double MIRROR1_X = 200;
    private const double MIRROR1_Y = 100;

    private const double MIRROR2_X = 400;
    private const double MIRROR2_Y = 200;

    private const double RECOMBINING_BEAM_SPLITTER_X = 400;
    private const double RECOMBINING_BEAM_SPLITTER_Y = 100;

    private const double MIRROR_WIDTH = 5;
    private const double MIRROR_HEIGHT = 20;

    private delegate void AnimationStep();

    private event AnimationStep SplitPhotonComplete;
    private event AnimationStep TopPhotonArrivesAtMirrorComplete;
    private event AnimationStep BottomPhotonArrivesAtMirrorComplete;

    public QuantumExperiment(Canvas canvas)
    {
        _canvas = canvas;
    }

    public void Run()
    {
        AddPhoton();

        AddBomb();

        AddBeamSplitter();

        AddRecombiningBeamSplitter();

        AddMirror(MIRROR1_X, MIRROR1_Y);
        AddMirror(MIRROR2_X, MIRROR2_Y);

        //AnimatePhoton(photon, 400, 1000);
    }

    private void AddPhoton()
    {
        Ellipse photon = new Ellipse
        {
            Width = PHOTON_SIZE,
            Height = PHOTON_SIZE,
            Fill = Brushes.Magenta
        };

        _canvas.Children.Add(photon);

        Canvas.SetLeft(photon, INITIAL_PHOTON_X - PHOTON_SIZE / 2.0);
        Canvas.SetTop(photon, INITIAL_PHOTON_Y - PHOTON_SIZE / 2.0);
    }

    private void AddBomb()
    {
        Ellipse bomb = new Ellipse
        {
            Width = BOMB_SIZE,
            Height = BOMB_SIZE,
            Fill = Brushes.Black
        };

        _canvas.Children.Add(bomb);

        Canvas.SetLeft(bomb, BOMB_X - BOMB_SIZE / 2.0);
        Canvas.SetTop(bomb, BOMB_Y - BOMB_SIZE / 2.0);
    }

    private void AddBeamSplitter()
    {
        Rectangle beamSplitter = new Rectangle
        {
            Width = BEAM_SPLITTER_WIDTH,
            Height = BEAM_SPLITTER_HEIGHT,
            Fill = Brushes.LightGreen
        };

        // Set the beam splitter's position
        Canvas.SetLeft(beamSplitter, BEAM_SPLITTER_X - BEAM_SPLITTER_WIDTH / 2); 
        Canvas.SetTop(beamSplitter, BEAM_SPLITTER_Y - BEAM_SPLITTER_HEIGHT / 2);

        // Rotate the beam splitter
        RotateTransform rotateTransform = new RotateTransform(45.0, BEAM_SPLITTER_WIDTH / 2, BEAM_SPLITTER_HEIGHT / 2);
        beamSplitter.RenderTransform = rotateTransform;

        _canvas.Children.Add(beamSplitter);
    }

    private void AddRecombiningBeamSplitter()
    {
        Rectangle recombiningBeamSplitter = new Rectangle
        {
            Width = BEAM_SPLITTER_WIDTH,
            Height = BEAM_SPLITTER_HEIGHT,
            Fill = Brushes.LightGreen
        };

        // Set the beam splitter's position
        Canvas.SetLeft(recombiningBeamSplitter, RECOMBINING_BEAM_SPLITTER_X - BEAM_SPLITTER_WIDTH / 2);
        Canvas.SetTop(recombiningBeamSplitter, RECOMBINING_BEAM_SPLITTER_Y - BEAM_SPLITTER_HEIGHT / 2);

        // Rotate the beam splitter
        RotateTransform rotateTransform = new RotateTransform(45.0, BEAM_SPLITTER_WIDTH / 2, BEAM_SPLITTER_HEIGHT / 2);
        recombiningBeamSplitter.RenderTransform = rotateTransform;

        _canvas.Children.Add(recombiningBeamSplitter);
    }

    private void AddMirror(double mirrorX, double mirrorY)
    {
        Rectangle mirror = new Rectangle
        {
            Width = MIRROR_WIDTH,
            Height = MIRROR_HEIGHT,
            Fill = Brushes.MediumPurple
        };

        // Set the beam splitter's position
        Canvas.SetLeft(mirror, mirrorX - MIRROR_WIDTH / 2);
        Canvas.SetTop(mirror, mirrorY - MIRROR_HEIGHT / 2);

        // Rotate the beam splitter
        RotateTransform rotateTransform = new RotateTransform(45.0, MIRROR_WIDTH / 2, MIRROR_HEIGHT / 2);
        mirror.RenderTransform = rotateTransform;

        _canvas.Children.Add(mirror);
    }

    public void AnimatePhoton(Ellipse photon, double targetX, double durationMilliseconds)
    {
        // This timer will fire every frame
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0) // 60 FPS
        };

        double startX = Canvas.GetLeft(photon);
        double distance = targetX - startX;
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
            double newX = startX + distance * progress;

            // Move the photon to the current position
            Canvas.SetLeft(photon, newX);

            // Add a new point to the Polyline at the photon's current position
            var newPoint = new Point(newX, Canvas.GetTop(photon) + photon.Height / 2);
            pathDashed.Points.Add(newPoint);

            // Stop the timer and the animation when we're done
            if (progress >= 1.0)
            {
                ((DispatcherTimer)sender).Stop();
                stopwatch.Stop();
            }
        };

        stopwatch.Start();
        timer.Start();
    }
}
