using Ciribob.SRS.Mobile.Client;

namespace Mobile
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private SRSAudioManager _srsAudioManager;

        public async Task<PermissionStatus> CheckAndRequestMicrophonePermission()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.Microphone>())
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<Permissions.Microphone>();

            return status;
        }

        public MainPage()
        {
            InitializeComponent();

            Task<PermissionStatus> status = CheckAndRequestMicrophonePermission();
        }

        void OnStopClicked(System.Object sender, System.EventArgs e)
        {
            _srsAudioManager?.StopEncoding();
        }

        void OnStartClicked(System.Object sender, System.EventArgs e)
        {
            _srsAudioManager?.StopEncoding();

            _srsAudioManager = new SRSAudioManager();
            _srsAudioManager.StartPreview();
        }

    }
}