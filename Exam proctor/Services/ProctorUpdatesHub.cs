using Exam_proctor.DTO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exam_proctor.Services
{
    public sealed class ProctorUpdatesHub
    {
        public static ProctorUpdatesHub Instance { get; } = new ProctorUpdatesHub();
        private ProctorUpdatesHub() { }

        private SynchronizationContext _uiCtx;

        // Call this once on the UI thread (e.g., in Form1/Program.cs)
        public void InitializeOnUIThread()
        {
            _uiCtx = SynchronizationContext.Current
                     ?? throw new InvalidOperationException("Call InitializeOnUIThread() from UI thread after Application.Run starts.");
        }




        public event EventHandler<ProctorUpdate> UpdateReceived;

        // Keep "latest" snapshot per source for quick badges
        private readonly ConcurrentDictionary<string, ProctorUpdate> _latest = new ConcurrentDictionary<string, ProctorUpdate>();

        public ProctorUpdate GetLatest(string source)
            => _latest.TryGetValue(source, out var u) ? u : null;

        public void Publish(ProctorUpdate update)
        {
            _latest[update.Source] = update;

            var handler = UpdateReceived;
            if (handler == null) return;

            if (_uiCtx != null)
            {
                // Marshal to UI thread so subscribers can touch controls safely
                _uiCtx.Post(_ => handler(this, update), null);
            }
            else
            {
                // Fallback (shouldn't happen if initialized)
                handler(this, update);
            }
        }
    }
}
