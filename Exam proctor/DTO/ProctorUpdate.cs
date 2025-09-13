using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exam_proctor.DTO
{
    public sealed class ProctorUpdate
    {

        public string Source { get; set; }          // "audio", "multiHuman", "facePose", "cheatingObjects", "keystroke", "paste"
        public string ModelOutput { get; set; }     // e.g., "true"/"false" or "2 humans" etc.
        public string Confidence { get; set; }      // string to keep formatting ("0.954123")
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Extra { get; set; }

    }
}
