using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace iSpyApplication.Controls
{

    public class FishEyeCorrect
    {
        private double[] _mFisheyeCorrect;
        private int _mFeLimit = -1; //1500;
        private double _mScaleFeSize;//0.9;
        private double _aFocalLinPixels;
        private int[,] _map;
        private int _w = -1, _h = -1;
        

        private void Init(double aFocalLinPixels, int limit, double scale, int w, int h)
        {
            _mFeLimit = limit;
            _mScaleFeSize = scale;
            _aFocalLinPixels = aFocalLinPixels;
            _w = w;
            _h = h;

            _mFisheyeCorrect = new double[_mFeLimit];
            for (int i = 0; i < _mFeLimit; i++)
            {
                double result = Math.Sqrt(1 - 1 / Math.Sqrt(1.0 + (double)i * i / 1000000.0)) * 1.4142136;
                _mFisheyeCorrect[i] = result;
            }
            _map = new int[w*h,2];
            //setup pixel map
            double xc = w / 2.0;
            double yc = h / 2.0;
            int c = 0;
            for (var i = 0; i < w; i++)
            {
                for (var j = 0; j < h; j++)
                {
                    var xpos = i > xc;
                    var ypos = j > yc;
                    var xdif = i - xc;
                    var ydif = j - yc;

                    var rusquare = xdif * xdif + ydif * ydif;
                    var theta = Math.Atan2(ydif, xdif);
                    var index = (int)(Math.Sqrt(rusquare) / aFocalLinPixels * 1000);
                    if (index >= _mFeLimit) index = _mFeLimit - 1;

                    var rd = aFocalLinPixels * _mFisheyeCorrect[index] / _mScaleFeSize;

                    var xdelta = Math.Abs(rd * Math.Cos(theta));
                    var ydelta = Math.Abs(rd * Math.Sin(theta));
                    var xd = (int)(xc + (xpos ? xdelta : -xdelta));
                    var yd = (int)(yc + (ypos ? ydelta : -ydelta));
                    xd = Math.Max(0, Math.Min(xd, w - 1));
                    yd = Math.Max(0, Math.Min(yd, h - 1));
                    _map[c, 0] = xd;
                    _map[c, 1] = yd;
                    c++;

                }
            }


        }
        public Bitmap Correct(Bitmap aImage, double aFocalLinPixels, int limit, double scale)
        {
            if (Math.Abs(_aFocalLinPixels - aFocalLinPixels) > Double.Epsilon || limit != _mFeLimit || Math.Abs(scale - _mScaleFeSize) > Double.Epsilon || aImage.Width!=_w || aImage.Height!=_h)
            {
                Init(aFocalLinPixels, limit, scale, aImage.Width, aImage.Height);
            }


            var origImage = new UnsafeBitmap(aImage);
            var correctImage = new UnsafeBitmap(aImage);

            origImage.LockBitmap();
            correctImage.LockBitmap();
            int c = 0;
            for (int x = 0; x < _w; x++)
            {
                for (int y = 0; y < _h; y++)
                {
                    correctImage.SetPixel(x, y, origImage.GetPixel(_map[c,0], _map[c,1]));
                    c++;
                }

            }
            correctImage.UnlockBitmap();
            origImage.UnlockBitmap();

            origImage.Dispose();
            aImage.Dispose();
            return correctImage.Bitmap;
        }


        public unsafe class UnsafeBitmap
        {
            readonly Bitmap _bitmap;

            int _width;
            BitmapData _bitmapData;
            Byte* _pBase = null;

            public UnsafeBitmap(Bitmap bitmap)
            {
                _bitmap = new Bitmap(bitmap);
            }

            public UnsafeBitmap(int width, int height)
            {
                _bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            }

            public void Dispose()
            {
                _bitmap.Dispose();
            }

            public Bitmap Bitmap
            {
                get
                {
                    return (_bitmap);
                }
            }

            public void LockBitmap()
            {
                var unit = GraphicsUnit.Pixel;
                var boundsF = _bitmap.GetBounds(ref unit);
                var bounds = new Rectangle((int)boundsF.X,
                (int)boundsF.Y,
                (int)boundsF.Width,
                (int)boundsF.Height);
                _width = (int)boundsF.Width * sizeof(PixelData);
                if (_width % 4 != 0)
                {
                    _width = 4 * (_width / 4 + 1);
                }
                _bitmapData =_bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                _pBase = (Byte*)_bitmapData.Scan0.ToPointer();
            }

            public PixelData GetPixel(int x, int y)
            {
                PixelData returnValue = *PixelAt(x, y);
                return returnValue;
            }

            public void SetPixel(int x, int y, PixelData colour)
            {
                PixelData* pixel = PixelAt(x, y);
                *pixel = colour;
            }

            public void UnlockBitmap()
            {
                _bitmap.UnlockBits(_bitmapData);
                _bitmapData = null;
                _pBase = null;
            }
            public PixelData* PixelAt(int x, int y)
            {
                return (PixelData*)(_pBase + y * _width + x * sizeof(PixelData));
            }
        }
        public struct PixelData
        {
            public byte Blue;
            public byte Green;
            public byte Red;
        }
    }
}