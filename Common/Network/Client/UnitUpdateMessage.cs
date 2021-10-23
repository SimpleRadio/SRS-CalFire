using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ciribob.SRS.Common.Network.Models;

namespace Ciribob.SRS.Common.Network.Client
{
    public class UnitUpdateMessage
    {
        private  PlayerUnitStateBase _unitUpdate;

        public PlayerUnitStateBase UnitUpdate
        {
            get { return _unitUpdate; }
            set
            {
                if (value == null)
                {
                    _unitUpdate = null;
                }
                else
                {
                    var clone = value.DeepClone();
                    _unitUpdate = clone;
                }
            }
        }

        public bool FullUpdate { get; set; }
    }
}
