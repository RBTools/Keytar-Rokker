using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Midi;

namespace KeytarRokker
{
    class MIDIStuff
    {
        private int TicksPerQuarterNote;
        private List<TempoEvent> TempoEvents;
        private MidiFile MIDIFile;
        public MIDIChart MIDI_Chart;
        private long LengthLong;
        public List<PracticeSection> PracticeSessions;
        private const int KEYS_OD = 116;
        private const int PRO_KEYS_SOLO = 115;

        public void Initialize()
        {
            MIDI_Chart = new MIDIChart();
            MIDI_Chart.Initialize();
            PracticeSessions = new List<PracticeSection>();
        }

        public bool ReadMIDIFile(string midi)
        {
            if (!File.Exists(midi)) return false;
            var Tools = new NemoTools();
            LengthLong = 0;
            MIDIFile = null;
            MIDIFile = Tools.NemoLoadMIDI(midi);
            if (MIDIFile == null) return false;
            try
            {
                TicksPerQuarterNote = MIDIFile.DeltaTicksPerQuarterNote;
                BuildTempoList();
                for (var i = 0; i < MIDIFile.Events.Tracks; i++)
                {
                    var trackname = MIDIFile.Events[i][0].ToString();
                    if (trackname.Contains("REAL_KEYS_X"))
                    {
                        MIDI_Chart.ProKeysX.Overdrive = GetSpecialMarker(MIDIFile.Events[i], KEYS_OD);
                        List<MIDINote> toadd;
                        CheckMIDITrack(MIDIFile.Events[i], MIDI_Chart.ProKeysX.ValidNotes, out toadd);
                        MIDI_Chart.ProKeysX.ChartedNotes.AddRange(toadd);
                        MIDI_Chart.ProKeysX.Solos = GetSpecialMarker(MIDIFile.Events[i], PRO_KEYS_SOLO);
                        GetRangeShifts(MIDIFile.Events[i]);
                    }
                    else if (trackname.Contains("REAL_KEYS_H"))
                    {
                        MIDI_Chart.ProKeysH.Overdrive = MIDI_Chart.ProKeysX.Overdrive;
                        List<MIDINote> toadd;
                        CheckMIDITrack(MIDIFile.Events[i], MIDI_Chart.ProKeysH.ValidNotes, out toadd);
                        MIDI_Chart.ProKeysH.ChartedNotes.AddRange(toadd);
                        MIDI_Chart.ProKeysH.Solos = MIDI_Chart.ProKeysX.Solos;
                    }
                    else if (trackname.Contains("REAL_KEYS_M"))
                    {
                        MIDI_Chart.ProKeysM.Overdrive = MIDI_Chart.ProKeysX.Overdrive;
                        List<MIDINote> toadd;
                        CheckMIDITrack(MIDIFile.Events[i], MIDI_Chart.ProKeysM.ValidNotes, out toadd);
                        MIDI_Chart.ProKeysM.ChartedNotes.AddRange(toadd);
                        MIDI_Chart.ProKeysM.Solos = MIDI_Chart.ProKeysX.Solos;
                    }
                    else if (trackname.Contains("REAL_KEYS_E"))
                    {
                        MIDI_Chart.ProKeysE.Overdrive = MIDI_Chart.ProKeysX.Overdrive;
                        List<MIDINote> toadd;
                        CheckMIDITrack(MIDIFile.Events[i], MIDI_Chart.ProKeysE.ValidNotes, out toadd);
                        MIDI_Chart.ProKeysE.ChartedNotes.AddRange(toadd);
                        MIDI_Chart.ProKeysE.Solos = MIDI_Chart.ProKeysX.Solos;
                    }
                    else if (trackname.Contains("ANIM") && trackname.Contains("RH"))
                    {
                        MIDI_Chart.ProKeysRH.Overdrive = MIDI_Chart.ProKeysX.Overdrive;
                        List<MIDINote> toadd;
                        CheckMIDITrack(MIDIFile.Events[i], MIDI_Chart.ProKeysRH.ValidNotes, out toadd);
                        MIDI_Chart.ProKeysRH.ChartedNotes.AddRange(toadd);
                        MIDI_Chart.ProKeysRH.Solos = MIDI_Chart.ProKeysX.Solos;
                    }
                    else if (trackname.Contains("ANIM") && trackname.Contains("LH"))
                    {
                        MIDI_Chart.ProKeysLH.Overdrive = MIDI_Chart.ProKeysX.Overdrive;
                        List<MIDINote> toadd;
                        CheckMIDITrack(MIDIFile.Events[i], MIDI_Chart.ProKeysLH.ValidNotes, out toadd);
                        MIDI_Chart.ProKeysLH.ChartedNotes.AddRange(toadd);
                        MIDI_Chart.ProKeysLH.Solos = MIDI_Chart.ProKeysX.Solos;
                    }
                    else if (trackname.Contains("EVENTS"))
                    {
                        foreach (var note in MIDIFile.Events[i])
                        {
                            switch (note.CommandCode)
                            {
                                case MidiCommandCode.MetaEvent:
                                    var section_event = (MetaEvent)note;
                                    if (section_event.MetaEventType != MetaEventType.Lyric &&
                                        section_event.MetaEventType != MetaEventType.TextEvent)
                                    {
                                        continue;
                                    }
                                    if (section_event.ToString().Contains("[section "))
                                    {
                                        var index = section_event.ToString().IndexOf("[", StringComparison.Ordinal);
                                        var new_section = section_event.ToString().Substring(index, section_event.ToString().Length - index);
                                        new_section = new_section.Replace("section ", "prc_");
                                        new_section = new_section.Replace("guitar", "gtr");
                                        new_section = new_section.Replace("practice_outro", "outro");
                                        new_section = new_section.Replace("big_rock_ending", "bre");
                                        new_section = new_section.Replace(" ", "_").Replace("-", "").Replace("!", "").Replace("?", "");
                                        GetPracticeSession(new_section, section_event.AbsoluteTime);
                                    }
                                    else if (section_event.ToString().Contains("[prc_"))
                                    {
                                        GetPracticeSession(section_event.ToString(), section_event.AbsoluteTime);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            MIDI_Chart.AverageBPM = AverageBPM();
            PracticeSessions.Sort((a, b) => a.SectionStart.CompareTo(b.SectionStart));
            MIDI_Chart.ProKeysX.Sort();
            MIDI_Chart.ProKeysH.Sort();
            MIDI_Chart.ProKeysM.Sort();
            MIDI_Chart.ProKeysE.Sort();
            MIDI_Chart.ProKeysRH.Sort();
            MIDI_Chart.ProKeysLH.Sort();
            MIDI_Chart.RangeShifts.Sort((a, b) => a.ShiftBegin.CompareTo(b.ShiftBegin));
            return true;
        }

        private List<SpecialMarker> GetSpecialMarker(IEnumerable<MidiEvent> track, int marker_note)
        {
            return (from notes in track where notes.CommandCode == MidiCommandCode.NoteOn select (NoteOnEvent)notes into note where note.Velocity > 0 && note.NoteNumber == marker_note let time = GetRealtime(note.AbsoluteTime) let end = GetRealtime(note.AbsoluteTime + note.NoteLength) select new SpecialMarker { MarkerBegin = time, MarkerEnd = end }).ToList();
        }

        private void GetRangeShifts(IList<MidiEvent> track)
        {
            var validRanges = new List<int> {0, 2, 4, 5, 7, 9};
            for (var z = 0; z < track.Count(); z++)
            {
                try
                {
                    var notes = track[z];
                    if (notes.AbsoluteTime > LengthLong)
                    {
                        LengthLong = notes.AbsoluteTime;
                    }
                    if (notes.CommandCode != MidiCommandCode.NoteOn) continue;
                    var note = (NoteOnEvent) notes;
                    if (note.Velocity <= 0 || !validRanges.Contains(note.NoteNumber)) continue;
                    var time = GetRealtime(note.AbsoluteTime);
                    var shift = new RangeShift {ShiftBegin = time, ShiftNote = note.NoteNumber};
                    MIDI_Chart.RangeShifts.Add(shift);
                }
                catch (Exception)
                {}
            }
        }

        private void GetPracticeSession(string session, long start_time)
        {
            var index = session.IndexOf("[", StringComparison.Ordinal);
            session = session.Substring(index, session.Length - index).Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Trim();
            if (File.Exists(Application.StartupPath + "\\bin\\sections"))
            {
                var sr = new StreamReader(Application.StartupPath + "\\bin\\sections");
                while (sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();
                    line = line.Replace("(", "").Replace(")", "");
                    var i = line.IndexOf("\"", StringComparison.Ordinal);
                    var prc = line.Substring(0, i).Trim();
                    if (prc != session) continue;
                    session = line.Substring(i, line.Length - i).Replace("\"", "").Trim();
                    session = session.Replace("Gtr", "Guitar");
                    var myTI = new CultureInfo("en-US", false).TextInfo;
                    session = myTI.ToTitleCase(session);
                    break;
                }
                sr.Dispose();
            }
            var practice = new PracticeSection
            {
                SectionStart = GetRealtime(start_time),
                SectionName = "[" + session.Replace("prc", "").Replace("_", " ").Trim() + "]"
            };
            PracticeSessions.Add(practice);
        }
        
        private void CheckMIDITrack(IList<MidiEvent> track, ICollection<int> valid_notes, out List<MIDINote> output)
        {
            output = new List<MIDINote>();
            for (var z = 0; z < track.Count(); z++)
            {
                try
                {
                    var notes = track[z];
                    if (notes.AbsoluteTime > LengthLong)
                    {
                        LengthLong = notes.AbsoluteTime;
                    }
                    if (notes.CommandCode != MidiCommandCode.NoteOn) continue;
                    var note = (NoteOnEvent)notes;
                    if (note.Velocity <= 0) continue;
                    if (!valid_notes.Contains(note.NoteNumber)) continue;
                    var time = GetRealtime(note.AbsoluteTime);
                    var length = GetRealtime(note.NoteLength);
                    var end = Math.Round(time + length, 5);
                    var hasOD = MIDI_Chart.ProKeysX.Overdrive.Where(OD => OD.MarkerBegin <= time).Any(OD => OD.MarkerEnd >= end);
                    var n = new MIDINote
                    {
                        NoteStart = time,
                        NoteLength = note.NoteLength,
                        NoteEnd = end,
                        NoteNumber = note.NoteNumber,
                        NoteName = note.NoteName,
                        hasOD = hasOD,
                        TicksPerQuarterNote =  TicksPerQuarterNote
                    };
                    output.Add(n);
                }
                catch (Exception)
                { }
            }
        }

        private double GetRealtime(long absdelta)
        {
            //code by raynebc
            var BPM = 120.0;   //As per the MIDI specification, until a tempo change is reached, 120BPM is assumed
            var reldelta = absdelta;   //The number of delta ticks between the delta time being converted and the tempo change immediately at or before it
            var time = 0.0;   //The real time position of the tempo change immediately at or before the delta time being converted
            foreach (var tempo in TempoEvents.Where(tempo => tempo.AbsoluteTime <= absdelta))
            {
                BPM = tempo.BPM;
                time = tempo.RealTime;
                reldelta = absdelta - tempo.AbsoluteTime;
            }
            time += (double)reldelta / TicksPerQuarterNote * (60000.0 / BPM);
            return Math.Round(time / 1000, 5);
        }
        
        private void BuildTempoList()
        {
            //code provided by raynebc
            //Build tempo list
            var currentbpm = 120.00;
            var realtime = 0.0;
            var reldelta = 0;   //The number of delta ticks since the last tempo change
            TempoEvents = new List<TempoEvent>();
            foreach (var ev in MIDIFile.Events[0])
            {
                reldelta += ev.DeltaTime;
                if (ev.CommandCode != MidiCommandCode.MetaEvent) continue;
                var tempo = (MetaEvent)ev;
                if (tempo.MetaEventType != MetaEventType.SetTempo) continue;
                var relativetime = (double)reldelta / TicksPerQuarterNote * (60000.0 / currentbpm);
                var index1 = tempo.ToString().IndexOf("SetTempo", StringComparison.Ordinal) + 9;
                var index2 = tempo.ToString().IndexOf("bpm", StringComparison.Ordinal);
                var bpm = tempo.ToString().Substring(index1, index2 - index1);
                currentbpm = Convert.ToDouble(bpm);   //As per the MIDI specification, until a tempo change is reached, 120BPM is assumed
                realtime += relativetime;   //Add that to the ongoing current real time of the MIDI
                reldelta = 0;
                var tempo_event = new TempoEvent
                {
                    AbsoluteTime = tempo.AbsoluteTime,
                    RealTime = realtime,
                    BPM = currentbpm
                };
                TempoEvents.Add(tempo_event);
            }
        }

        private double AverageBPM()
        {
            var total_bpm = 0.0;
            var last = 0.0;
            var bpm = 120.0;
            double difference;
            var LengthSeconds = GetRealtime(LengthLong);
            if (LengthSeconds <= 0.0)
            {
                var count = TempoEvents.Sum(tempo => tempo.BPM);
                return Math.Round(count / TempoEvents.Count, 2);
            }
            foreach (var tempo in TempoEvents)
            {
                var current = GetRealtime(tempo.AbsoluteTime);
                difference = current - last;
                last = GetRealtime(tempo.AbsoluteTime);
                if (difference <= 0.0)
                {
                    bpm = tempo.BPM;
                    continue;
                }
                total_bpm += bpm * (difference / LengthSeconds);
                bpm = tempo.BPM;
            }
            difference = LengthSeconds - last;
            total_bpm += bpm * (difference / LengthSeconds);
            if (total_bpm == 0)
            {
                total_bpm = bpm;
            }
            return Math.Round(total_bpm, 2);
        }
    }

    public class MIDITrack
    {
        public string Name { get; set; }
        public List<int> ValidNotes { get; set; }
        public List<MIDINote> ChartedNotes { get; set; }
        public List<SpecialMarker> Solos { get; set; }
        public List<SpecialMarker> Overdrive { get; set; }
        public void Sort()
        {
            ChartedNotes.Sort((a, b) => a.NoteStart.CompareTo(b.NoteStart));
            Solos.Sort((a, b) => a.MarkerBegin.CompareTo(b.MarkerBegin));
            Overdrive.Sort((a, b) => a.MarkerBegin.CompareTo(b.MarkerBegin));
        }
        public void Initialize()
        {
            ChartedNotes = new List<MIDINote>();
            Solos = new List<SpecialMarker>();
            Overdrive = new List<SpecialMarker>();
        }
    }

    public class MIDIChart
    {
        public MIDITrack ProKeysX { get; set; }
        public MIDITrack ProKeysH { get; set; }
        public MIDITrack ProKeysM { get; set; }
        public MIDITrack ProKeysE { get; set; }
        public MIDITrack ProKeysRH { get; set; }
        public MIDITrack ProKeysLH { get; set; }
        public List<RangeShift> RangeShifts { get; set; } 
        public double AverageBPM { get; set; }
        public void Initialize()
        {
            ProKeysX = new MIDITrack { Name = "Pro Keys X", ValidNotes = new List<int> { 72, 71, 70, 69, 68, 67, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48 } };
            ProKeysH = new MIDITrack { Name = "Pro Keys H", ValidNotes = new List<int> { 72, 71, 70, 69, 68, 67, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48 } };
            ProKeysM = new MIDITrack { Name = "Pro Keys M", ValidNotes = new List<int> { 72, 71, 70, 69, 68, 67, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48 } };
            ProKeysE = new MIDITrack { Name = "Pro Keys E", ValidNotes = new List<int> { 72, 71, 70, 69, 68, 67, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48 } };
            ProKeysRH = new MIDITrack { Name = "Pro Keys RH", ValidNotes = new List<int> { 72, 71, 70, 69, 68, 67, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48 } };
            ProKeysLH = new MIDITrack { Name = "Pro Keys LH", ValidNotes = new List<int> { 72, 71, 70, 69, 68, 67, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48 } };
            ProKeysX.Initialize();
            ProKeysH.Initialize();
            ProKeysM.Initialize();
            ProKeysE.Initialize();
            ProKeysRH.Initialize();
            ProKeysLH.Initialize();
            AverageBPM = 0.0;
            RangeShifts = new List<RangeShift>();
        }
    }
    
    public class MIDINote
    {
        public int NoteNumber { get; set; }
        public double NoteStart { get; set; }
        public double NoteEnd { get; set; }
        public double NoteLength { get; set; }
        public string NoteName { get; set; }
        public Color NoteColor { get; set; }
        public bool hasOD { get; set; }
        public bool Played { get; set; }
        public bool Stopped { get; set; }
        public int TicksPerQuarterNote { get; set; }
    }

    public class TempoEvent
    {
        public long AbsoluteTime { get; set; }
        public double RealTime { get; set; }
        public double BPM { get; set; }
    }

    public class PracticeSection
    {
        public double SectionStart { get; set; }
        public string SectionName { get; set; }
    }

    public class SpecialMarker
    {
        public double MarkerBegin { get; set; }
        public double MarkerEnd { get; set; }
    }

    public class RangeShift
    {
        public double ShiftBegin { get; set; }
        public int ShiftNote { get; set; }
    }
}