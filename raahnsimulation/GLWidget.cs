//Context creation adapted from GLWidget http://www.opentk.com/project/glwidget
using System;
using System.Security;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform;
using Gtk;

namespace RaahnSimulation 
{
    public class GLWidget : DrawingArea, IDisposable
    {
        private enum XVisualClass
        {
            StaticGray = 0,
            GrayScale = 1,
            StaticColor = 2,
            PseudoColor = 3,
            TrueColor = 4,
            DirectColor = 5,
        }

        [Flags]
        internal enum XVisualInfoMask
        {
            No = 0x0,
            ID = 0x1,
            Screen = 0x2,
            Depth = 0x4,
            Class = 0x8,
            Red = 0x10,
            Green = 0x20,
            Blue = 0x40,
            ColormapSize = 0x80,
            BitsPerRGB = 0x100,
            All = 0x1FF,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XVisualInfo
        {
            public IntPtr Visual;
            public IntPtr VisualID;
            public int Screen;
            public int Depth;
            public XVisualClass Class;
            public long RedMask;
            public long GreenMask;
            public long blueMask;
            public int ColormapSize;
            public int BitsPerRgb;

            public override string ToString()
            {
                return String.Format("id ({0}), screen ({1}), depth ({2}), class ({3})",
                                     VisualID, Screen, Depth, Class);
            }
        }

        public delegate void GLEvent();

        private const int GL_MAJOR_VERSION = 1;
        private const int GL_MINOR_VERSION = 5;
        private const int GLX_RGBA = 4;
        private const int GLX_DOUBLEBUFFER = 5;
        private const int DEFAULT_WIDTH = 300;
        private const int DEFAULT_HEIGHT = 300;
        private const string LIBX11 = "libX11.so.6";
        private const string LIBX11_GENERIC = "libX11";
        private const string LIBGDK_X11 = "libgdk-x11-2.0.so.0";
        private const string LIBGL = "libGL.so.1";
        private const string LIBGDK_WIN32 = "libgdk-win32-2.0-0.dll";
        private static readonly int[] ATTRIBUTES = { GLX_RGBA, GLX_DOUBLEBUFFER };

        private bool current;
        private bool sharedContextInitialized;
        private int graphicsContextCount;
        private IWindowInfo windowInfo;
        private IGraphicsContext graphicsContext;
        private GraphicsMode graphicsMode;
        private GLEvent onInit;
        private GLEvent onDraw;

        public GLWidget(GraphicsMode mode, GLEvent init, GLEvent draw)
        {
            Construct(mode, init, draw);
        }

        private void Construct(GraphicsMode mode, GLEvent init, GLEvent draw)
        {
            CanFocus = true;

            current = false;
            sharedContextInitialized = false;
            graphicsContextCount = 0;

            SetSizeRequest(DEFAULT_WIDTH, DEFAULT_HEIGHT);

            graphicsMode = mode;

            onInit = init;
            onDraw = draw;
        }

        //Init GL states.
        protected override void OnRealized()
        {
            base.OnRealized();

            if (graphicsContext != null)
                return;

            if (Configuration.RunningOnX11)
            {
                IntPtr display = gdk_x11_display_get_xdisplay(Display.Handle);
                IntPtr handle = gdk_x11_drawable_get_xid(GdkWindow.Handle);
                IntPtr rootWindow = gdk_x11_drawable_get_xid(RootWindow.Handle);

                IntPtr visualInfo;

                if (graphicsMode.Index.HasValue)
                {
                    XVisualInfo vInfo = new XVisualInfo();
                    vInfo.VisualID = graphicsMode.Index.Value;
                    int dummy;
                    visualInfo = XGetVisualInfo(display, (IntPtr)(int)(XVisualInfoMask.ID), ref vInfo, out dummy);
                }
                else
                    visualInfo = glXChooseVisual(display, Screen.Number, ATTRIBUTES);
            
                windowInfo = Utilities.CreateX11WindowInfo(display, Screen.Number, handle, rootWindow, visualInfo);
                XFree(visualInfo);
            }
            else if (Configuration.RunningOnWindows)
            {
                IntPtr handle = gdk_win32_drawable_get_handle(GdkWindow.Handle);
                windowInfo = Utilities.CreateWindowsWindowInfo(handle);
            }
            else if (Configuration.RunningOnMacOS)
            {
                IntPtr handle = gdk_x11_drawable_get_xid(GdkWindow.Handle);
                windowInfo = Utilities.CreateMacOSCarbonWindowInfo(handle, true, true);
            }

            graphicsContext = new GraphicsContext(graphicsMode, windowInfo, GL_MAJOR_VERSION, GL_MINOR_VERSION, 
                                                  GraphicsContextFlags.Default);
            graphicsContext.MakeCurrent(windowInfo);

            if (GraphicsContext.ShareContexts)
            {
                Interlocked.Increment(ref graphicsContextCount);

                if (!sharedContextInitialized)
                {
                    sharedContextInitialized = true;
                    ((IGraphicsContextInternal)graphicsContext).LoadAll();
                }
            }
            else
                ((IGraphicsContextInternal)graphicsContext).LoadAll();

            onInit();

            current = true;
        }

        //The widget is resized, change the viewport.
        protected override bool OnConfigureEvent(Gdk.EventConfigure eConfig)
        {   
            bool returnValue = base.OnConfigureEvent(eConfig);

            if (graphicsContext == null)
                return returnValue;

            graphicsContext.Update(windowInfo);

            return returnValue;
        }

        //Render the frame here.
        public void RenderFrame()
        {
            if (!current)
                return;

            graphicsContext.MakeCurrent(windowInfo);

            onDraw();

            graphicsContext.SwapBuffers();
        }

        public void Invalidate()
        {
            current = false;
        }

        public override void Dispose()
        {
            graphicsContext.Dispose();

            base.Dispose();
        }

        //Win32 functions.
        [SuppressUnmanagedCodeSecurity, DllImport(LIBGDK_WIN32)]
        private static extern IntPtr gdk_win32_drawable_get_handle(IntPtr d);

        //X11 functions.
        [SuppressUnmanagedCodeSecurity, DllImport(LIBX11)]
        private static extern void XFree(IntPtr handle);

        [SuppressUnmanagedCodeSecurity, DllImport(LIBX11_GENERIC)]
        private static extern IntPtr XGetVisualInfo(IntPtr display, IntPtr vinfo_mask, ref XVisualInfo template, out int nitems);

        [SuppressUnmanagedCodeSecurity, DllImport(LIBGDK_X11)]
        private static extern IntPtr gdk_x11_drawable_get_xid(IntPtr gdkDisplay);

        [SuppressUnmanagedCodeSecurity, DllImport(LIBGDK_X11)]
        private static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdkDisplay);

        [SuppressUnmanagedCodeSecurity, DllImport(LIBGL)]
        private static extern IntPtr glXChooseVisual(IntPtr display, int screen, int[] attr);
    }
}