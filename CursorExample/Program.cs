using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL;

/// <summary>
/// 
/// </summary>

namespace CursorExample
{
    static class Program
    {
        private static MouseCursor[] _cursors = new MouseCursor[3];
        private static Font _defaultFont = new Font(FontFamily.GenericSansSerif, 16.0f);
        private static Font _smallFont = new Font(FontFamily.GenericSansSerif, 12.0f);
        private static int GenTextTexture(string text, Font font)
        {
            var texId = GL.GenTexture();

            using (var bmp = new Bitmap(256, 32))
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.Clear(Color.Black);
                var sz = gr.MeasureString(text, font);
                
                gr.DrawString(text, font, new SolidBrush(Color.White), (256 - sz.Width) / 2, (32 - sz.Height) / 2);
                gr.Flush();
                var mem = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                GL.BindTexture(TextureTarget.Texture2D, texId);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.PixelStore(PixelStoreParameter.UnpackRowLength, mem.Stride / 4);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 256, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, mem.Scan0);
                bmp.UnlockBits(mem);
            }
            return texId;
        }

        private static MouseCursorFrame CreateFrame(int angle)
        {
            using (var bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.Clear(Color.Transparent);
                gr.DrawArc(new Pen(Color.Blue, 4.0f), 2, 2, 28, 28, angle, 300);

                var mem = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                var arr = new byte[4 * bmp.Width * bmp.Height];
                for (int y = 0; y < bmp.Height; y++)
                    Marshal.Copy(mem.Scan0 + mem.Stride * y, arr, bmp.Width * 4 * y, bmp.Width * 4);
                bmp.UnlockBits(mem);
                return new MouseCursorFrame(16, 16, 32, 32, arr);
            }
        }

        private static void DisplayText(int texId, int xOff, int yOff)
        {
            GL.Translate(xOff, yOff, 0);
            GL.BindTexture(TextureTarget.Texture2D, texId);
            GL.Enable(EnableCap.Texture2D);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(0, 0);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(256, 0);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(256, 32);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(0, 32);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
            GL.LoadIdentity();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            _cursors[0] = MouseCursor.Default;
            _cursors[1] = MouseCursor.Empty;
            _cursors[2] = new MouseCursor();
            
            for (int i = 0; i < 360; i += 20)
                _cursors[2].AddFrame(CreateFrame(i), 5.0f/60.0f);

            using (var win = new GameWindow())
            {
                int tex0 = 0, tex1 = 0, tex2 = 0, tex3 = 0, tex4 = 0;

                win.Load += (s, e) =>
                {
                    tex0 = GenTextTexture("Default Cursor", _defaultFont);
                    tex1 = GenTextTexture("Empty Cursor", _defaultFont);
                    tex2 = GenTextTexture("Hardware Animated", _defaultFont);
                    tex3 = GenTextTexture("Software Animated", _defaultFont);
                    tex4 = GenTextTexture("Hold mouse to emulate low FPS", _smallFont);
                };
                win.RenderFrame += (s, e) =>
                {
                    GL.Viewport(win.ClientRectangle);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    GL.Color3(0.8f, 0.8f, 1.0f);
                    GL.Rect(-1.0f, 0.0f, 0.0f, 1.0f);

                    GL.Color3(1.0f, 0.4f, 0.8f);
                    GL.Rect(0.0f, 0.0f, 1.0f, 1.0f);


                    GL.Color3(0.3f, 1.0f, 0.5f);
                    GL.Rect(-1.0f, -1.0f, 0.0f, 0.0f);

                    GL.Color3(0.8f, 0.9f, 0.4f);
                    GL.Rect(0.0f, -1.0f, 1.0f, 0.0f);

                    GL.Color3(1.0f, 1.0f, 1.0f);

                    GL.MatrixMode(MatrixMode.Projection);
                    var proj = Matrix4.CreateOrthographicOffCenter(0, win.ClientSize.Width, win.ClientSize.Height, 0, -1, 1);
                    GL.LoadMatrix(ref proj);
                    GL.MatrixMode(MatrixMode.Modelview);


                    DisplayText(tex0, win.Width / 4 - 128, win.Height / 4 - 16);
                    DisplayText(tex1, (3 * win.Width) / 4 - 128, win.Height / 4 - 16);
                    DisplayText(tex2, win.Width / 4 - 128, (3 * win.Height) / 4 - 16);
                    DisplayText(tex3, (3 * win.Width) / 4 - 128, (3 * win.Height) / 4 - 16);

                    DisplayText(tex4, win.Width / 2 - 128, win.Height / 2 - 16);


                    GL.MatrixMode(MatrixMode.Projection);
                    GL.LoadIdentity();
                    GL.MatrixMode(MatrixMode.Modelview);
                    GL.LoadIdentity();

                    GL.Flush();
                    if (win.Mouse.GetCursorState().IsAnyButtonDown)
                        Thread.Sleep(200);
                    win.SwapBuffers();
                };


                IEnumerator<MouseCursorFrameInformation> _softAnim = null;
                double _waitTime = 0.0;
                win.UpdateFrame += (s, e) =>
                {
                    if (_softAnim == null)
                        return;

                    _waitTime -= e.Time;
                    if (_waitTime <= 0.0f)
                    {
                        _waitTime += _softAnim.Current.duration;
                        var cur = new MouseCursor();
                        cur.AddFrame(_softAnim.Current.frame, 1.0f);
                        win.Cursor = cur;

                        if (!_softAnim.MoveNext())
                        {
                            _softAnim.Reset();
                            _softAnim.MoveNext();
                        }
                    }
                };

                win.MouseMove += (s, e) =>
                {

                    int quadrant = ((e.X < win.ClientRectangle.Width / 2) ? 0 : 1)
                        + ((e.Y < win.ClientRectangle.Height / 2) ? 0 : 2);

                    if (quadrant < 3)
                    {
                        _softAnim = null;
                        win.Cursor = _cursors[quadrant];
                    }
                    else
                    {
                        if (_softAnim == null)
                        {
                            _softAnim = _cursors[2].GetEnumerator();
                            _softAnim.MoveNext();
                            _waitTime = 0.0f;
                        }
                    }
                };

                win.Run(60.0f, 60.0f);
            }
        }
    }
}
