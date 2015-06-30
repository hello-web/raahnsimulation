using System;
using Gdk;

namespace RaahnSimulation
{
    public class Event
    {
        public double X;
        public double Y;
        public int width;
        public int height;
        public uint button;
        public Gdk.Key key;
        public Gdk.ScrollDirection scrollDirection;
        public Gdk.EventType type;
        public Gtk.Window window;
    }
}

