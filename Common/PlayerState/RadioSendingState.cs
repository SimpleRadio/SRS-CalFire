using Ciribob.SRS.Common.Helpers;
using Newtonsoft.Json;

namespace Ciribob.SRS.Common.PlayerState
{
    public class RadioSendingState : PropertyChangedBase
    {
        [JsonIgnore] public long LastSentAt { get; set; }

        public bool IsSending { get; set; }

        public int SendingOn { get; set; }

        public int IsEncrypted { get; set; }
    }
}