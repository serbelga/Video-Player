using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace AplicacionGestures
{

    public sealed partial class MainPage : Page
    {
        MediaPlayer mediaPlayer;
        MediaTimelineController mediaTimelineController;
        CompositeTransform ct = new CompositeTransform();
        CompositeTransform ctv = new CompositeTransform();
        Rect sourceRect = new Rect(0, 0, 1, 1);
        Boolean controlsVisibility = false;
        public event WindowSizeChangedEventHandler SizeChanged;

        public MainPage()
        {
            this.InitializeComponent();
            this.speed.ManipulationMode = ManipulationModes.Rotate;
            this.volume.ManipulationMode = ManipulationModes.TranslateX;
            this.mediaPlayerElement.ManipulationMode = ManipulationModes.Scale | ManipulationModes.TranslateInertia | ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            mediaPlayer = new MediaPlayer();
            mediaTimelineController = new MediaTimelineController();
            mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/sample.mp4"));
            mediaPlayer = mediaPlayerElement.MediaPlayer;
            mediaPlayer.CommandManager.IsEnabled = false;
            mediaPlayer.TimelineController = mediaTimelineController;
            mediaTimelineController.IsLoopingEnabled = true;
            mediaPlayer.Volume = 0.5;
            InitializePropertiesAsync();
            OnResize();
            SizeChanged = new WindowSizeChangedEventHandler((o, e) => OnResize());
            Window.Current.SizeChanged += SizeChanged;
        }

        public async void InitializePropertiesAsync()
        {
            try
            {
                StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/sample.mp4"));
                if (storageFile != null)
                {
                    SetVideoTitle(storageFile);
                }
            }
            catch (FileNotFoundException)
            {
                title.Text = "Título";
            }

        }

        public async void SetVideoTitle(StorageFile storageFile)
        {
            if (storageFile != null)
            {
                VideoProperties videoProperties = await storageFile.Properties.GetVideoPropertiesAsync();
                if (videoProperties.Title == "")
                {
                    title.Text = storageFile.Name;
                } else
                {
                    title.Text = videoProperties.Title;
                }
            }
        }

        public void OnResize()
        {
            this.controlsPanel.Width = Window.Current.Bounds.Width * 0.9 - Window.Current.Bounds.Width * 0.1;
            GeneralTransform generalTransform = this.volume.TransformToVisual(Window.Current.Content);
            Point screenCoords = generalTransform.TransformPoint(new Point(0, 0));


            double low = Window.Current.Bounds.Width * 0.2;
            double high = Window.Current.Bounds.Width * 0.8;
            double mediatrix = (high + low) / 2;
            double length = high - low;
            double screenCoordsX = mediaPlayer.Volume * length + low;
            double posX = screenCoordsX - mediatrix;
            ctv.TranslateX = posX;
        }


        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.PlaybackSession.PlaybackRate = 1.0;
        }

        private void Image_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            FrameworkElement source = (FrameworkElement)e.OriginalSource;

            ct.CenterX = source.ActualWidth / 2.0;
            ct.CenterY = source.ActualHeight / 2.0;

            double rotation = Math.Max(-60, Math.Min(120, ct.Rotation + e.Delta.Rotation));
            double rotationAux = rotation + 60;
            double clockRate = rotationAux / 60;
            mediaTimelineController.ClockRate = clockRate;
            this.speedText.Text = clockRate.ToString("0.0") + "x";
            ct.Rotation = rotation;

            source.RenderTransform = ct;
        }

        /**
         * Action launched on Play Button
         */
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (mediaTimelineController.State != MediaTimelineControllerState.Running)
            {
                mediaTimelineController.Resume();
            }
        }

        /**
         * Called when Pointer is Pressed on Media Player Element
         */
        private void _mediaPlayerElement_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!controlsVisibility)
            {
                this.controlsPanel.Visibility = Visibility.Visible;
                this.commandBar.Visibility = Visibility.Visible;
                controlsVisibility = true;
            }
            else
            {
                this.controlsPanel.Visibility = Visibility.Collapsed;
                this.commandBar.Visibility = Visibility.Collapsed;
                controlsVisibility = false;
            }
        }

        /**
         * Escalado y movimiento del elemento de vídeo
         * Basado en el código disponible en los documentos de la librería del elemento MediaPlayer
         */
        private void _mediaPlayerElement_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            //Diferente de 1 -> Escalando
            if (e.Delta.Scale != 1)
            {
                double halfWidth = sourceRect.Width / 2.0;
                double halfHeight = sourceRect.Height / 2.0;

                double centerX = sourceRect.X + halfWidth;
                double centerY = sourceRect.Y + halfHeight;

                float scale = e.Delta.Scale;
                double newHalfWidth = (sourceRect.Width * scale) / 2.0;
                double newHalfHeight = (sourceRect.Height * scale) / 2.0;

                if (centerX - newHalfWidth > 0 && centerX + newHalfWidth <= 1.0 &&
                    centerY - newHalfHeight > 0 && centerY + newHalfHeight <= 1.0)
                {
                    sourceRect.X = centerX - newHalfWidth;
                    sourceRect.Y = centerY - newHalfHeight;
                    sourceRect.Width *= e.Delta.Scale;
                    sourceRect.Height *= e.Delta.Scale;
                }
            }
            //Igual a 1 -> Movimiento
            else
            {
                //Translation X y Y se divide entre el Tamaño actual del Rect para adaptar la translación al tamaño
                double translateX = -1 * e.Delta.Translation.X / mediaPlayerElement.ActualWidth;
                double translateY = -1 * e.Delta.Translation.Y / mediaPlayerElement.ActualHeight;
                //Comprobar límites de translación
                if (sourceRect.X + translateX >= 0 && sourceRect.X + sourceRect.Width + translateX <= 1.0 &&
                    sourceRect.Y + translateY >= 0 && sourceRect.Y + sourceRect.Height + translateY <= 1.0)
                {
                    sourceRect.X += translateX;
                    sourceRect.Y += translateY;
                }
            }
            //NormalizedSourceRect es el Rect normalizado en la fuente de vídeo. Se utiliza para el zoom y movimiento
            mediaPlayer.PlaybackSession.NormalizedSourceRect = sourceRect;
        }

        /**
         * 
         */
        private void MediaPlayerElement_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //Reset del tamaño del fotograma de vídeo
            sourceRect = new Rect(0, 0, 1, 1);
            mediaPlayer.PlaybackSession.NormalizedSourceRect = sourceRect;
        }

        private void MediaPlayerElement_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (mediaTimelineController.State == MediaTimelineControllerState.Running && e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                mediaTimelineController.Pause();
            }
            else if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                mediaTimelineController.Resume();
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaTimelineController.State == MediaTimelineControllerState.Running)
            {
                mediaTimelineController.Pause();
            }
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            mediaTimelineController.Start();
        }
        
        private void Volume_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            FrameworkElement source = (FrameworkElement) e.OriginalSource;
            GeneralTransform generalTransform = source.TransformToVisual(Window.Current.Content);
            Point screenCoords = generalTransform.TransformPoint(new Point(0, 0));
            double low = Window.Current.Content.RenderSize.Width * 0.2;
            double high = Window.Current.Content.RenderSize.Width * 0.8;
            double mediatrix = (high + low) / 2;
            double medium = mediatrix - low;
            if (screenCoords.X + e.Delta.Translation.X > low && screenCoords.X + source.ActualWidth + e.Delta.Translation.X < high)
            {
                ctv.TranslateX += e.Delta.Translation.X;
                source.RenderTransform = ctv;
                double current = ((screenCoords.X + source.ActualWidth / 2) - low) / (high - low);
                mediaPlayer.Volume = current;
                volumeText.Text = current.ToString("0.0");
            }
            ModifyVolumeIcon();
        }

        /**
         * Modifies Volume icon by the current Level
         */ 
        private void ModifyVolumeIcon()
        {
            if (mediaPlayer.Volume < 0.1)
            {
                volume.Text = "\xE992";
            }
            else if (mediaPlayer.Volume >= 0.1 && mediaPlayer.Volume < 0.3)
            {
                volume.Text = "\xE993";
            }
            else if (mediaPlayer.Volume >= 0.3 && mediaPlayer.Volume < 0.6)
            {
                volume.Text = "\xE994";
            }
            else
            {
                volume.Text = "\xE995";
            }
        }

        /**
         * Open video file
         */ 
        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".mkv");
            try
            {
                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    SetVideoTitle(file);
                    mediaPlayerElement.Source = MediaSource.CreateFromStorageFile(file);
                }
                else
                {
                    

                }
            } catch (IOException)
            {

            }
        }
    }
}




