using System;
using System.Runtime.InteropServices;
using ZeroUI.Core.Common;
using ZeroUI.WinForms.Native;

namespace ZeroUI.WinForms.Rendering
{
    /// <summary>
    /// Encapsulates a Win32 Memory DC backed by an unmanaged 32bpp DIB Section.
    /// Provides zero-copy BitBlt and hardware ClearType text rasterization.
    /// </summary>
    public sealed unsafe class MemoryDIBSection : IDisposable
    {
        private IntPtr _hMemDC;
        private IntPtr _hBitmap;
        private IntPtr _hOldBmp;
        private void* _pBits;
        private int _width;
        private int _height;
        private bool _disposed;

        public int Width => _width;
        public int Height => _height;
        public IntPtr Handle => _hMemDC;
        public void* PixelPointer => _pBits;

        public void EnsureSize(int width, int height, IntPtr hScreenDC)
        {
            if (width <= 0 || height <= 0) return;
            if (_hMemDC != IntPtr.Zero && _width >= width && _height >= height) return;

            ReleaseResources();

            _width = Math.Max(width, 100);
            _height = Math.Max(height, 100);

            _hMemDC = NativeMethods.CreateCompatibleDC(hScreenDC);

            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
            bmi.bmiHeader.biWidth = _width;
            bmi.bmiHeader.biHeight = -_height; // Top-down DIB
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = (uint)NativeMethods.BI_RGB;

            _hBitmap = NativeMethods.CreateDIBSection(
                _hMemDC,
                ref bmi,
                (uint)NativeMethods.DIB_RGB_COLORS,
                out IntPtr pBits,
                IntPtr.Zero,
                0);

            _pBits = (void*)pBits;
            _hOldBmp = NativeMethods.SelectObject(_hMemDC, _hBitmap);

            NativeMethods.SetBkMode(_hMemDC, NativeMethods.TRANSPARENT);
        }

        public void Clear(uint colorRef)
        {
            if (_hMemDC == IntPtr.Zero || _width <= 0 || _height <= 0) return;

            RECT rc = new RECT(0, 0, _width, _height);
            IntPtr hbr = NativeMethods.CreateSolidBrush(colorRef);
            NativeMethods.FillRect(_hMemDC, ref rc, hbr);
            NativeMethods.DeleteObject(hbr);
        }

        private IntPtr _hOldFont;

        public void SelectFont(IntPtr hFont)
        {
            if (_hMemDC == IntPtr.Zero || hFont == IntPtr.Zero) return;
            IntPtr old = NativeMethods.SelectObject(_hMemDC, hFont);
            if (_hOldFont == IntPtr.Zero)
            {
                _hOldFont = old;
            }
        }

        public void FillRectangle(int x, int y, int width, int height, uint colorRef)
        {
            if (_hMemDC == IntPtr.Zero || width <= 0 || height <= 0) return;

            RECT rc = new RECT(x, y, x + width, y + height);
            IntPtr hbr = NativeMethods.CreateSolidBrush(colorRef);
            NativeMethods.FillRect(_hMemDC, ref rc, hbr);
            NativeMethods.DeleteObject(hbr);
        }

        public void DrawText(ReadOnlySpan<char> text, ref RECT rect, uint textColor, CellAlignment alignment, int textHeight)
        {
            if (_hMemDC == IntPtr.Zero || text.IsEmpty || rect.Width <= 0 || rect.Height <= 0) return;

            NativeMethods.SetTextColor(_hMemDC, textColor);

            // Compute Y position vertically centered
            int y = rect.Top + Math.Max(0, (rect.Height - textHeight) / 2);
            int x = rect.Left + 8; // 8px standard left padding

            fixed (char* pText = text)
            {
                if (alignment == CellAlignment.Right)
                {
                    if (NativeMethods.GetTextExtentPoint32(_hMemDC, pText, text.Length, out SIZE sz))
                    {
                        x = Math.Max(rect.Left + 4, rect.Right - sz.cx - 8);
                    }
                }
                else if (alignment == CellAlignment.Center)
                {
                    if (NativeMethods.GetTextExtentPoint32(_hMemDC, pText, text.Length, out SIZE sz))
                    {
                        x = Math.Max(rect.Left + 4, rect.Left + (rect.Width - sz.cx) / 2);
                    }
                }

                NativeMethods.ExtTextOut(
                    _hMemDC,
                    x,
                    y,
                    NativeMethods.ETO_CLIPPED,
                    ref rect,
                    pText,
                    (uint)text.Length,
                    null);
            }
        }

        public void BitBltTo(IntPtr hTargetDC, int destX, int destY, int width, int height)
        {
            if (_hMemDC == IntPtr.Zero) return;

            NativeMethods.BitBlt(
                hTargetDC,
                destX,
                destY,
                width,
                height,
                _hMemDC,
                0,
                0,
                NativeMethods.SRCCOPY);
        }

        private void ReleaseResources()
        {
            if (_hMemDC != IntPtr.Zero)
            {
                if (_hOldFont != IntPtr.Zero)
                {
                    NativeMethods.SelectObject(_hMemDC, _hOldFont);
                    _hOldFont = IntPtr.Zero;
                }
                if (_hOldBmp != IntPtr.Zero)
                {
                    NativeMethods.SelectObject(_hMemDC, _hOldBmp);
                    _hOldBmp = IntPtr.Zero;
                }
                if (_hBitmap != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(_hBitmap);
                    _hBitmap = IntPtr.Zero;
                }
                NativeMethods.DeleteDC(_hMemDC);
                _hMemDC = IntPtr.Zero;
                _pBits = null;
            }
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                ReleaseResources();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~MemoryDIBSection()
        {
            ReleaseResources();
        }
    }
}
