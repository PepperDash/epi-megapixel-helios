using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Communication monitor for a device reached over HTTP: each successful response reports the device OK; once
    /// responses stop, it reports a warning after the warning time and an error after the error time. A device that
    /// has never answered stays Unknown until then.
    /// </summary>
    public class HeliosResponseMonitor : StatusMonitorBase
    {
        public HeliosResponseMonitor(IKeyed parent, long warningTimeMs, long errorTimeMs)
            : base(parent, warningTimeMs, errorTimeMs)
        {
        }

        public override void Start()
        {
            // Nothing heard yet: start counting towards warning and error.
            StartErrorTimers();
        }

        public override void Stop()
        {
            StopErrorTimers();
        }

        /// <summary>The device answered.</summary>
        public void ReportResponse()
        {
            StopErrorTimers();
            Status = MonitorStatus.IsOk;
        }

        /// <summary>A request failed or was refused: count from now unless already counting.</summary>
        public void ReportNoResponse()
        {
            StartErrorTimers();
        }
    }
}
